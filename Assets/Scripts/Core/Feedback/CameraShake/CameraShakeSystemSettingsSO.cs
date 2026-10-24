using UnityEngine;

namespace BlockBlast.Core.Feedback
{
    /// <summary>
    /// Stateless configuration asset defining global camera shake settings and defaults.
    /// </summary>
    [CreateAssetMenu(fileName = "CameraShakeSystemSettings", menuName = "Feedback/Camera Shake/System Settings")]
    public sealed class CameraShakeSystemSettingsSO : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Whether camera shake is enabled by default upon service initialization.")]
        private bool _enabledByDefault = true;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Global intensity multiplier applied to all camera shake playback [0..1].")]
        private float _globalIntensity = 1f;

        public bool EnabledByDefault => _enabledByDefault;
        public float GlobalIntensity => Mathf.Clamp01(_globalIntensity);

        public void InitializeForTesting(bool enabledByDefault, float globalIntensity)
        {
            _enabledByDefault = enabledByDefault;
            _globalIntensity = Mathf.Clamp01(globalIntensity);
        }
    }
}
