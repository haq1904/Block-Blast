namespace BlockBlast.Core.Feedback
{
    /// <summary>
    /// Service contract for triggering haptic feedback.
    /// </summary>
    public interface IHapticService
    {
        bool Enabled { get; set; }
        HapticCapabilities Capabilities { get; }
        bool TryPlay(HapticProfileSO profile, float runtimeIntensityScale = 1f);
    }
}
