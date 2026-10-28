using System;
using System.Threading;
using System.Threading.Tasks;
using BlockBlast.Features.CloudServices;
using BlockBlast.Features.Lobby.GameCenter;
using BlockBlast.Features.PlayerData;
using BlockBlast.Features.PlayerCloud;
using BlockBlast.Features.PlayerSession;
using NUnit.Framework;
using UnityEngine;

namespace BlockBlast.Features.GameFlow.Navigation.Test
{
    [TestFixture]
    public sealed class LobbyGameCenterDataControllerTest
    {
        private GameObject _holder;
        private LobbyGameCenterPanelView _panelView;
        private LobbyGameCenterDataController _controller;
        private FakePlayerCloudService _cloudService;
        private FakeSubmissionService _submissionService;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("GameCenterControllerTest");
            _holder.AddComponent<CanvasGroup>();
            _panelView = _holder.AddComponent<LobbyGameCenterPanelView>();
            _controller = _holder.AddComponent<LobbyGameCenterDataController>();

            _cloudService = new FakePlayerCloudService();
            _submissionService = new FakeSubmissionService();

            _controller.Initialize(_cloudService, _panelView, _submissionService);
        }

        [TearDown]
        public void TearDown()
        {
            if (_holder != null)
            {
                UnityEngine.Object.DestroyImmediate(_holder);
            }
        }

        [Test]
        public async Task RefreshDataAsync_WhenCloudSucceeds_FlushesPendingAndRendersSyncedData()
        {
            _submissionService.PendingCount = 1;
            var snapshot = CreateSnapshot(revision: 5L, bestScore: 9999);
            _cloudService.LoadResultToReturn = CloudOperationResult<GameCenterSnapshot>.Success(snapshot);

            await _controller.RefreshDataAsync();

            Assert.AreEqual(1, _submissionService.FlushCallCount);
            Assert.AreEqual(1, _cloudService.LoadCallCount);
            Assert.AreEqual(5L, _cloudService.CachedSnapshot.Revision);
        }

        [Test]
        public async Task RefreshDataAsync_WhenCloudFailsWithCachedSnapshot_RendersCachedData()
        {
            var cached = CreateSnapshot(revision: 3L, bestScore: 4000);
            _cloudService.CachedSnapshot = cached;
            _cloudService.LoadResultToReturn = CloudOperationResult<GameCenterSnapshot>.Failure(
                CloudFailureKind.Offline,
                true,
                "NETWORK_ERROR");

            await _controller.RefreshDataAsync();

            Assert.AreEqual(1, _cloudService.LoadCallCount);
            Assert.AreEqual(3L, _cloudService.CachedSnapshot.Revision);
        }

        [Test]
        public void RefreshDataAsync_WhenNoCacheAndCloudFails_DoesNotThrow()
        {
            _cloudService.CachedSnapshot = null;
            _cloudService.LoadResultToReturn = CloudOperationResult<GameCenterSnapshot>.Failure(
                CloudFailureKind.Offline,
                false,
                "NETWORK_UNAVAILABLE");

            Assert.DoesNotThrowAsync(async () => await _controller.RefreshDataAsync());
        }

        private static GameCenterSnapshot CreateSnapshot(long revision, int bestScore)
        {
            return new GameCenterSnapshot(
                revision,
                1000L,
                1000L,
                new PlayerStats(1, bestScore, bestScore, 5, 20, 2, 1),
                new WeeklyPlayerStats(1000L, 1, bestScore, bestScore, 5, 20, 1),
                new WeeklyPlayerStats(500L, 0, 0, 0, 0, 0, 0),
                5L,
                Array.Empty<RecentGameResult>(),
                Array.Empty<AchievementProgress>(),
                Array.Empty<PlayerInsight>(),
                PlayerDataSchema.CurrentVersion);
        }

        private sealed class FakePlayerCloudService : IPlayerCloudService
        {
            public CloudSyncState State { get; set; } = CloudSyncState.Ready;
            public GameCenterSnapshot CachedSnapshot { get; set; }
            public int LoadCallCount { get; private set; }
            public CloudOperationResult<GameCenterSnapshot> LoadResultToReturn { get; set; }

            public Task<CloudOperationResult<GameCenterSnapshot>> LoadSnapshotAsync(CancellationToken cancellationToken = default)
            {
                LoadCallCount++;
                if (LoadResultToReturn.IsSuccess && LoadResultToReturn.Value != null)
                {
                    CachedSnapshot = LoadResultToReturn.Value;
                }
                return Task.FromResult(LoadResultToReturn);
            }

            public Task<CloudOperationResult<SubmitGameResultResponse>> SubmitGameResultAsync(GameResult result, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(CloudOperationResult<SubmitGameResultResponse>.Failure(CloudFailureKind.Cancelled, false, "N/A"));
            }

            public Task<CloudOperationResult<GameCenterSnapshot>> ResetForIdentityChangeAsync(CancellationToken cancellationToken = default)
            {
                CachedSnapshot = null;
                return Task.FromResult(CloudOperationResult<GameCenterSnapshot>.Success(null));
            }
        }

        private sealed class FakeSubmissionService : IGameResultSubmissionService
        {
#pragma warning disable 0067
            public event Action<GameResultSubmissionOutcome> OnResultProcessed;
#pragma warning restore 0067
            public int PendingCount { get; set; }
            public int FlushCallCount { get; private set; }

            public QueueEnqueueResult Enqueue(GameResult result) => QueueEnqueueResult.Success;

            public Task FlushPendingAsync(CancellationToken cancellationToken = default)
            {
                FlushCallCount++;
                return Task.CompletedTask;
            }
        }
    }
}
