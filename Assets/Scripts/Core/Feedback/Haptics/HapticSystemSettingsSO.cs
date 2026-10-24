using UnityEngine;

namespace BlockBlast.Core.Feedback
{
    /// <summary>
    /// Stateless configuration asset defining global haptic settings and defaults.
    /// </summary>
    [CreateAssetMenu(fileName = "HapticSystemSettings", menuName = "Feedback/Haptics/System Settings")]
    public sealed class HapticSystemSettingsSO : ScriptableObject
    {
        [SerializeField]
        [Tooltip("Whether haptic feedback is enabled by default upon service initialization.")]
        private bool _enabledByDefault = true;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Global intensity multiplier applied to all haptic playback [0..1].")]
        private float _globalIntensity = 1f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Minimum time interval in seconds between consecutive haptic playback requests to prevent spamming.")]
        private float _minimumIntervalSeconds = 0.05f;

        public bool EnabledByDefault => _enabledByDefault;
        public float GlobalIntensity => Mathf.Clamp01(_globalIntensity);
        public float MinimumIntervalSeconds => Mathf.Max(0f, _minimumIntervalSeconds);

        public void InitializeForTesting(bool enabledByDefault, float globalIntensity, float minimumIntervalSeconds)
        {
            _enabledByDefault = enabledByDefault;
            _globalIntensity = Mathf.Clamp01(globalIntensity);
            _minimumIntervalSeconds = Mathf.Max(0f, minimumIntervalSeconds);
        }
    }
}
