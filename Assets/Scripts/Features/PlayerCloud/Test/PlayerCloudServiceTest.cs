using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlockBlast.Features.CloudServices;
using BlockBlast.Features.PlayerData;
using BlockBlast.Features.PlayerCloud.Gateway;
using BlockBlast.Features.PlayerCloud.Mapping;
using BlockBlast.Features.PlayerCloud.Persistence;
using BlockBlast.Features.PlayerCloud.Retry;
using BlockBlast.Features.PlayerCloud.Time;
using NUnit.Framework;
using Generated = Unity.Services.CloudCode.GeneratedBindings.CubePuzzle3DPlayerData.Contracts;

namespace BlockBlast.Features.PlayerCloud.Test
{
    [TestFixture]
    public sealed class PlayerCloudServiceTest
    {
        private FakeInitService _initService;
        private FakeProtectedGateway _protectedGateway;
        private FakeCloudCodeGateway _cloudCodeGateway;
        private FakeSnapshotCache _cache;
        private FakeRetryDelay _retryDelay;
        private FakeClock _clock;
        private UnityJsonPlayerDataSerializer _serializer;

        [SetUp]
        public void SetUp()
        {
            _initService = new FakeInitService(true);
            _protectedGateway = new FakeProtectedGateway();
            _cloudCodeGateway = new FakeCloudCodeGateway();
            _cache = new FakeSnapshotCache();
            _retryDelay = new FakeRetryDelay();
            _clock = new FakeClock(1700000000L);
            _serializer = new UnityJsonPlayerDataSerializer();
        }

        [Test]
        public async Task LoadSnapshotAsync_WhenInitFails_ReturnsAuthUnavailable()
        {
            _initService.InitResult = false;
            using var service = CreateService();

            var result = await service.LoadSnapshotAsync();

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(CloudFailureKind.AuthenticationUnavailable, result.FailureKind);
            Assert.AreEqual(CloudSyncState.Offline, service.State);
        }

        [Test]
        public async Task LoadSnapshotAsync_MissingKey_ReturnsEmptySnapshotWithEighteenAchievements()
        {
            _protectedGateway.ResultToReturn = ProtectedPlayerDataReadResult.NotFound();
            using var service = CreateService();

            var result = await service.LoadSnapshotAsync();

            Assert.IsTrue(result.IsSuccess);
            Assert.IsNotNull(result.Value);
            Assert.AreEqual(0L, result.Value.Revision);
            Assert.AreEqual(AchievementCatalog.Definitions.Count, result.Value.Achievements.Count);
            Assert.AreEqual(CloudSyncState.Ready, service.State);
        }

        [Test]
        public async Task LoadSnapshotAsync_ConcurrentCalls_JoinSingleFlight()
        {
            var tcs = new TaskCompletionSource<ProtectedPlayerDataReadResult>();
            _protectedGateway.DelayedTask = tcs.Task;
            using var service = CreateService();

            var task1 = service.LoadSnapshotAsync();
            var task2 = service.LoadSnapshotAsync();
            var task3 = service.LoadSnapshotAsync();

            Assert.AreEqual(1, _protectedGateway.CallCount);

            tcs.SetResult(ProtectedPlayerDataReadResult.NotFound());

            var r1 = await task1;
            var r2 = await task2;
            var r3 = await task3;

            Assert.IsTrue(r1.IsSuccess);
            Assert.IsTrue(r2.IsSuccess);
            Assert.IsTrue(r3.IsSuccess);
            Assert.AreEqual(1, _protectedGateway.CallCount);
        }

        [Test]
        public async Task LoadSnapshotAsync_RetriesTransientFailureUpToThreeAttempts()
        {
            _protectedGateway.ExceptionsToThrow = new Queue<Exception>(new[]
            {
                new TimeoutException("Temporary timeout"),
                new TimeoutException("Temporary timeout")
            });
            _protectedGateway.ResultToReturn = ProtectedPlayerDataReadResult.NotFound();
            using var service = CreateService();

            var result = await service.LoadSnapshotAsync();

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(3, _protectedGateway.CallCount);
            Assert.AreEqual(2, _retryDelay.DelayCalls.Count);
            Assert.AreEqual(PlayerCloudConstants.DefaultRetryDelay1Ms, _retryDelay.DelayCalls[0]);
            Assert.AreEqual(PlayerCloudConstants.DefaultRetryDelay2Ms, _retryDelay.DelayCalls[1]);
        }

