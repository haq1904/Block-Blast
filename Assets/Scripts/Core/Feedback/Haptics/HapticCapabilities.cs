using System;

namespace BlockBlast.Core.Feedback
{
    /// <summary>
    /// Capability flags exposed by haptic backends.
    /// </summary>
    [Flags]
    public enum HapticCapabilities
    {
        None = 0,
        BasicPulse = 1 << 0,
        Pattern = 1 << 1,
        Intensity = 1 << 2
    }
}
