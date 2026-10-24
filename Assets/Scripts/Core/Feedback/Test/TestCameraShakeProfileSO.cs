using BlockBlast.Core.Feedback;

namespace BlockBlast.Core.Feedback.Test
{
    public sealed class TestCameraShakeProfileSO : CameraShakeProfileSO
    {
        public void Initialize(float baseAmplitude)
        {
            SetBaseAmplitudeForTesting(baseAmplitude);
        }
    }
}
