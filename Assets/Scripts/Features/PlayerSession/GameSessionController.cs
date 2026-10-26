using System;
using System.Threading;
using BlockBlast.Core;
using BlockBlast.Features.PlayerData;
using UnityEngine;

namespace BlockBlast.Features.PlayerSession
{
    [DefaultExecutionOrder(-50)]
    public sealed class GameSessionController : MonoBehaviour
    {
        private IGridService _gridService;
        private ISpawnService _spawnService;
        private IScoreService _scoreService;
        private IGameResultSubmissionService _submissionService;

        private IGameSessionStats _sessionStats;

        public IGameSessionStats SessionStats => _sessionStats;

        public void InitializeForTest(
            IGridService gridService,
            ISpawnService spawnService,
            IScoreService scoreService,
            IGameResultSubmissionService submissionService,
            IGameSessionStats sessionStats = null)
        {
            _gridService = gridService;
            _spawnService = spawnService;
            _scoreService = scoreService;
            _submissionService = submissionService;
            _sessionStats = sessionStats ?? new GameSessionStats();

            SubscribeEvents();
        }

        private void Start()
        {
            if (_sessionStats != null)
            {
                // Already initialized (e.g. in test).
                return;
            }

            if (!ServiceLocator.TryGet<IGridService>(out _gridService) ||
                !ServiceLocator.TryGet<ISpawnService>(out _spawnService) ||
                !ServiceLocator.TryGet<IScoreService>(out _scoreService) ||
                !ServiceLocator.TryGet<IGameResultSubmissionService>(out _submissionService))
            {
                Debug.LogWarning("[GameSessionController] Missing required service dependency. Disabling session controller.", this);
                enabled = false;
                return;
            }

            _sessionStats = new GameSessionStats();
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            if (_gridService != null)
            {
                _gridService.OnPlacementResolved += HandlePlacementResolved;
            }

            if (_spawnService != null)
            {
                _spawnService.OnNoMovesLeft += HandleNoMovesLeft;
            }
        }

        private void UnsubscribeEvents()
        {
            if (_gridService != null)
            {
                _gridService.OnPlacementResolved -= HandlePlacementResolved;
            }

            if (_spawnService != null)
            {
                _spawnService.OnNoMovesLeft -= HandleNoMovesLeft;
            }
        }

        private void HandlePlacementResolved(int tilesPlaced, int totalLinesCleared, bool isAllClear)
        {
            if (_sessionStats == null || _sessionStats.IsCompleted)
            {
                return;
            }

            _sessionStats.RecordPlacement(tilesPlaced, totalLinesCleared);
        }

        private void HandleNoMovesLeft()
        {
            if (_sessionStats == null || _sessionStats.IsCompleted)
            {
                return;
            }

            int finalScore = _scoreService != null ? _scoreService.CurrentScore : 0;
            int bestCombo = _scoreService != null ? _scoreService.MaxCombo : 0;
            long completedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            GameResult result = _sessionStats.Complete(finalScore, bestCombo, completedAt);

            if (_submissionService != null)
            {
                _submissionService.Enqueue(result);
                _ = _submissionService.FlushPendingAsync(CancellationToken.None);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }
    }
}
