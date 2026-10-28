using System;
using BlockBlast.Core.Feedback;

namespace BlockBlast.Features.Feedback
{
    /// <summary>
    /// Pure domain service that orchestrates player feedback preferences (Haptic and Camera Shake)
    /// across service lifetimes, supporting independent late binding and deferred disk flushing.
    /// </summary>
    public sealed class FeedbackSettingsService : IFeedbackSettingsService
    {
        private readonly IFeedbackSettingsStore _store;
        private readonly Func<IHapticService> _hapticResolver;
        private readonly Func<ICameraShakeService> _cameraResolver;

        private HapticSettingsController _hapticController;
        private CameraShakeSettingsController _cameraController;
        private bool _isHapticBound;
        private bool _isCameraBound;

        public bool IsHapticAvailable => _hapticController != null && _hapticController.IsAvailable;

        public bool IsHapticEnabled
        {
            get
            {
                if (_hapticController != null)
                {
                    return _hapticController.IsEnabled;
                }

                if (_store != null && _store.HasKey(HapticSettingsController.PreferenceKey))
                {
                    return _store.GetBool(HapticSettingsController.PreferenceKey);
                }

                return true;
            }
        }

        public bool IsCameraShakeAvailable => _cameraController != null && _cameraController.IsAvailable;

        public bool IsCameraShakeEnabled
        {
            get
            {
                if (_cameraController != null)
                {
                    return _cameraController.IsEnabled;
                }

                if (_store != null && _store.HasKey(CameraShakeSettingsController.PreferenceKey))
                {
                    return _store.GetBool(CameraShakeSettingsController.PreferenceKey);
                }

                return true;
            }
        }

        public bool AllServicesBound => _isHapticBound && _isCameraBound;

        public FeedbackSettingsService(
            IFeedbackSettingsStore store,
            Func<IHapticService> hapticResolver = null,
            Func<ICameraShakeService> cameraResolver = null)
        {
            _store = store;
            _hapticResolver = hapticResolver ?? DefaultHapticResolver;
            _cameraResolver = cameraResolver ?? DefaultCameraResolver;
        }

        public void TryBindAvailableServices()
        {
            if (!_isHapticBound)
            {
                var haptic = _hapticResolver?.Invoke();
                if (haptic != null)
                {
                    _hapticController = new HapticSettingsController(haptic, _store);
                    _hapticController.LoadAndApply();
                    _isHapticBound = true;
                }
            }

            if (!_isCameraBound)
            {
                var camera = _cameraResolver?.Invoke();
                if (camera != null)
                {
                    _cameraController = new CameraShakeSettingsController(camera, _store);
                    _cameraController.LoadAndApply();
                    _isCameraBound = true;
                }
            }
        }

        public void SetHapticEnabled(bool enabled)
        {
            if (_hapticController != null)
            {
                _hapticController.SetEnabled(enabled);
            }
            else
            {
                _store?.SetBool(HapticSettingsController.PreferenceKey, enabled);
            }
        }

        public void SetCameraShakeEnabled(bool enabled)
        {
            if (_cameraController != null)
            {
                _cameraController.SetEnabled(enabled);
            }
            else
            {
                _store?.SetBool(CameraShakeSettingsController.PreferenceKey, enabled);
            }
        }

        public void Flush()
        {
            _store?.Flush();
        }

        private static IHapticService DefaultHapticResolver()
        {
            return ServiceLocator.TryGet<IHapticService>(out var service) ? service : null;
        }

        private static ICameraShakeService DefaultCameraResolver()
        {
            return ServiceLocator.TryGet<ICameraShakeService>(out var service) ? service : null;
        }
    }
}
