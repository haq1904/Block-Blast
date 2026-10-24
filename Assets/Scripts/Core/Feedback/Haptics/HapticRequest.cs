namespace BlockBlast.Core.Feedback
{
    /// <summary>
    /// Allocation-free request passed to haptic backends.
    /// </summary>
    public readonly struct HapticRequest
    {
        public HapticPattern Pattern { get; }
        public float Intensity { get; }

        public HapticRequest(HapticPattern pattern, float intensity)
        {
            Pattern = pattern;
            Intensity = intensity;
        }
    }
}
