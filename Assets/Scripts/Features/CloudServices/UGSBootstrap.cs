using UnityEngine;

namespace BlockBlast.Features.CloudServices
{
    /// <summary>
    /// Persistent bootstrap host for Unity Gaming Services and anonymous authentication.
    /// Resides on SceneServices composition root and manages ICloudInitializationService registration.
    /// </summary>
    public sealed class UGSBootstrap : MonoBehaviour
    {
        private ICloudInitializationService _service;
        private bool _ownsRegistration;

        public bool OwnsRegistration => _ownsRegistration;

        private void Awake()
        {
            if (ServiceLocator.TryGet<ICloudInitializationService>(out _))
            {
                Debug.LogWarning("[UGSBootstrap] Another ICloudInitializationService is already registered. Disabling duplicate host.", this);
                enabled = false;
                return;
            }

            string environment = CloudEnvironmentResolver.Resolve(Application.isEditor, Debug.isDebugBuild);
            _service = new CloudInitializationService(new UnityServicesGateway(), environment);
            ServiceLocator.Register<ICloudInitializationService>(_service);
            _ownsRegistration = true;
        }

        private async void Start()
        {
            if (!_ownsRegistration || _service == null)
            {
                return;
            }

            bool success = await _service.InitializeAsync();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (success)
            {
                Debug.Log($"[UGSBootstrap] Initialized environment '{_service.EnvironmentName}'.", this);
            }
            else
            {
                Debug.LogWarning($"[UGSBootstrap] Failed to initialize environment '{_service.EnvironmentName}'. Operating in offline mode.", this);
            }
#else
            if (!success)
            {
                Debug.LogWarning("[UGSBootstrap] Cloud services initialization failed. Operating in offline mode.", this);
            }
#endif
        }

        private void OnDestroy()
        {
            if (_ownsRegistration && _service != null)
            {
                ServiceLocator.Unregister<ICloudInitializationService>(_service);
                _ownsRegistration = false;
            }
        }
    }
}
