using System;
using System.Threading;
using System.Threading.Tasks;
using BlockBlast.Features.CloudServices;
using BlockBlast.Features.PlayerCloud;
using BlockBlast.Features.PlayerIdentity.Gateway;
using BlockBlast.Features.PlayerSession;

namespace BlockBlast.Features.PlayerIdentity
{
    public sealed class PlayerIdentityService : IPlayerIdentityService, IDisposable
    {
        private const int MaxPendingDrainBatches = 10;

        private readonly ICloudInitializationService _initService;
        private readonly IPlayerIdentityAuthGateway _authGateway;
        private readonly IPlatformAuthCodeProvider _authCodeProvider;
        private readonly IPlayerCloudService _cloudService;
        private readonly IGameResultSubmissionService _submissionService;

        private readonly SemaphoreSlim _operationGate = new SemaphoreSlim(1, 1);
        private readonly object _lock = new object();

        private PlayerIdentitySnapshot _snapshot = PlayerIdentitySnapshot.Unavailable();

        public event Action<PlayerIdentitySnapshot> StateChanged;

        public PlayerIdentitySnapshot Snapshot
        {
            get
            {
                lock (_lock)
                {
                    return _snapshot;
                }
            }
        }

        public PlayerIdentityService(
            ICloudInitializationService initService,
            IPlayerIdentityAuthGateway authGateway,
            IPlatformAuthCodeProvider authCodeProvider,
            IPlayerCloudService cloudService,
            IGameResultSubmissionService submissionService)
        {
            _initService = initService ?? throw new ArgumentNullException(nameof(initService));
            _authGateway = authGateway ?? throw new ArgumentNullException(nameof(authGateway));
            _authCodeProvider = authCodeProvider ?? throw new ArgumentNullException(nameof(authCodeProvider));
            _cloudService = cloudService ?? throw new ArgumentNullException(nameof(cloudService));
            _submissionService = submissionService ?? throw new ArgumentNullException(nameof(submissionService));
        }

        public async Task<PlayerIdentityResult> RefreshAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return PlayerIdentityResult.Failure(Snapshot, PlayerIdentityFailureKind.Cancelled, "CANCELLED");
            }

