using UnityEngine;

namespace BlockBlast.Core.Feedback
{
    /// <summary>
    /// MonoBehaviour host managing lifecycle and ServiceLocator registration of IHapticService.
    /// </summary>
    public sealed class HapticServiceHost : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Global haptic system settings asset.")]
        private HapticSystemSettingsSO _settings;

        [SerializeField]
        [Tooltip("Optional custom MonoBehaviour implementing IHapticBackend. If not set, falls back to UnityHandheldHapticBackend.")]
        private MonoBehaviour _customBackend;

        private IHapticService _service;
        private bool _ownsRegistration;

        public IHapticService Service => _service;
        public bool OwnsRegistration => _ownsRegistration;

        private void Awake()
        {
            if (ServiceLocator.TryGet<IHapticService>(out _))
            {
                Debug.LogWarning("[HapticServiceHost] Another IHapticService is already registered. Disabling duplicate host.", this);
                enabled = false;
                return;
            }

            if (_settings == null)
            {
                Debug.LogError("[HapticServiceHost] Missing HapticSystemSettingsSO configuration asset. Cannot initialize IHapticService.", this);
                enabled = false;
                return;
            }

            IHapticBackend backend = _customBackend as IHapticBackend;
            if (backend == null)
            {
                backend = UnityHandheldHapticBackend.Instance;
            }

            _service = new HapticService(backend, UnityRealtimeFeedbackClock.Instance, _settings);
            ServiceLocator.Register<IHapticService>(_service);
            _ownsRegistration = true;
        }

        private void OnDestroy()
        {
            if (_ownsRegistration && _service != null)
            {
                ServiceLocator.Unregister<IHapticService>(_service);
                _ownsRegistration = false;
                _service = null;
            }
        }

        public void InitializeForTesting(HapticSystemSettingsSO settings, MonoBehaviour customBackend)
        {
            _settings = settings;
            _customBackend = customBackend;
        }
    }
}
