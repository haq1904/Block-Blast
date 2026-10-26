using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlockBlast.Features.CloudServices;
using BlockBlast.Features.PlayerCloud;
using BlockBlast.Features.PlayerData;
using BlockBlast.Features.PlayerIdentity.Gateway;
using BlockBlast.Features.PlayerSession;
using NUnit.Framework;

namespace BlockBlast.Features.PlayerIdentity.Test
{
    [TestFixture]
    public sealed class PlayerIdentityServiceTest
    {
        private FakeInitService _initService;
        private FakeAuthGateway _authGateway;
        private FakeAuthCodeProvider _authCodeProvider;
        private FakeCloudService _cloudService;
        private FakeSubmissionService _submissionService;
        private PlayerIdentityService _service;
        private List<PlayerIdentitySnapshot> _stateHistory;

        [SetUp]
        public void SetUp()
        {
            _initService = new FakeInitService(true);
            _authGateway = new FakeAuthGateway("player_anon_1", true);
            _authCodeProvider = new FakeAuthCodeProvider(true, "valid_code_123");
            _cloudService = new FakeCloudService();
            _submissionService = new FakeSubmissionService();

            _service = new PlayerIdentityService(
                _initService,
                _authGateway,
                _authCodeProvider,
                _cloudService,
                _submissionService);

            _stateHistory = new List<PlayerIdentitySnapshot>();
            _service.StateChanged += s => _stateHistory.Add(s);
        }

        [TearDown]
        public void TearDown()
        {
            _service.Dispose();
        }

        [Test]
        public async Task RefreshAsync_WhenNotReady_ReturnsUnavailable()
        {
            _initService.IsReady = false;

            var result = await _service.RefreshAsync();

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(PlayerIdentityStatus.Unavailable, _service.Snapshot.Status);
            Assert.IsFalse(_service.Snapshot.IsLinked);
        }

        [Test]
        public async Task RefreshAsync_WhenSignedInWithGoogle_SetsLinkedStatus()
        {
            _authGateway.HasGooglePlayGamesIdentity = true;

            var result = await _service.RefreshAsync();

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(PlayerIdentityStatus.Linked, _service.Snapshot.Status);
            Assert.IsTrue(_service.Snapshot.IsLinked);
        }

        [Test]
        public async Task RefreshAsync_WhenSignedInAnonymously_SetsAnonymousStatus()
        {
            _authGateway.HasGooglePlayGamesIdentity = false;

            var result = await _service.RefreshAsync();

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(PlayerIdentityStatus.Anonymous, _service.Snapshot.Status);
            Assert.IsFalse(_service.Snapshot.IsLinked);
        }

        [Test]
        public async Task LinkGooglePlayGamesAsync_UnsupportedPlatform_ReturnsFailure()
        {
            _authCodeProvider.IsSupported = false;

            var result = await _service.LinkGooglePlayGamesAsync();

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(PlayerIdentityFailureKind.UnsupportedPlatform, result.FailureKind);
        }

        [Test]
        public async Task LinkGooglePlayGamesAsync_Success_MaintainsSamePlayerId_SetsLinkedStatus()
        {
            string originalPlayerId = _authGateway.PlayerId;

            var result = await _service.LinkGooglePlayGamesAsync();

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(PlayerIdentityStatus.Linked, _service.Snapshot.Status);
            Assert.IsTrue(_service.Snapshot.IsLinked);
            Assert.AreEqual(originalPlayerId, _authGateway.PlayerId);
            Assert.IsFalse(_authGateway.LastForceLinkOption);
            Assert.AreEqual(1, _cloudService.LoadCallCount);
        }

        [Test]
        public async Task LinkGooglePlayGamesAsync_ProviderCancel_KeepsAnonymousStatus()
        {
            _authCodeProvider.AuthResultToReturn = AuthCodeFetchResult.Failure(
                PlayerIdentityFailureKind.Cancelled,
                "AUTH_CANCELLED");

            var result = await _service.LinkGooglePlayGamesAsync();

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(PlayerIdentityFailureKind.Cancelled, result.FailureKind);
            Assert.AreEqual(PlayerIdentityStatus.Anonymous, _service.Snapshot.Status);
        }

