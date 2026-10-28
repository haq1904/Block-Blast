using System;
using System.Threading;
using System.Threading.Tasks;
using BlockBlast.Features.CloudServices;
using BlockBlast.Features.PlayerData;
using BlockBlast.Features.PlayerCloud.Gateway;
using BlockBlast.Features.PlayerCloud.Mapping;
using BlockBlast.Features.PlayerCloud.Persistence;
using BlockBlast.Features.PlayerCloud.Retry;
namespace BlockBlast.Features.PlayerCloud
{
    public sealed partial class PlayerCloudService : IPlayerCloudService, IDisposable
    {
        private readonly ICloudInitializationService _initService;
        private readonly IProtectedPlayerDataGateway _protectedGateway;
        private readonly IPlayerCloudCodeGateway _cloudCodeGateway;
        private readonly IPlayerCloudSnapshotCache _cache;
        private readonly IPlayerCloudRetryDelay _retryDelay;
        private readonly IPlayerDataSerializer _serializer;

        private readonly SemaphoreSlim _operationGate = new SemaphoreSlim(1, 1);
        private readonly object _syncRoot = new object();

        private Task<CloudOperationResult<GameCenterSnapshot>> _currentLoadTask;
        private bool _isHydrated;

        public CloudSyncState State { get; private set; } = CloudSyncState.Uninitialized;
        public GameCenterSnapshot CachedSnapshot { get; private set; }

        public PlayerCloudService(
            ICloudInitializationService initService,
            IProtectedPlayerDataGateway protectedGateway,
            IPlayerCloudCodeGateway cloudCodeGateway,
            IPlayerCloudSnapshotCache cache,
            IPlayerCloudRetryDelay retryDelay,
            IPlayerDataSerializer serializer)
        {
            _initService = initService ?? throw new ArgumentNullException(nameof(initService));
            _protectedGateway = protectedGateway ?? throw new ArgumentNullException(nameof(protectedGateway));
            _cloudCodeGateway = cloudCodeGateway ?? throw new ArgumentNullException(nameof(cloudCodeGateway));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _retryDelay = retryDelay ?? throw new ArgumentNullException(nameof(retryDelay));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

            CachedSnapshot = PlayerCloudContractMapper.CreateEmptySnapshot();
        }

