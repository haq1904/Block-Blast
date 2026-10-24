using UnityEngine;

namespace BlockBlast.Core.Feedback
{
    /// <summary>
    /// MonoBehaviour host managing lifecycle and ServiceLocator registration of ICameraShakeService.
    /// </summary>
    public sealed class CameraShakeServiceHost : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Global camera shake system settings asset.")]
        private CameraShakeSystemSettingsSO _settings;

        [SerializeField]
        [Tooltip("Component implementing ICameraShakeBackend. If not set, falls back to NullCameraShakeBackend.")]
        private MonoBehaviour _backendComponent;

        private ICameraShakeService _service;
        private bool _ownsRegistration;

        public ICameraShakeService Service => _service;
        public bool OwnsRegistration => _ownsRegistration;

        private void Awake()
        {
            if (ServiceLocator.TryGet<ICameraShakeService>(out _))
            {
                Debug.LogWarning("[CameraShakeServiceHost] Another ICameraShakeService is already registered. Disabling duplicate host.", this);
                enabled = false;
                return;
            }

            if (_settings == null)
            {
                Debug.LogError("[CameraShakeServiceHost] Missing CameraShakeSystemSettingsSO configuration asset. Cannot initialize ICameraShakeService.", this);
                enabled = false;
                return;
            }

            ICameraShakeBackend backend = _backendComponent as ICameraShakeBackend;
            if (backend == null)
            {
                backend = NullCameraShakeBackend.Instance;
            }

            _service = new CameraShakeService(backend, _settings);
            ServiceLocator.Register<ICameraShakeService>(_service);
            _ownsRegistration = true;
        }

        private void OnDestroy()
        {
            if (_ownsRegistration && _service != null)
            {
                ServiceLocator.Unregister<ICameraShakeService>(_service);
                _ownsRegistration = false;
                _service = null;
            }
        }

        public void InitializeForTesting(CameraShakeSystemSettingsSO settings, MonoBehaviour backendComponent)
        {
            _settings = settings;
            _backendComponent = backendComponent;
        }
    }
}
