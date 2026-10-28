using BlockBlast.Core.Feedback;
using BlockBlast.Features.Feedback;
using BlockBlast.Features.Feedback.Audio;
using BlockBlast.Features.GameFlow.Audio;
using UnityEngine;

namespace BlockBlast.Features.Lobby
{
    public sealed partial class LobbyController
    {
        public void SyncAudioSettingsFromService()
        {
            var settings = SettingsOverlay;
            if (settings == null) return;

            var audioService = GetAudioVolumeSettingsService();
            if (audioService == null) return;

            float music = audioService.MusicVolume;
            float sfx = audioService.SfxVolume;
            settings.SetAudioSliderValues(music, sfx);
        }

        private IAudioVolumeSettingsService GetAudioVolumeSettingsService()
        {
            if (_audioVolumeSettingsService == null)
            {
                ServiceLocator.TryGet<IAudioVolumeSettingsService>(out _audioVolumeSettingsService);
            }
            return _audioVolumeSettingsService;
        }

        public void SetAudioVolumeSettingsServiceForTest(IAudioVolumeSettingsService audioVolumeSettingsService)
        {
            _audioVolumeSettingsService = audioVolumeSettingsService;
        }

        public void FlushAudioSettings()
        {
            _audioVolumeSettingsService?.Flush();
        }

        public void FlushFeedbackSettings()
        {
            _feedbackSettingsService?.Flush();
        }

        private IFeedbackSettingsService GetFeedbackSettingsService()
        {
            if (_feedbackSettingsService == null)
            {
                ServiceLocator.TryGet<IFeedbackSettingsService>(out _feedbackSettingsService);
            }
            return _feedbackSettingsService;
        }

        public void SetFeedbackSettingsServiceForTest(IFeedbackSettingsService feedbackSettingsService)
        {
            _feedbackSettingsService = feedbackSettingsService;
        }

        public void SetFeedbackSettingsForTest(
            IHapticService hapticService,
            ICameraShakeService cameraShakeService,
            IFeedbackSettingsStore store)
        {
            var service = new FeedbackSettingsService(store, () => hapticService, () => cameraShakeService);
            service.TryBindAvailableServices();
            _feedbackSettingsService = service;
            SyncFeedbackSettingsToView();
        }

        private void HandleMusicVolumeChanged(float value) =>
            GetAudioVolumeSettingsService()?.SetMusicVolume(value);

        private void HandleSfxVolumeChanged(float value) =>
            GetAudioVolumeSettingsService()?.SetSfxVolume(value);

        private void HandleHapticEnabledChanged(bool enabled) =>
            GetFeedbackSettingsService()?.SetHapticEnabled(enabled);

        private void HandleCameraShakeEnabledChanged(bool enabled) =>
            GetFeedbackSettingsService()?.SetCameraShakeEnabled(enabled);

        public void SyncFeedbackSettingsToView()
        {
            var settings = SettingsOverlay;
            if (settings == null) return;

            var feedbackService = GetFeedbackSettingsService();
            if (feedbackService == null)
            {
                settings.SetHapticEnabled(false, false);
                settings.SetCameraShakeEnabled(false, false);
                return;
            }

            settings.SetHapticEnabled(feedbackService.IsHapticEnabled, feedbackService.IsHapticAvailable);
            settings.SetCameraShakeEnabled(feedbackService.IsCameraShakeEnabled, feedbackService.IsCameraShakeAvailable);
        }
    }
}
