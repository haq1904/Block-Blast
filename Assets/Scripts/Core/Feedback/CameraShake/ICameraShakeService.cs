namespace BlockBlast.Core.Feedback
{
    /// <summary>
    /// Service contract for triggering camera shake effects.
    /// </summary>
    public interface ICameraShakeService
    {
        bool Enabled { get; set; }
        bool IsSupported { get; }
        bool TryPlay(in CameraShakeRequest request);
    }
}
