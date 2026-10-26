using System;
using BlockBlast.Core.Feedback;

namespace BlockBlast.Integrations.Haptics
{
    /// <summary>
    /// Internal driver seam for mobile haptic platform implementations.
    /// </summary>
    internal interface IHapticPlatformDriver : IDisposable
    {
        HapticCapabilities Capabilities { get; }
        bool TryPlay(in HapticRequest request);
    }
}
