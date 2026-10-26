using BlockBlast.Core.Feedback;

namespace BlockBlast.Integrations.Haptics
{
    /// <summary>
    /// Fallback driver for Editor and unsupported platforms that performs no haptics.
    /// </summary>
    internal sealed class NullHapticDriver : IHapticPlatformDriver
    {
        public static NullHapticDriver Instance { get; } = new NullHapticDriver();

        public HapticCapabilities Capabilities => HapticCapabilities.None;

        public bool TryPlay(in HapticRequest request) => false;

        public void Dispose()
        {
        }
    }
}