        [Test]
        public async Task LinkGooglePlayGamesAsync_AccountAlreadyLinked_TransitionsToConflict()
        {
            _authGateway.LinkResultToReturn = AuthGatewayResult.Failure(
                PlayerIdentityFailureKind.AccountAlreadyLinked,
                "ACCOUNT_ALREADY_LINKED");

            var result = await _service.LinkGooglePlayGamesAsync();

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(PlayerIdentityFailureKind.AccountAlreadyLinked, result.FailureKind);
            Assert.AreEqual(PlayerIdentityStatus.Conflict, _service.Snapshot.Status);
        }

        [Test]
        public async Task ResolveConflictAsync_Cancel_ReturnsToAnonymousStatus()
        {
            _authGateway.LinkResultToReturn = AuthGatewayResult.Failure(
                PlayerIdentityFailureKind.AccountAlreadyLinked,
                "ACCOUNT_ALREADY_LINKED");
            await _service.LinkGooglePlayGamesAsync();
            Assert.AreEqual(PlayerIdentityStatus.Conflict, _service.Snapshot.Status);

            var cancelResult = await _service.ResolveConflictAsync(AccountLinkConflictResolution.Cancel);

            Assert.IsTrue(cancelResult.IsSuccess);
            Assert.AreEqual(PlayerIdentityStatus.Anonymous, _service.Snapshot.Status);
        }

        [Test]
        public async Task ResolveConflictAsync_KeepCurrentProgress_CallsForceLink_MaintainsSamePlayerId_SetsLinkedStatus()
        {
            _authGateway.LinkResultToReturn = AuthGatewayResult.Failure(
                PlayerIdentityFailureKind.AccountAlreadyLinked,
                "ACCOUNT_ALREADY_LINKED");
            await _service.LinkGooglePlayGamesAsync();
            Assert.AreEqual(PlayerIdentityStatus.Conflict, _service.Snapshot.Status);

            string originalPlayerId = _authGateway.PlayerId;
            _authGateway.LinkResultToReturn = AuthGatewayResult.Success(originalPlayerId);

            var keepResult = await _service.ResolveConflictAsync(AccountLinkConflictResolution.KeepCurrentProgress);

            Assert.IsTrue(keepResult.IsSuccess);
            Assert.AreEqual(PlayerIdentityStatus.Linked, _service.Snapshot.Status);
            Assert.IsTrue(_service.Snapshot.IsLinked);
            Assert.IsTrue(_authGateway.LastForceLinkOption);
            Assert.AreEqual(originalPlayerId, _authGateway.PlayerId);
        }

        [Test]
        public async Task ResolveConflictAsync_SwitchToExistingAccount_WhenPendingQueueNotEmpty_DrainsQueueAndSwitches()
        {
            _authGateway.LinkResultToReturn = AuthGatewayResult.Failure(
                PlayerIdentityFailureKind.AccountAlreadyLinked,
                "ACCOUNT_ALREADY_LINKED");
            await _service.LinkGooglePlayGamesAsync();
            Assert.AreEqual(PlayerIdentityStatus.Conflict, _service.Snapshot.Status);

            _submissionService.PendingCount = 3;
            _submissionService.DrainOnFlush = true;

            _authGateway.NewPlayerIdOnGoogleSignIn = "player_existing_cloud_2";

            var switchResult = await _service.ResolveConflictAsync(AccountLinkConflictResolution.SwitchToExistingAccount);

            Assert.IsTrue(switchResult.IsSuccess);
            Assert.AreEqual(PlayerIdentityStatus.Linked, _service.Snapshot.Status);
            Assert.AreEqual("player_existing_cloud_2", _authGateway.PlayerId);
            Assert.AreEqual(1, _cloudService.ResetCallCount);
            Assert.AreEqual(0, _submissionService.PendingCount);
        }