        [Test]
        public async Task SubmitGameResultAsync_ValidAccepted_UpdatesCachedSnapshot()
        {
            var validSnapshot = CreateValidContract(1L);
            _cloudCodeGateway.ResponseToReturn = new Generated.SubmitGameResultResponse
            {
                Status = Generated.SubmitGameResultStatus.Accepted,
                UpdatedSnapshot = validSnapshot,
                NewlyUnlockedAchievementIds = new List<string> { AchievementCatalog.FirstSteps },
                IsNewPersonalBest = true,
                IsDuplicate = false
            };
            using var service = CreateService();

            var gameResult = new GameResult("session_1", 100, 2, 5, 1, 1000L);
            var result = await service.SubmitGameResultAsync(gameResult);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(1L, result.Value.UpdatedSnapshot.Revision);
            Assert.AreEqual(1L, service.CachedSnapshot.Revision);
            Assert.AreEqual(CloudSyncState.Ready, service.State);
            Assert.AreEqual(1L, _cache.SavedSnapshot?.Revision);
        }

        [Test]
        public async Task SubmitGameResultAsync_Duplicate_ReturnsDuplicateWithoutRegressingRevision()
        {
            var initialSnapshot = CreateValidContract(5L);
            _cloudCodeGateway.ResponseToReturn = new Generated.SubmitGameResultResponse
            {
                Status = Generated.SubmitGameResultStatus.Accepted,
                UpdatedSnapshot = initialSnapshot,
                NewlyUnlockedAchievementIds = new List<string>(),
                IsNewPersonalBest = false,
                IsDuplicate = false
            };
            using var service = CreateService();

            var gameResult = new GameResult("session_1", 100, 2, 5, 1, 1000L);
            await service.SubmitGameResultAsync(gameResult);
            Assert.AreEqual(5L, service.CachedSnapshot.Revision);

            // Resubmit duplicate session returning same revision
            _cloudCodeGateway.ResponseToReturn = new Generated.SubmitGameResultResponse
            {
                Status = Generated.SubmitGameResultStatus.Duplicate,
                UpdatedSnapshot = initialSnapshot,
                NewlyUnlockedAchievementIds = new List<string>(),
                IsNewPersonalBest = false,
                IsDuplicate = true
            };

            var dupResult = await service.SubmitGameResultAsync(gameResult);

            Assert.IsTrue(dupResult.IsSuccess);
            Assert.IsTrue(dupResult.Value.IsDuplicate);
            Assert.AreEqual(5L, service.CachedSnapshot.Revision);
        }

        [Test]
        public async Task SubmitGameResultAsync_ServerRejection_DoesNotRetry()
        {
            _cloudCodeGateway.ResponseToReturn = new Generated.SubmitGameResultResponse
            {
                Status = Generated.SubmitGameResultStatus.Rejected,
                ErrorCode = "INVALID_SESSION_ID"
            };
            using var service = CreateService();

            var gameResult = new GameResult("session_bad", 100, 2, 5, 1, 1000L);
            var result = await service.SubmitGameResultAsync(gameResult);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(CloudFailureKind.InvalidPayload, result.FailureKind);
            Assert.AreEqual(1, _cloudCodeGateway.CallCount);
            Assert.AreEqual(0, _retryDelay.DelayCalls.Count);
        }

        [Test]
        public async Task SubmitGameResultAsync_Cancellation_ReturnsCancelledWithoutMutatingCache()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            using var service = CreateService();

            var gameResult = new GameResult("session_cancel", 100, 2, 5, 1, 1000L);
            var result = await service.SubmitGameResultAsync(gameResult, cts.Token);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(CloudFailureKind.Cancelled, result.FailureKind);
            Assert.AreEqual(0, _cloudCodeGateway.CallCount);
            Assert.IsNull(_cache.SavedSnapshot);
        }

        [Test]
        public async Task ResetForIdentityChangeAsync_ClearsMemorySnapshot_AndInvokesCacheClear()
        {
            var service = CreateService();
            _protectedGateway.ResultToReturn = ProtectedPlayerDataReadResult.NotFound();

            var loadResult = await service.LoadSnapshotAsync();
            Assert.IsTrue(loadResult.IsSuccess);
            Assert.AreEqual(CloudSyncState.Ready, service.State);

            var resetResult = await service.ResetForIdentityChangeAsync();
            Assert.IsTrue(resetResult.IsSuccess);
            Assert.AreEqual(0L, service.CachedSnapshot.Revision);
            Assert.AreEqual(CloudSyncState.Uninitialized, service.State);
            Assert.AreEqual(1, _cache.ClearCallCount);
        }

        [Test]
        public async Task ResetForIdentityChangeAsync_WhenCancelled_ReturnsCancelledFailure()
        {
            var service = CreateService();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var result = await service.ResetForIdentityChangeAsync(cts.Token);
            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(CloudFailureKind.Cancelled, result.FailureKind);
            Assert.AreEqual(0, _cache.ClearCallCount);
        }

        private PlayerCloudService CreateService()
        {
            return new PlayerCloudService(
                _initService,
                _protectedGateway,
                _cloudCodeGateway,
                _cache,
                _retryDelay,
                _clock,
                _serializer);
        }