        public async Task<CloudOperationResult<GameCenterSnapshot>> LoadSnapshotAsync(
            CancellationToken cancellationToken = default)
        {

            if (cancellationToken.IsCancellationRequested)
            {
                return CloudOperationResult<GameCenterSnapshot>.Failure(
                    CloudFailureKind.Cancelled,
                    false,
                    PlayerCloudConstants.ErrorCodes.Cancelled);
            }

            Task<CloudOperationResult<GameCenterSnapshot>> loadTask;
            lock (_syncRoot)
            {
                if (_currentLoadTask == null || _currentLoadTask.IsCompleted)
                {
                    _currentLoadTask = ExecuteLoadSnapshotAsync();
                }

                loadTask = _currentLoadTask;
            }

            try
            {
                if (cancellationToken.CanBeCanceled)
                {
                    var cancelTcs = new TaskCompletionSource<bool>();
                    using (cancellationToken.Register(() => cancelTcs.TrySetResult(true)))
                    {
                        var completedTask = await Task.WhenAny(loadTask, cancelTcs.Task);
                        if (completedTask == cancelTcs.Task)
                        {
                            _ = loadTask.ContinueWith(t => { var _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
                            return CloudOperationResult<GameCenterSnapshot>.Failure(
                                CloudFailureKind.Cancelled,
                                false,
                                PlayerCloudConstants.ErrorCodes.Cancelled);
                        }
                    }
                }

                return await loadTask;
            }
            catch (Exception ex)
            {
                var failure = UnityCloudFailureClassifier.Classify(ex);
                return CloudOperationResult<GameCenterSnapshot>.Failure(
                    failure.Kind,
                    failure.IsRetryable,
                    failure.ErrorCode,
                    failure.RetryAfterSeconds);
            }
        }

        public async Task<CloudOperationResult<SubmitGameResultResponse>> SubmitGameResultAsync(
            GameResult result,
            CancellationToken cancellationToken = default)
        {
            if (result == null)
            {
                return CloudOperationResult<SubmitGameResultResponse>.Failure(
                    CloudFailureKind.InvalidPayload,
                    false,
                    PlayerCloudConstants.ErrorCodes.InvalidPayload);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return CloudOperationResult<SubmitGameResultResponse>.Failure(
                    CloudFailureKind.Cancelled,
                    false,
                    PlayerCloudConstants.ErrorCodes.Cancelled);
            }

            bool initReady;
            try
            {
                initReady = await _initService.InitializeAsync();
            }
            catch (Exception ex)
            {
                var classified = UnityCloudFailureClassifier.Classify(ex);
                lock (_syncRoot)
                {
                    State = classified.Kind == CloudFailureKind.Offline ? CloudSyncState.Offline : CloudSyncState.Error;
                }

                return CloudOperationResult<SubmitGameResultResponse>.Failure(
                    classified.Kind,
                    classified.IsRetryable,
                    classified.ErrorCode,
                    classified.RetryAfterSeconds);
            }

            if (!initReady)
            {
                lock (_syncRoot)
                {
                    State = CloudSyncState.Offline;
                }

                return CloudOperationResult<SubmitGameResultResponse>.Failure(
                    CloudFailureKind.AuthenticationUnavailable,
                    false,
                    PlayerCloudConstants.ErrorCodes.Unauthenticated);
            }

            if (!TryResolveActiveScope(out var scope))
            {
                lock (_syncRoot)
                {
                    State = CloudSyncState.Offline;
                }

                return CloudOperationResult<SubmitGameResultResponse>.Failure(
                    CloudFailureKind.AuthenticationUnavailable,
                    false,
                    PlayerCloudConstants.ErrorCodes.Unauthenticated);
            }

            EnsureHydratedForScope(scope);

            try
            {
                await _operationGate.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return CloudOperationResult<SubmitGameResultResponse>.Failure(
                    CloudFailureKind.Cancelled,
                    false,
                    PlayerCloudConstants.ErrorCodes.Cancelled);
            }

            try
            {
                lock (_syncRoot)
                {
                    State = CloudSyncState.Synchronizing;
                }

                var request = PlayerCloudContractMapper.ToGeneratedRequest(result);

                for (int attempt = 1; attempt <= PlayerCloudConstants.MaxRetryAttempts; attempt++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return CloudOperationResult<SubmitGameResultResponse>.Failure(
                            CloudFailureKind.Cancelled,
                            false,
                            PlayerCloudConstants.ErrorCodes.Cancelled);
                    }

                    Task<Unity.Services.CloudCode.GeneratedBindings.CubePuzzle3DPlayerData.Contracts.SubmitGameResultResponse> submitTask;
                    try
                    {
                        submitTask = _cloudCodeGateway.SubmitGameResultAsync(request);
                    }
                    catch (Exception ex)
                    {
                        var classified = UnityCloudFailureClassifier.Classify(ex);
                        if (classified.IsRetryable && attempt < PlayerCloudConstants.MaxRetryAttempts)
                        {
                            int delayMs = GetRetryDelayMs(attempt, classified.RetryAfterSeconds);
                            try
                            {
                                await _retryDelay.DelayAsync(delayMs, cancellationToken);
                            }
                            catch (OperationCanceledException)
                            {
                                return CloudOperationResult<SubmitGameResultResponse>.Failure(
                                    CloudFailureKind.Cancelled,
                                    false,
                                    PlayerCloudConstants.ErrorCodes.Cancelled);
                            }

                            continue;
                        }

                        lock (_syncRoot)
                        {
                            State = classified.Kind == CloudFailureKind.Offline ? CloudSyncState.Offline : CloudSyncState.Error;
                        }

                        return CloudOperationResult<SubmitGameResultResponse>.Failure(
                            classified.Kind,
                            classified.IsRetryable,
                            classified.ErrorCode,
                            classified.RetryAfterSeconds);
                    }

                    Unity.Services.CloudCode.GeneratedBindings.CubePuzzle3DPlayerData.Contracts.SubmitGameResultResponse responseDto;
                    try
                    {
                        responseDto = await submitTask;
                    }
                    catch (Exception ex)
                    {
                        var classified = UnityCloudFailureClassifier.Classify(ex);
                        if (classified.IsRetryable && attempt < PlayerCloudConstants.MaxRetryAttempts)
                        {
                            int delayMs = GetRetryDelayMs(attempt, classified.RetryAfterSeconds);
                            try
                            {
                                await _retryDelay.DelayAsync(delayMs, cancellationToken);
                            }
                            catch (OperationCanceledException)
                            {
                                return CloudOperationResult<SubmitGameResultResponse>.Failure(
                                    CloudFailureKind.Cancelled,
                                    false,
                                    PlayerCloudConstants.ErrorCodes.Cancelled);
                            }

                            continue;
                        }

                        lock (_syncRoot)
                        {
                            State = classified.Kind == CloudFailureKind.Offline ? CloudSyncState.Offline : CloudSyncState.Error;
                        }

                        return CloudOperationResult<SubmitGameResultResponse>.Failure(
                            classified.Kind,
                            classified.IsRetryable,
                            classified.ErrorCode,
                            classified.RetryAfterSeconds);
                    }

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return CloudOperationResult<SubmitGameResultResponse>.Failure(
                            CloudFailureKind.Cancelled,
                            false,
                            PlayerCloudConstants.ErrorCodes.Cancelled);
                    }

                    var mappedResult = PlayerCloudContractMapper.MapServerStatusToResult(responseDto);

                    if (mappedResult.IsSuccess)
                    {
                        lock (_syncRoot)
                        {
                            var updated = mappedResult.Value.UpdatedSnapshot;
                            if (updated.Revision >= CachedSnapshot.Revision)
                            {
                                CachedSnapshot = updated;
                                SaveToActiveScopeCache(scope, updated);
                            }

                            State = CloudSyncState.Ready;
                        }

                        return mappedResult;
                    }

                    if (mappedResult.IsRetryable && attempt < PlayerCloudConstants.MaxRetryAttempts)
                    {
                        int delayMs = GetRetryDelayMs(attempt, mappedResult.RetryAfterSeconds);
                        try
                        {
                            await _retryDelay.DelayAsync(delayMs, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            return CloudOperationResult<SubmitGameResultResponse>.Failure(
                                CloudFailureKind.Cancelled,
                                false,
                                PlayerCloudConstants.ErrorCodes.Cancelled);
                        }

                        continue;
                    }

                    lock (_syncRoot)
                    {
                        State = mappedResult.FailureKind == CloudFailureKind.Offline ? CloudSyncState.Offline : CloudSyncState.Error;
                    }

                    return mappedResult;
                }

                lock (_syncRoot)
                {
                    State = CloudSyncState.Error;
                }

                return CloudOperationResult<SubmitGameResultResponse>.Failure(
                    CloudFailureKind.ServiceUnavailable,
                    true,
                    PlayerCloudConstants.ErrorCodes.ServiceUnavailable);
            }
            finally
            {
                _operationGate.Release();
            }
        }

        public void Dispose()
        {
            _operationGate?.Dispose();
        }

        private async Task<CloudOperationResult<GameCenterSnapshot>> ExecuteLoadSnapshotAsync()
        {
            bool initReady;
            try
            {
                initReady = await _initService.InitializeAsync();
            }
            catch (Exception ex)
            {
                var classified = UnityCloudFailureClassifier.Classify(ex);
                lock (_syncRoot)
                {
                    State = classified.Kind == CloudFailureKind.Offline ? CloudSyncState.Offline : CloudSyncState.Error;
                }

                return CloudOperationResult<GameCenterSnapshot>.Failure(
                    classified.Kind,
                    classified.IsRetryable,
                    classified.ErrorCode,
                    classified.RetryAfterSeconds);
            }

            if (!initReady)
            {
                lock (_syncRoot)
                {
                    State = CloudSyncState.Offline;
                }

                return CloudOperationResult<GameCenterSnapshot>.Failure(
                    CloudFailureKind.AuthenticationUnavailable,
                    false,
                    PlayerCloudConstants.ErrorCodes.Unauthenticated);
            }

            if (!TryResolveActiveScope(out var scope))
            {
                lock (_syncRoot)
                {
                    State = CloudSyncState.Offline;
                }

                return CloudOperationResult<GameCenterSnapshot>.Failure(
                    CloudFailureKind.AuthenticationUnavailable,
                    false,
                    PlayerCloudConstants.ErrorCodes.Unauthenticated);
            }

            EnsureHydratedForScope(scope);

            await _operationGate.WaitAsync();
            try
            {
                lock (_syncRoot)
                {
                    State = CloudSyncState.Synchronizing;
                }

                for (int attempt = 1; attempt <= PlayerCloudConstants.MaxRetryAttempts; attempt++)
                {
                    try
                    {
                        var readResult = await _protectedGateway.LoadProtectedSnapshotJsonAsync();
                        GameCenterSnapshot snapshot;

                        if (!readResult.Found || string.IsNullOrEmpty(readResult.JsonValue))
                        {
                            snapshot = PlayerCloudContractMapper.CreateEmptySnapshot();
                        }
                        else
                        {
                            snapshot = _serializer.DeserializeSnapshot(readResult.JsonValue);
                            if (snapshot == null || snapshot.SchemaVersion != PlayerDataSchema.CurrentVersion)
                            {
                                lock (_syncRoot)
                                {
                                    State = CloudSyncState.Error;
                                }

                                return CloudOperationResult<GameCenterSnapshot>.Failure(
                                    CloudFailureKind.InvalidResponse,
                                    false,
                                    PlayerCloudConstants.ErrorCodes.InvalidResponse);
                            }
                        }

                        lock (_syncRoot)
                        {
                            if (snapshot.Revision >= CachedSnapshot.Revision)
                            {
                                CachedSnapshot = snapshot;
                                SaveToActiveScopeCache(scope, snapshot);
                            }

                            State = CloudSyncState.Ready;
                        }

                        return CloudOperationResult<GameCenterSnapshot>.Success(snapshot);
                    }
                    catch (Exception ex)
                    {
                        var classified = UnityCloudFailureClassifier.Classify(ex);
                        if (classified.IsRetryable && attempt < PlayerCloudConstants.MaxRetryAttempts)
                        {
                            int delayMs = GetRetryDelayMs(attempt, classified.RetryAfterSeconds);
                            await _retryDelay.DelayAsync(delayMs, CancellationToken.None);
                            continue;
                        }

                        lock (_syncRoot)
                        {
                            State = classified.Kind == CloudFailureKind.Offline ? CloudSyncState.Offline : CloudSyncState.Error;
                        }

                        return CloudOperationResult<GameCenterSnapshot>.Failure(
                            classified.Kind,
                            classified.IsRetryable,
                            classified.ErrorCode,
                            classified.RetryAfterSeconds);
                    }
                }

                lock (_syncRoot)
                {
                    State = CloudSyncState.Error;
                }

                return CloudOperationResult<GameCenterSnapshot>.Failure(
                    CloudFailureKind.ServiceUnavailable,
                    true,
                    PlayerCloudConstants.ErrorCodes.ServiceUnavailable);
            }
            finally
            {
                _operationGate.Release();
            }
        }

        private static int GetRetryDelayMs(int attempt, int? retryAfterSeconds)
        {
            if (retryAfterSeconds.HasValue && retryAfterSeconds.Value > 0)
            {
                int cappedSeconds = Math.Min(retryAfterSeconds.Value, PlayerCloudConstants.MaxRetryAfterCapSeconds);
                return cappedSeconds * 1000;
            }

            return attempt == 1
                ? PlayerCloudConstants.DefaultRetryDelay1Ms
                : PlayerCloudConstants.DefaultRetryDelay2Ms;
        }
    }
}