            await _operationGate.WaitAsync(cancellationToken);
            try
            {
                if (!_initService.IsReady || !_authGateway.IsSignedIn)
                {
                    UpdateSnapshot(PlayerIdentitySnapshot.Unavailable("NOT_INITIALIZED"));
                    return PlayerIdentityResult.Success(Snapshot);
                }

                var infoResult = await _authGateway.GetPlayerInfoAsync(cancellationToken);
                if (!infoResult.IsSuccess)
                {
                    UpdateSnapshot(PlayerIdentitySnapshot.Unavailable(infoResult.ErrorCode));
                    return PlayerIdentityResult.Failure(Snapshot, infoResult.FailureKind, infoResult.ErrorCode);
                }

                if (infoResult.HasGooglePlayGamesIdentity)
                {
                    UpdateSnapshot(PlayerIdentitySnapshot.Linked());
                }
                else
                {
                    UpdateSnapshot(PlayerIdentitySnapshot.Anonymous());
                }

                return PlayerIdentityResult.Success(Snapshot);
            }
            catch (Exception ex)
            {
                var failureKind = ex is OperationCanceledException
                    ? PlayerIdentityFailureKind.Cancelled
                    : PlayerIdentityFailureKind.Unknown;
                return PlayerIdentityResult.Failure(Snapshot, failureKind, ex.Message);
            }
            finally
            {
                _operationGate.Release();
            }
        }

        public async Task<PlayerIdentityResult> LinkGooglePlayGamesAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return PlayerIdentityResult.Failure(Snapshot, PlayerIdentityFailureKind.Cancelled, "CANCELLED");
            }

            await _operationGate.WaitAsync(cancellationToken);
            try
            {
                if (!_authCodeProvider.IsSupported)
                {
                    return PlayerIdentityResult.Failure(
                        Snapshot,
                        PlayerIdentityFailureKind.UnsupportedPlatform,
                        "UNSUPPORTED_PLATFORM");
                }

                bool initialized = await _initService.InitializeAsync();
                if (!initialized || !_authGateway.IsSignedIn)
                {
                    return PlayerIdentityResult.Failure(
                        Snapshot,
                        PlayerIdentityFailureKind.AuthenticationUnavailable,
                        "AUTH_NOT_READY");
                }

                string playerIdBefore = _authGateway.PlayerId;
                UpdateSnapshot(PlayerIdentitySnapshot.Linking());

                var codeResult = await _authCodeProvider.RequestServerAuthCodeAsync(true, cancellationToken);
                if (!codeResult.IsSuccess)
                {
                    UpdateSnapshot(PlayerIdentitySnapshot.Anonymous());
                    return PlayerIdentityResult.Failure(Snapshot, codeResult.FailureKind, codeResult.ErrorCode);
                }

                var linkResult = await _authGateway.LinkWithGooglePlayGamesAsync(
                    codeResult.AuthCode,
                    forceLink: false,
                    cancellationToken);

                if (!linkResult.IsSuccess)
                {
                    if (linkResult.FailureKind == PlayerIdentityFailureKind.AccountAlreadyLinked)
                    {
                        UpdateSnapshot(PlayerIdentitySnapshot.Conflict());
                        return PlayerIdentityResult.Failure(
                            Snapshot,
                            PlayerIdentityFailureKind.AccountAlreadyLinked,
                            "ACCOUNT_ALREADY_LINKED");
                    }

                    UpdateSnapshot(PlayerIdentitySnapshot.Anonymous());
                    return PlayerIdentityResult.Failure(Snapshot, linkResult.FailureKind, linkResult.ErrorCode);
                }

                if (_authGateway.PlayerId != playerIdBefore)
                {
                    UpdateSnapshot(PlayerIdentitySnapshot.Error(
                        PlayerIdentityFailureKind.InvalidResponse,
                        "PLAYER_ID_MISMATCH"));
                    return PlayerIdentityResult.Failure(
                        Snapshot,
                        PlayerIdentityFailureKind.InvalidResponse,
                        "PLAYER_ID_MISMATCH");
                }

                await _cloudService.LoadSnapshotAsync(cancellationToken);
                UpdateSnapshot(PlayerIdentitySnapshot.Linked());
                return PlayerIdentityResult.Success(Snapshot);
            }
            catch (Exception ex)
            {
                var failureKind = ex is OperationCanceledException
                    ? PlayerIdentityFailureKind.Cancelled
                    : PlayerIdentityFailureKind.Unknown;
                UpdateSnapshot(PlayerIdentitySnapshot.Anonymous());
                return PlayerIdentityResult.Failure(Snapshot, failureKind, ex.Message);
            }
            finally
            {
                _operationGate.Release();
            }
        }

        public async Task<PlayerIdentityResult> ResolveConflictAsync(
            AccountLinkConflictResolution resolution,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return PlayerIdentityResult.Failure(Snapshot, PlayerIdentityFailureKind.Cancelled, "CANCELLED");
            }

            await _operationGate.WaitAsync(cancellationToken);
            try
            {
                if (Snapshot.Status != PlayerIdentityStatus.Conflict)
                {
                    return PlayerIdentityResult.Failure(
                        Snapshot,
                        PlayerIdentityFailureKind.InvalidResponse,
                        "NOT_IN_CONFLICT_STATE");
                }

                switch (resolution)
                {
                    case AccountLinkConflictResolution.Cancel:
                        UpdateSnapshot(PlayerIdentitySnapshot.Anonymous());
                        return PlayerIdentityResult.Success(Snapshot);

                    case AccountLinkConflictResolution.KeepCurrentProgress:
                        return await ExecuteKeepCurrentProgressAsync(cancellationToken);

                    case AccountLinkConflictResolution.SwitchToExistingAccount:
                        return await ExecuteSwitchToExistingAccountAsync(cancellationToken);

                    default:
                        return PlayerIdentityResult.Failure(
                            Snapshot,
                            PlayerIdentityFailureKind.InvalidResponse,
                            "UNKNOWN_RESOLUTION");
                }
            }
            finally
            {
                _operationGate.Release();
            }
        }

        private async Task<PlayerIdentityResult> ExecuteKeepCurrentProgressAsync(CancellationToken cancellationToken)
        {
            string playerIdBefore = _authGateway.PlayerId;
            UpdateSnapshot(PlayerIdentitySnapshot.Linking());

            var codeResult = await _authCodeProvider.RequestServerAuthCodeAsync(true, cancellationToken);
            if (!codeResult.IsSuccess)
            {
                UpdateSnapshot(PlayerIdentitySnapshot.Conflict());
                return PlayerIdentityResult.Failure(Snapshot, codeResult.FailureKind, codeResult.ErrorCode);
            }

            var linkResult = await _authGateway.LinkWithGooglePlayGamesAsync(
                codeResult.AuthCode,
                forceLink: true,
                cancellationToken);

            if (!linkResult.IsSuccess)
            {
                UpdateSnapshot(PlayerIdentitySnapshot.Conflict());
                return PlayerIdentityResult.Failure(Snapshot, linkResult.FailureKind, linkResult.ErrorCode);
            }

            if (_authGateway.PlayerId != playerIdBefore)
            {
                UpdateSnapshot(PlayerIdentitySnapshot.Error(
                    PlayerIdentityFailureKind.InvalidResponse,
                    "PLAYER_ID_MISMATCH"));
                return PlayerIdentityResult.Failure(
                    Snapshot,
                    PlayerIdentityFailureKind.InvalidResponse,
                    "PLAYER_ID_MISMATCH");
            }

            await _cloudService.LoadSnapshotAsync(cancellationToken);
            UpdateSnapshot(PlayerIdentitySnapshot.Linked());
            return PlayerIdentityResult.Success(Snapshot);
        }

        private async Task<PlayerIdentityResult> ExecuteSwitchToExistingAccountAsync(CancellationToken cancellationToken)
        {
            if (_submissionService.PendingCount > 0)
            {
                int previousCount = _submissionService.PendingCount;
                for (int i = 0; i < MaxPendingDrainBatches; i++)
                {
                    await _submissionService.FlushPendingAsync(cancellationToken);
                    if (_submissionService.PendingCount == 0 || _submissionService.PendingCount >= previousCount)
                    {
                        break;
                    }

                    previousCount = _submissionService.PendingCount;
                }

                if (_submissionService.PendingCount > 0)
                {
                    UpdateSnapshot(PlayerIdentitySnapshot.Conflict());
                    return PlayerIdentityResult.Failure(
                        Snapshot,
                        PlayerIdentityFailureKind.PendingResults,
                        "PENDING_RESULTS_REMAINING");
                }
            }

            UpdateSnapshot(PlayerIdentitySnapshot.Switching());

            await _cloudService.ResetForIdentityChangeAsync(cancellationToken);

            string previousPlayerId = _authGateway.PlayerId;
            _authGateway.SignOut(clearCredentials: false);

            var codeResult = await _authCodeProvider.RequestServerAuthCodeAsync(true, cancellationToken);
            if (!codeResult.IsSuccess)
            {
                await RollbackSwitchAsync(previousPlayerId, cancellationToken);
                UpdateSnapshot(PlayerIdentitySnapshot.Conflict());
                return PlayerIdentityResult.Failure(Snapshot, codeResult.FailureKind, codeResult.ErrorCode);
            }

            var signInResult = await _authGateway.SignInWithGooglePlayGamesAsync(
                codeResult.AuthCode,
                cancellationToken);

            if (!signInResult.IsSuccess)
            {
                bool restored = await RollbackSwitchAsync(previousPlayerId, cancellationToken);
                if (restored)
                {
                    UpdateSnapshot(PlayerIdentitySnapshot.Conflict());
                    return PlayerIdentityResult.Failure(Snapshot, signInResult.FailureKind, signInResult.ErrorCode);
                }

                UpdateSnapshot(PlayerIdentitySnapshot.Error(
                    PlayerIdentityFailureKind.AuthenticationUnavailable,
                    "ROLLBACK_FAILED"));
                return PlayerIdentityResult.Failure(
                    Snapshot,
                    PlayerIdentityFailureKind.AuthenticationUnavailable,
                    "ROLLBACK_FAILED");
            }

            if (string.IsNullOrEmpty(_authGateway.PlayerId) || _authGateway.PlayerId == previousPlayerId)
            {
                UpdateSnapshot(PlayerIdentitySnapshot.Error(
                    PlayerIdentityFailureKind.InvalidResponse,
                    "INVALID_NEW_PLAYER_ID"));
                return PlayerIdentityResult.Failure(
                    Snapshot,
                    PlayerIdentityFailureKind.InvalidResponse,
                    "INVALID_NEW_PLAYER_ID");
            }

            await _cloudService.LoadSnapshotAsync(cancellationToken);
            UpdateSnapshot(PlayerIdentitySnapshot.Linked());
            return PlayerIdentityResult.Success(Snapshot);
        }

        private async Task<bool> RollbackSwitchAsync(string previousPlayerId, CancellationToken cancellationToken)
        {
            var restoreResult = await _authGateway.SignInAnonymouslyAsync(cancellationToken);
            if (restoreResult.IsSuccess && _authGateway.PlayerId == previousPlayerId)
            {
                await _cloudService.LoadSnapshotAsync(cancellationToken);
                return true;
            }

            return false;
        }

        private void UpdateSnapshot(PlayerIdentitySnapshot newSnapshot)
        {
            lock (_lock)
            {
                _snapshot = newSnapshot;
            }

            StateChanged?.Invoke(newSnapshot);
        }

        public void Dispose()
        {
            _operationGate?.Dispose();
        }
    }
}
