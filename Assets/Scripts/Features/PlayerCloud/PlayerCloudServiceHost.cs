using System.IO;
using System.Threading;
using BlockBlast.Core;
using BlockBlast.Features.CloudServices;
using BlockBlast.Features.PlayerData;
using BlockBlast.Features.PlayerCloud.Gateway;
using BlockBlast.Features.PlayerCloud.Persistence;
using BlockBlast.Features.PlayerCloud.Retry;
using UnityEngine;

namespace BlockBlast.Features.PlayerCloud
{
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerCloudServiceHost : MonoBehaviour
    {
        private PlayerCloudService _service;
        private bool _ownsRegistration;
        private CancellationTokenSource _lifecycleCts;

        public bool OwnsRegistration => _ownsRegistration;

        private void Start()
        {
            if (ServiceLocator.TryGet<IPlayerCloudService>(out _))
            {
                Debug.LogWarning("[PlayerCloudServiceHost] Another IPlayerCloudService is already registered. Disabling duplicate host.", this);
                enabled = false;
                return;
            }

            if (!ServiceLocator.TryGet<ICloudInitializationService>(out var initService))
            {
                Debug.LogError("[PlayerCloudServiceHost] ICloudInitializationService not found in ServiceLocator.", this);
                return;
            }

            _lifecycleCts = new CancellationTokenSource();

            var serializer = new UnityJsonPlayerDataSerializer();
            string cacheRootDir = Path.Combine(
                Application.persistentDataPath,
                PlayerCloudConstants.PlayerDataDirectoryName);
            var cache = new FilePlayerCloudSnapshotCache(cacheRootDir, serializer);

            var cloudCodeGateway = new UnityPlayerCloudCodeGateway();
            var protectedGateway = new UnityProtectedPlayerDataGateway();
            var retryDelay = new TaskPlayerCloudRetryDelay();

            _service = new PlayerCloudService(
                initService,
                protectedGateway,
                cloudCodeGateway,
                cache,
                retryDelay,
                serializer);

            ServiceLocator.Register<IPlayerCloudService>(_service);
            _ownsRegistration = true;
        }

        private void OnDestroy()
        {
            _lifecycleCts?.Cancel();
            _lifecycleCts?.Dispose();
            _lifecycleCts = null;

            if (_ownsRegistration && _service != null)
            {
                ServiceLocator.Unregister<IPlayerCloudService>(_service);
                _ownsRegistration = false;
                _service.Dispose();
                _service = null;
            }
        }
    }
}
