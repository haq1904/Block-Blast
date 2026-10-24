namespace BlockBlast.Core.Feedback
{
    /// <summary>
    /// Contract for camera shake technology adapters.
    /// </summary>
    public interface ICameraShakeBackend
    {
        bool IsSupported { get; }
        bool TryPlay(in CameraShakeRequest request);
    }
}
