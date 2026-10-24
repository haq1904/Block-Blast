using UnityEngine;

namespace BlockBlast.Core.Feedback
{
    /// <summary>
    /// Allocation-free request structure passed to camera shake services and backends.
    /// </summary>
    public readonly struct CameraShakeRequest
    {
        public CameraShakeProfileSO Profile { get; }
        public float RuntimeIntensityScale { get; }
        public Vector3? ExplicitPosition { get; }
        public Vector3 ExplicitDirection { get; }
        public float EffectiveAmplitude { get; }

        public CameraShakeRequest(
            CameraShakeProfileSO profile,
            float runtimeIntensityScale = 1f,
            Vector3? explicitPosition = null,
            Vector3 explicitDirection = default,
            float effectiveAmplitude = 0f)
        {
            Profile = profile;
            RuntimeIntensityScale = runtimeIntensityScale;
            ExplicitPosition = explicitPosition;
            ExplicitDirection = explicitDirection;
            EffectiveAmplitude = effectiveAmplitude;
        }
    }
}