        private static Generated.GameCenterSnapshotContract CreateValidContract(long revision)
        {
            return new Generated.GameCenterSnapshotContract
            {
                SchemaVersion = PlayerDataSchema.CurrentVersion,
                Revision = revision,
                CreatedAtUnixSeconds = 1000L,
                UpdatedAtUnixSeconds = 1000L,
                LifetimeStats = new Generated.PlayerStatsContract
                {
                    GamesPlayed = 1L,
                    TotalScore = 100L,
                    BestScore = 100,
                    TotalLinesCleared = 2L,
                    TotalBlocksPlaced = 5L,
                    BestCombo = 1,
                    PersonalBestBreakCount = 1L
                },
                CurrentWeek = new Generated.WeeklyPlayerStatsContract
                {
                    WeekStartUnixSeconds = 1000L,
                    GamesPlayed = 1L,
                    TotalScore = 100L,
                    BestScore = 100,
                    TotalLinesCleared = 2L,
                    TotalBlocksPlaced = 5L,
                    PersonalBestBreakCount = 1L
                },
                PreviousWeek = new Generated.WeeklyPlayerStatsContract
                {
                    WeekStartUnixSeconds = 500L,
                    GamesPlayed = 0L,
                    TotalScore = 0L,
                    BestScore = 0,
                    TotalLinesCleared = 0L,
                    TotalBlocksPlaced = 0L,
                    PersonalBestBreakCount = 0L
                },
                BestWeekLinesCleared = 2L,
                RecentGames = new List<Generated.RecentGameResultContract>(),
                Achievements = new List<Generated.AchievementProgressContract>(),
                Insights = new List<Generated.PlayerInsightContract>()
            };
        }

        private sealed class FakeInitService : ICloudInitializationService
        {
            public CloudInitializationState State => InitResult ? CloudInitializationState.Ready : CloudInitializationState.Unavailable;
            public string EnvironmentName => "development";
            public string PlayerId => "test_player";
            public bool IsReady => InitResult;

            public bool InitResult { get; set; }

            public FakeInitService(bool initResult)
            {
                InitResult = initResult;
            }

            public Task<bool> InitializeAsync() => Task.FromResult(InitResult);
        }

        private sealed class FakeProtectedGateway : IProtectedPlayerDataGateway
        {
            public int CallCount { get; private set; }
            public ProtectedPlayerDataReadResult ResultToReturn { get; set; } = ProtectedPlayerDataReadResult.NotFound();
            public Task<ProtectedPlayerDataReadResult> DelayedTask { get; set; }
            public Queue<Exception> ExceptionsToThrow { get; set; } = new Queue<Exception>();

            public async Task<ProtectedPlayerDataReadResult> LoadProtectedSnapshotJsonAsync()
            {
                CallCount++;
                if (ExceptionsToThrow.Count > 0)
                {
                    throw ExceptionsToThrow.Dequeue();
                }

                if (DelayedTask != null)
                {
                    return await DelayedTask;
                }

                return ResultToReturn;
            }
        }

        private sealed class FakeCloudCodeGateway : IPlayerCloudCodeGateway
        {
            public int CallCount { get; private set; }
            public Generated.SubmitGameResultResponse ResponseToReturn { get; set; }
            public Queue<Exception> ExceptionsToThrow { get; set; } = new Queue<Exception>();

            public Task<Generated.SubmitGameResultResponse> SubmitGameResultAsync(Generated.GameResultRequest request)
            {
                CallCount++;
                if (ExceptionsToThrow.Count > 0)
                {
                    throw ExceptionsToThrow.Dequeue();
                }

                return Task.FromResult(ResponseToReturn);
            }
        }

        private sealed class FakeSnapshotCache : IPlayerCloudSnapshotCache
        {
            public GameCenterSnapshot SavedSnapshot { get; private set; }
            public GameCenterSnapshot LoadedSnapshot { get; set; }
            public int ClearCallCount { get; private set; }

            public bool TryLoad(out GameCenterSnapshot snapshot)
            {
                snapshot = LoadedSnapshot;
                return LoadedSnapshot != null;
            }

            public bool Save(GameCenterSnapshot snapshot)
            {
                SavedSnapshot = snapshot;
                return true;
            }

            public void Clear()
            {
                ClearCallCount++;
                SavedSnapshot = null;
                LoadedSnapshot = null;
            }
        }

        private sealed class FakeRetryDelay : IPlayerCloudRetryDelay
        {
            public List<int> DelayCalls { get; } = new List<int>();

            public Task DelayAsync(int milliseconds, CancellationToken cancellationToken)
            {
                DelayCalls.Add(milliseconds);
                return Task.CompletedTask;
            }
        }

        private sealed class FakeClock : IPlayerCloudClock
        {
            public long NowUnixSeconds { get; }

            public FakeClock(long now)
            {
                NowUnixSeconds = now;
            }
        }
    }
}
