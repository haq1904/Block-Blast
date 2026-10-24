namespace BlockBlast.Core.Feedback
{
    /// <summary>
    /// Contract for technology/platform-specific haptic execution backends.
    /// </summary>
    public interface IHapticBackend
    {
        HapticCapabilities Capabilities { get; }
        bool TryPlay(in HapticRequest request);
    }
}
