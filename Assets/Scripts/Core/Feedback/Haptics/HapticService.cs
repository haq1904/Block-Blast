using System;
using UnityEngine;

namespace BlockBlast.Core.Feedback
{
    /// <summary>
    /// Pure haptic service implementing validation, cooldown throttling, and intensity normalization.
    /// </summary>
    public sealed class HapticService : IHapticService
    {
        private readonly IHapticBackend _backend;
        private readonly IFeedbackClock _clock;
        private readonly HapticSystemSettingsSO _settings;

        private double _lastPlayTime = double.MinValue;

        public bool Enabled { get; set; }

        public HapticCapabilities Capabilities => _backend != null ? _backend.Capabilities : HapticCapabilities.None;

        public HapticService(
            IHapticBackend backend,
            IFeedbackClock clock,
            HapticSystemSettingsSO settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _backend = backend ?? NullHapticBackend.Instance;
            _clock = clock ?? UnityRealtimeFeedbackClock.Instance;

            Enabled = _settings.EnabledByDefault;
        }

        public bool TryPlay(HapticProfileSO profile, float runtimeIntensityScale = 1f)
        {
            if (!Enabled || _backend.Capabilities == HapticCapabilities.None)
            {
                return false;
            }

            if (profile == null || profile.Pattern == HapticPattern.None)
            {
                return false;
            }

            if (runtimeIntensityScale <= 0f)
            {
                return false;
            }

            float effectiveIntensity = Mathf.Clamp01(_settings.GlobalIntensity * profile.Intensity * runtimeIntensityScale);
            if (effectiveIntensity <= 0f)
            {
                return false;
            }

            double now = _clock.RealtimeSinceStartup;
            double minInterval = _settings.MinimumIntervalSeconds;
            if (_lastPlayTime >= 0.0 && (now - _lastPlayTime) < minInterval)
            {
                return false;
            }

            HapticRequest request = new HapticRequest(profile.Pattern, effectiveIntensity);
            bool played = _backend.TryPlay(in request);
            if (played)
            {
                _lastPlayTime = now;
            }

            return played;
        }
    }
}
