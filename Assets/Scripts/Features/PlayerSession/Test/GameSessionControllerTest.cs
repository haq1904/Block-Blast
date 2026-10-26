using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlockBlast.Features.PlayerData;
using NUnit.Framework;
using UnityEngine;

namespace BlockBlast.Features.PlayerSession.Test
{
    [TestFixture]
    public sealed class GameSessionControllerTest
    {
        private GameObject _go;
        private GameSessionController _controller;
        private FakeGridService _gridService;
        private FakeSpawnService _spawnService;
        private FakeScoreService _scoreService;
        private FakeSubmissionService _submissionService;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("GameSessionControllerTest");
            _controller = _go.AddComponent<GameSessionController>();
            _gridService = new FakeGridService();
            _spawnService = new FakeSpawnService();
            _scoreService = new FakeScoreService();
            _submissionService = new FakeSubmissionService();

            _controller.InitializeForTest(_gridService, _spawnService, _scoreService, _submissionService);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                UnityEngine.Object.DestroyImmediate(_go);
            }
        }

        [Test]
        public void PlacementResolved_AccumulatesMetricsInSessionStats()
        {
            _gridService.SimulatePlacement(4, 1, false);
            _gridService.SimulatePlacement(3, 0, false);

            Assert.AreEqual(7, _controller.SessionStats.BlocksPlaced);
            Assert.AreEqual(1, _controller.SessionStats.LinesCleared);
        }

        [Test]
        public void NoMovesLeft_EnqueuesResultWithCapturedScoreAndCombo()
        {
            _gridService.SimulatePlacement(5, 2, false);
            _scoreService.CurrentScore = 2500;
            _scoreService.MaxCombo = 4;

            _spawnService.SimulateNoMovesLeft();

            Assert.IsTrue(_controller.SessionStats.IsCompleted);
            Assert.AreEqual(1, _submissionService.EnqueuedResults.Count);

            var enqueued = _submissionService.EnqueuedResults[0];
            Assert.AreEqual(2500, enqueued.Score);
            Assert.AreEqual(4, enqueued.BestCombo);
            Assert.AreEqual(5, enqueued.BlocksPlaced);
            Assert.AreEqual(2, enqueued.LinesCleared);
            Assert.AreEqual(1, _submissionService.FlushCallCount);
        }

        [Test]
        public void RepeatedNoMovesLeft_OnlyEnqueuesOnce()
        {
            _gridService.SimulatePlacement(3, 1, false);
            _scoreService.CurrentScore = 500;
            _scoreService.MaxCombo = 2;

            _spawnService.SimulateNoMovesLeft();
            _spawnService.SimulateNoMovesLeft();
            _spawnService.SimulateNoMovesLeft();

            Assert.AreEqual(1, _submissionService.EnqueuedResults.Count);
            Assert.AreEqual(1, _submissionService.FlushCallCount);
        }

        private sealed class FakeGridService : IGridService
        {
            public int GridWidth => 8;
            public int GridHeight => 8;
            public int OccupiedCellCount => 0;
            public float OccupancyRatio => 0f;

            public event Action<bool, List<CellPlacementData>> OnPreviewStateChanged;
            public event Action<List<CellPlacementData>> OnBlockPlaced;
            public event Action<List<int>, List<int>, List<Vector3>> OnLinesCleared;
            public event Action<List<int>, List<int>> OnPreviewLinesToClear;
            public event Action<int, int, bool> OnPlacementResolved;

            public void SimulatePlacement(int tilesPlaced, int totalLinesCleared, bool isAllClear)
            {
                OnPlacementResolved?.Invoke(tilesPlaced, totalLinesCleared, isAllClear);
            }

            public Vector2Int GetGridPositionFromWorld(Vector3 worldPos) => Vector2Int.zero;
            public Vector3 GetWorldPositionFromGrid(Vector2Int gridPos) => Vector3.zero;
            public bool CanPlaceBlocks(List<CellPlacementData> cells) => true;
            public bool CanPlaceBlocks(List<Vector2Int> gridPositions) => true;
            public bool IsCellOccupied(int col, int row) => false;
            public void RequestPreview(List<CellPlacementData> cells) { }
            public void RequestPreview(List<Vector2Int> gridPositions) { }
            public void PlaceBlocks(List<CellPlacementData> cells) { }
            public void PlaceBlocks(List<Vector2Int> gridPositions) { }
        }

        private sealed class FakeSpawnService : ISpawnService
        {
            public event Action<BlockModel[], Vector3[]> OnBatchSpawned;
            public event Action OnNoMovesLeft;

            public void SimulateNoMovesLeft()
            {
                OnNoMovesLeft?.Invoke();
            }

            public void SpawnBatch(int score) { }
            public void MarkSlotEmpty(int slotIndex) { }
        }

        private sealed class FakeScoreService : IScoreService
        {
            public int CurrentScore { get; set; }
            public int HighScore { get; set; }
            public int CurrentCombo { get; set; }
            public int MaxCombo { get; set; }
            public bool IsPrewarmed => true;
            public int TurnsRemaining => 0;

            public event Action<int, int> OnScoreChanged;
            public event Action<int> OnComboChanged;
            public event Action<int> OnHighScoreChanged;

            public void ResetScore() { }
        }

        private sealed class FakeSubmissionService : IGameResultSubmissionService
        {
            public List<GameResult> EnqueuedResults { get; } = new List<GameResult>();
            public int FlushCallCount { get; private set; }
            public int PendingCount => EnqueuedResults.Count;

            public event Action<GameResultSubmissionOutcome> OnResultProcessed;

            public QueueEnqueueResult Enqueue(GameResult result)
            {
                EnqueuedResults.Add(result);
                return QueueEnqueueResult.Success;
            }

            public Task FlushPendingAsync(CancellationToken cancellationToken = default)
            {
                FlushCallCount++;
                return Task.CompletedTask;
            }
        }
    }
}
