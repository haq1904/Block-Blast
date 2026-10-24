using UnityEngine;

namespace BlockBlast.Core.Feedback
{
    /// <summary>
    /// Stateless configuration asset defining an individual haptic profile.
    /// </summary>
    [CreateAssetMenu(fileName = "HapticProfile", menuName = "Feedback/Haptics/Profile")]
    public sealed class HapticProfileSO : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Semantic haptic pattern represented by this profile.")]
        private HapticPattern _pattern = HapticPattern.ImpactLight;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Baseline normalized intensity for this profile [0..1].")]
        private float _intensity = 1f;

        public HapticPattern Pattern => _pattern;
        public float Intensity => Mathf.Clamp01(_intensity);

        public void InitializeForTesting(HapticPattern pattern, float intensity)
        {
            _pattern = pattern;
            _intensity = Mathf.Clamp01(intensity);
        }
    }
}
