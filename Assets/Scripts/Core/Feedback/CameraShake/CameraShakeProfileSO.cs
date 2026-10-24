using UnityEngine;

namespace BlockBlast.Core.Feedback
{
    /// <summary>
    /// Abstract reusable base class for camera shake profiles.
    /// Technology-specific parameters are defined in backend-specific subclasses.
    /// </summary>
    public abstract class CameraShakeProfileSO : ScriptableObject
    {
        [SerializeField]
        [Min(0f)]
        [Tooltip("Baseline amplitude for this shake profile.")]
        private float _baseAmplitude = 0.5f;

        public float BaseAmplitude => Mathf.Max(0f, _baseAmplitude);

        public void SetBaseAmplitudeForTesting(float baseAmplitude)
        {
            _baseAmplitude = Mathf.Max(0f, baseAmplitude);
        }
    }
}
