using System.Runtime.InteropServices;
using BlockBlast.Core.Feedback;

namespace BlockBlast.Integrations.Haptics
{
    /// <summary>
    /// iOS platform haptic driver binding to native Core Haptics / UIKit plugin.
    /// Safely reports unsupported on non-iOS or Editor environments.
    /// </summary>
    internal sealed class IosHapticDriver : IHapticPlatformDriver
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void _BlockBlast_Haptics_Initialize();

        [DllImport("__Internal")]
        private static extern int _BlockBlast_Haptics_GetCapabilities();

        [DllImport("__Internal")]
        private static extern bool _BlockBlast_Haptics_Play(int pattern, float intensity);

        [DllImport("__Internal")]
        private static extern void _BlockBlast_Haptics_Shutdown();

        private readonly HapticCapabilities _capabilities;

        public HapticCapabilities Capabilities => _capabilities;

        public IosHapticDriver()
        {
            _BlockBlast_Haptics_Initialize();
            _capabilities = (HapticCapabilities)_BlockBlast_Haptics_GetCapabilities();
        }

        public bool TryPlay(in HapticRequest request)
        {
            return _BlockBlast_Haptics_Play((int)request.Pattern, request.Intensity);
        }

        public void Dispose()
        {
            _BlockBlast_Haptics_Shutdown();
        }
#else
        public HapticCapabilities Capabilities => HapticCapabilities.None;
        public bool TryPlay(in HapticRequest request) => false;
        public void Dispose() { }
#endif
    }
}