        [Test]
        public async Task ResolveConflictAsync_SwitchToExistingAccount_WhenPendingQueueFailsToDrain_BlocksSwitch()
        {
            _authGateway.LinkResultToReturn = AuthGatewayResult.Failure(
                PlayerIdentityFailureKind.AccountAlreadyLinked,
                "ACCOUNT_ALREADY_LINKED");
            await _service.LinkGooglePlayGamesAsync();
            Assert.AreEqual(PlayerIdentityStatus.Conflict, _service.Snapshot.Status);

            _submissionService.PendingCount = 5;
            _submissionService.DrainOnFlush = false;

            var switchResult = await _service.ResolveConflictAsync(AccountLinkConflictResolution.SwitchToExistingAccount);

            Assert.IsFalse(switchResult.IsSuccess);
            Assert.AreEqual(PlayerIdentityFailureKind.PendingResults, switchResult.FailureKind);
            Assert.AreEqual(PlayerIdentityStatus.Conflict, _service.Snapshot.Status);
            Assert.AreEqual(0, _cloudService.ResetCallCount);
            Assert.IsFalse(_authGateway.SignOutCalled);
        }

        [Test]
        public async Task ResolveConflictAsync_SwitchToExistingAccount_RollsBackToOriginalPlayer_WhenSignInFails()
        {
            _authGateway.LinkResultToReturn = AuthGatewayResult.Failure(
                PlayerIdentityFailureKind.AccountAlreadyLinked,
                "ACCOUNT_ALREADY_LINKED");
            await _service.LinkGooglePlayGamesAsync();
            Assert.AreEqual(PlayerIdentityStatus.Conflict, _service.Snapshot.Status);

            string originalPlayerId = _authGateway.PlayerId;

            _authGateway.SignInGoogleResultToReturn = AuthGatewayResult.Failure(
                PlayerIdentityFailureKind.Offline,
                "NETWORK_TIMEOUT");

            var switchResult = await _service.ResolveConflictAsync(AccountLinkConflictResolution.SwitchToExistingAccount);

            Assert.IsFalse(switchResult.IsSuccess);
            Assert.AreEqual(PlayerIdentityFailureKind.Offline, switchResult.FailureKind);
            Assert.AreEqual(PlayerIdentityStatus.Conflict, _service.Snapshot.Status);
            Assert.AreEqual(originalPlayerId, _authGateway.PlayerId);
            Assert.IsTrue(_authGateway.SignInAnonymouslyCalled);
        }

        #region Mocks and Fakes

        private sealed class FakeInitService : ICloudInitializationService
        {
            public CloudInitializationState State => IsReady ? CloudInitializationState.Ready : CloudInitializationState.Unavailable;
            public string EnvironmentName => "development";
            public string PlayerId => "init_player";
            public bool IsReady { get; set; }

            public FakeInitService(bool isReady)
            {
                IsReady = isReady;
            }

            public Task<bool> InitializeAsync() => Task.FromResult(IsReady);
        }

        private sealed class FakeAuthGateway : IPlayerIdentityAuthGateway
        {
            public bool IsSignedIn { get; set; }
            public string PlayerId { get; set; }
            public bool HasGooglePlayGamesIdentity { get; set; }
            public bool LastForceLinkOption { get; private set; }
            public bool SignOutCalled { get; private set; }
            public bool SignInAnonymouslyCalled { get; private set; }
            public string NewPlayerIdOnGoogleSignIn { get; set; } = "player_new_123";

            public AuthGatewayResult? LinkResultToReturn { get; set; }
            public AuthGatewayResult? SignInGoogleResultToReturn { get; set; }

            public FakeAuthGateway(string playerId, bool isSignedIn)
            {
                PlayerId = playerId;
                IsSignedIn = isSignedIn;
            }

