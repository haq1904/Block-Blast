using System;
using System.Threading;
using System.Threading.Tasks;
using BlockBlast.Features.PlayerData;
using BlockBlast.Features.PlayerCloud.Gateway;
using BlockBlast.Features.PlayerCloud.Mapping;
using BlockBlast.Features.PlayerCloud.Persistence;

namespace BlockBlast.Features.PlayerCloud
{
    public sealed partial class PlayerCloudService
    {
        private PlayerCloudCacheScope? _activeScope;

        public PlayerCloudCacheScope? ActiveScope => _activeScope;

        private bool TryResolveActiveScope(out PlayerCloudCacheScope scope)
        {
            if (_initService.IsReady &&
                !string.IsNullOrWhiteSpace(_initService.EnvironmentName) &&
                !string.IsNullOrWhiteSpace(_initService.PlayerId))
            {
                try
                {
                    scope = new PlayerCloudCacheScope(_initService.EnvironmentName, _initService.PlayerId);
                    return true;
                }
                catch
                {
                    scope = default;
                    return false;
                }
            }

            scope = default;
            return false;
        }

        private void EnsureHydratedForScope(PlayerCloudCacheScope scope)
        {
            lock (_syncRoot)
            {
                if (_isHydrated && _activeScope.HasValue && _activeScope.Value == scope)
                {
                    return;
                }

                if (_activeScope.HasValue && _activeScope.Value != scope)
                {
                    CachedSnapshot = PlayerCloudContractMapper.CreateEmptySnapshot();
                    _currentLoadTask = null;
                }

                _activeScope = scope;
                _isHydrated = true;

                if (_cache != null && _cache.TryLoad(scope, out var loaded) && loaded != null)
                {
                    CachedSnapshot = loaded;
                    State = CloudSyncState.Ready;
                }
                else
                {
                    CachedSnapshot = PlayerCloudContractMapper.CreateEmptySnapshot();
                    State = CloudSyncState.Uninitialized;
                }
            }
        }

        private void SaveToActiveScopeCache(PlayerCloudCacheScope scope, GameCenterSnapshot snapshot)
        {
            if (_cache != null && snapshot != null)
            {
                _cache.Save(scope, snapshot);
            }
        }

        public async Task<CloudOperationResult<GameCenterSnapshot>> ResetForIdentityChangeAsync(
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return CloudOperationResult<GameCenterSnapshot>.Failure(
                    CloudFailureKind.Cancelled,
                    false,
                    PlayerCloudConstants.ErrorCodes.Cancelled);
            }

            Task<CloudOperationResult<GameCenterSnapshot>> runningLoadTask = null;
            lock (_syncRoot)
            {
                if (_currentLoadTask != null && !_currentLoadTask.IsCompleted)
                {
                    runningLoadTask = _currentLoadTask;
                }
            }

            if (runningLoadTask != null)
            {
                try
                {
                    await runningLoadTask;
                }
                catch
                {
                    // Ignore fault from in-flight load task while draining before reset
                }
            }

            await _operationGate.WaitAsync(cancellationToken);
            try
            {
                PlayerCloudCacheScope? scopeToClear;
                lock (_syncRoot)
                {
                    _currentLoadTask = null;
                    _isHydrated = false;
                    CachedSnapshot = PlayerCloudContractMapper.CreateEmptySnapshot();
                    State = CloudSyncState.Uninitialized;
                    scopeToClear = _activeScope;
                    _activeScope = null;
                }

                if (scopeToClear.HasValue && _cache != null)
                {
                    _cache.Clear(scopeToClear.Value);
                }

                return CloudOperationResult<GameCenterSnapshot>.Success(CachedSnapshot);
            }
            catch (Exception ex)
            {
                var failure = UnityCloudFailureClassifier.Classify(ex);
                return CloudOperationResult<GameCenterSnapshot>.Failure(
                    failure.Kind,
                    failure.IsRetryable,
                    failure.ErrorCode);
            }
            finally
            {
                _operationGate.Release();
            }
        }
    }
}
