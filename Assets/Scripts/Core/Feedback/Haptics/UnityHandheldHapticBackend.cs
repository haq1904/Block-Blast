using UnityEngine;

namespace BlockBlast.Core.Feedback
{
    /// <summary>
    /// Fallback haptic backend utilizing UnityEngine.Handheld.Vibrate on Android/iOS devices.
    /// Safely reports unsupported on Editor/Desktop platforms.
    /// </summary>
    public sealed class UnityHandheldHapticBackend : IHapticBackend
    {
        public static UnityHandheldHapticBackend Instance { get; } = new UnityHandheldHapticBackend();

        private UnityHandheldHapticBackend()
        {
        }

        public HapticCapabilities Capabilities
        {
            get
            {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
                return HapticCapabilities.BasicPulse;
#else
                return HapticCapabilities.None;
#endif
            }
        }

        public bool TryPlay(in HapticRequest request)
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Handheld.Vibrate();
            return true;
#else
            return false;
#endif
        }
    }
}