            public Task<PlayerIdentityInfoResult> GetPlayerInfoAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(PlayerIdentityInfoResult.Success(HasGooglePlayGamesIdentity, PlayerId));
            }

            public Task<AuthGatewayResult> LinkWithGooglePlayGamesAsync(
                string authCode,
                bool forceLink,
                CancellationToken cancellationToken = default)
            {
                LastForceLinkOption = forceLink;
                if (LinkResultToReturn.HasValue)
                {
                    return Task.FromResult(LinkResultToReturn.Value);
                }

                HasGooglePlayGamesIdentity = true;
                return Task.FromResult(AuthGatewayResult.Success(PlayerId));
            }

            public Task<AuthGatewayResult> SignInWithGooglePlayGamesAsync(
                string authCode,
                CancellationToken cancellationToken = default)
            {
                if (SignInGoogleResultToReturn.HasValue)
                {
                    return Task.FromResult(SignInGoogleResultToReturn.Value);
                }

                PlayerId = NewPlayerIdOnGoogleSignIn;
                IsSignedIn = true;
                HasGooglePlayGamesIdentity = true;
                return Task.FromResult(AuthGatewayResult.Success(PlayerId));
            }

            public Task<AuthGatewayResult> SignInAnonymouslyAsync(CancellationToken cancellationToken = default)
            {
                SignInAnonymouslyCalled = true;
                IsSignedIn = true;
                return Task.FromResult(AuthGatewayResult.Success(PlayerId));
            }

            public void SignOut(bool clearCredentials)
            {
                SignOutCalled = true;
                IsSignedIn = false;
            }
        }

        private sealed class FakeAuthCodeProvider : IPlatformAuthCodeProvider
        {
            public bool IsSupported { get; set; }
            public string CodeToReturn { get; set; }
            public AuthCodeFetchResult? AuthResultToReturn { get; set; }

            public FakeAuthCodeProvider(bool isSupported, string code)
            {
                IsSupported = isSupported;
                CodeToReturn = code;
            }

            public Task<AuthCodeFetchResult> RequestServerAuthCodeAsync(
                bool isManual,
                CancellationToken cancellationToken = default)
            {
                if (AuthResultToReturn.HasValue)
                {
                    return Task.FromResult(AuthResultToReturn.Value);
                }

                return Task.FromResult(AuthCodeFetchResult.Success(CodeToReturn));
            }
        }

        private sealed class FakeCloudService : IPlayerCloudService
        {
            public CloudSyncState State => CloudSyncState.Ready;
            public GameCenterSnapshot CachedSnapshot => null;
            public int LoadCallCount { get; private set; }
            public int ResetCallCount { get; private set; }

            public Task<CloudOperationResult<GameCenterSnapshot>> LoadSnapshotAsync(CancellationToken cancellationToken = default)
            {
                LoadCallCount++;
                return Task.FromResult(CloudOperationResult<GameCenterSnapshot>.Success(null));
            }

            public Task<CloudOperationResult<SubmitGameResultResponse>> SubmitGameResultAsync(GameResult result, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(CloudOperationResult<SubmitGameResultResponse>.Success(null));
            }

            public Task<CloudOperationResult<GameCenterSnapshot>> ResetForIdentityChangeAsync(CancellationToken cancellationToken = default)
            {
                ResetCallCount++;
                return Task.FromResult(CloudOperationResult<GameCenterSnapshot>.Success(null));
            }
        }

        private sealed class FakeSubmissionService : IGameResultSubmissionService
        {
            public event Action<GameResultSubmissionOutcome> OnResultProcessed;
            public int PendingCount { get; set; }
            public bool DrainOnFlush { get; set; }

            public QueueEnqueueResult Enqueue(GameResult result)
            {
                PendingCount++;
                return QueueEnqueueResult.Success;
            }

            public Task FlushPendingAsync(CancellationToken cancellationToken = default)
            {
                if (DrainOnFlush)
                {
                    PendingCount = 0;
                }

                return Task.CompletedTask;
            }
        }

        #endregion
    }
}
