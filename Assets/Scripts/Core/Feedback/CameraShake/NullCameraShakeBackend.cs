namespace BlockBlast.Core.Feedback
{
    /// <summary>
    /// Safe no-op backend used when camera shake is unsupported or unassigned.
    /// </summary>
    public sealed class NullCameraShakeBackend : ICameraShakeBackend
    {
        public static NullCameraShakeBackend Instance { get; } = new NullCameraShakeBackend();

        private NullCameraShakeBackend()
        {
        }

        public bool IsSupported => false;

        public bool TryPlay(in CameraShakeRequest request)
        {
            return false;
        }
    }
}
