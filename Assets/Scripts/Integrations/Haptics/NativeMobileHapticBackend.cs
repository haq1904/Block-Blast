using BlockBlast.Core.Feedback;
using UnityEngine;

namespace BlockBlast.Integrations.Haptics
{
    /// <summary>
    /// Native mobile haptic backend executing feedback via platform-specific drivers (Android/iOS).
    /// Safely no-ops with Capabilities.None on Editor and desktop platforms.
    /// </summary>
    public sealed class NativeMobileHapticBackend : MonoBehaviour, IHapticBackend
    {
        private IHapticPlatformDriver _driver;

        public HapticCapabilities Capabilities => _driver != null ? _driver.Capabilities : HapticCapabilities.None;

        private void Awake()
        {
            InitializeDriver();
        }

        private void OnDestroy()
        {
            _driver?.Dispose();
            _driver = null;
        }

        private void InitializeDriver()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            _driver = new AndroidHapticDriver();
#elif UNITY_IOS && !UNITY_EDITOR
            _driver = new IosHapticDriver();
#else
            _driver = NullHapticDriver.Instance;
#endif
        }

        public bool TryPlay(in HapticRequest request)
        {
            if (_driver == null || _driver.Capabilities == HapticCapabilities.None)
            {
                return false;
            }

            return _driver.TryPlay(in request);
        }

        internal void SetDriverForTesting(IHapticPlatformDriver driver)
        {
            _driver?.Dispose();
            _driver = driver;
        }
    }
}
