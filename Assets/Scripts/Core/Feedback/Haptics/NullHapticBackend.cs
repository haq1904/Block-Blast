namespace BlockBlast.Core.Feedback
{
    /// <summary>
    /// Safe no-op backend used when haptic feedback is unsupported or unassigned.
    /// </summary>
    public sealed class NullHapticBackend : IHapticBackend
    {
        public static NullHapticBackend Instance { get; } = new NullHapticBackend();

        private NullHapticBackend()
        {
        }

        public HapticCapabilities Capabilities => HapticCapabilities.None;

        public bool TryPlay(in HapticRequest request)
        {
            return false;
        }
    }
}
