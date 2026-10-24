using System;
using UnityEngine;

namespace BlockBlast.Core.Feedback
{
    /// <summary>
    /// Pure camera shake service responsible for capability gating, validation, and amplitude calculation.
    /// </summary>
    public sealed class CameraShakeService : ICameraShakeService
    {
        private readonly ICameraShakeBackend _backend;
        private readonly CameraShakeSystemSettingsSO _settings;

        public bool Enabled { get; set; }

        public bool IsSupported => _backend != null && _backend.IsSupported;

        public CameraShakeService(
            ICameraShakeBackend backend,
            CameraShakeSystemSettingsSO settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _backend = backend ?? NullCameraShakeBackend.Instance;

            Enabled = _settings.EnabledByDefault;
        }

        public bool TryPlay(in CameraShakeRequest request)
        {
            if (!Enabled || _backend == null || !_backend.IsSupported)
            {
                return false;
            }

            if (request.Profile == null)
            {
                return false;
            }

            if (request.RuntimeIntensityScale <= 0f)
            {
                return false;
            }

            float normalizedScale = Mathf.Clamp01(request.RuntimeIntensityScale);
            float effectiveAmplitude = request.Profile.BaseAmplitude * _settings.GlobalIntensity * normalizedScale;
            if (effectiveAmplitude <= 0f)
            {
                return false;
            }

            CameraShakeRequest normalizedRequest = new CameraShakeRequest(
                request.Profile,
                normalizedScale,
                request.ExplicitPosition,
                request.ExplicitDirection,
                effectiveAmplitude);

            return _backend.TryPlay(in normalizedRequest);
        }
    }
}
