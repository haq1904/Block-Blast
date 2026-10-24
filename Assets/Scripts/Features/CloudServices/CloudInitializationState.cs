namespace BlockBlast.Features.CloudServices
{
    /// <summary>
    /// Represents the high-level state of cloud services initialization.
    /// </summary>
    public enum CloudInitializationState
    {
        NotStarted,
        Initializing,
        Ready,
        Unavailable
    }
}
