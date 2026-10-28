using BlockBlast.Core.Persistence;
using UnityEngine;

namespace BlockBlast.Features.Feedback.Audio
{
    /// <summary>
    /// Pure domain service that orchestrates normalized music and SFX volume between
    /// an <see cref="IAudioMixerService"/> and an <see cref="IKeyValueStore"/>.
    /// </summary>
    public sealed class AudioVolumeSettingsService : IAudioVolumeSettingsService
    {
        public const string MusicVolumeKey = "Settings.Audio.MusicVolumeMilli.v1";
        public const string SfxVolumeKey = "Settings.Audio.SfxVolumeMilli.v1";
        public const int VolumeScale = 1000;

        private readonly IAudioMixerService _mixerService;
        private readonly IKeyValueStore _store;

        private int _currentMusicMilli;
        private int _currentSfxMilli;
        private bool _hasMusicMilli;
        private bool _hasSfxMilli;
        private bool _isDirty;

        public bool IsDirty => _isDirty;

        public float MusicVolume =>
            _mixerService != null ? _mixerService.GetChannelVolume(AudioChannels.BGMusicVolume) : 1f;

        public float SfxVolume =>
            _mixerService != null ? _mixerService.GetChannelVolume(AudioChannels.SoundFXVolume) : 1f;

        public AudioVolumeSettingsService(IAudioMixerService mixerService, IKeyValueStore store)
        {
            _mixerService = mixerService;
            _store = store;
        }

        public void LoadAndApply()
        {
            if (_store != null && _store.HasKey(MusicVolumeKey))
            {
                int raw = _store.GetInt(MusicVolumeKey);
                _currentMusicMilli = Mathf.Clamp(raw, 0, VolumeScale);
                _hasMusicMilli = true;
                _mixerService?.SetChannelVolume(AudioChannels.BGMusicVolume, _currentMusicMilli / (float)VolumeScale);
            }
            else if (_mixerService != null)
            {
                float current = _mixerService.GetChannelVolume(AudioChannels.BGMusicVolume);
                _currentMusicMilli = Mathf.Clamp(Mathf.RoundToInt(current * VolumeScale), 0, VolumeScale);
                _hasMusicMilli = true;
            }

            if (_store != null && _store.HasKey(SfxVolumeKey))
            {
                int raw = _store.GetInt(SfxVolumeKey);
                _currentSfxMilli = Mathf.Clamp(raw, 0, VolumeScale);
                _hasSfxMilli = true;
                _mixerService?.SetChannelVolume(AudioChannels.SoundFXVolume, _currentSfxMilli / (float)VolumeScale);
            }
            else if (_mixerService != null)
            {
                float current = _mixerService.GetChannelVolume(AudioChannels.SoundFXVolume);
                _currentSfxMilli = Mathf.Clamp(Mathf.RoundToInt(current * VolumeScale), 0, VolumeScale);
                _hasSfxMilli = true;
            }
        }

        public void SetMusicVolume(float normalizedVolume)
        {
            float clamped = Mathf.Clamp01(normalizedVolume);
            int milli = Mathf.RoundToInt(clamped * VolumeScale);

            if (_hasMusicMilli && milli == _currentMusicMilli)
            {
                return;
            }

            _currentMusicMilli = milli;
            _hasMusicMilli = true;

            _mixerService?.SetChannelVolume(AudioChannels.BGMusicVolume, milli / (float)VolumeScale);

            if (_store != null)
            {
                _store.SetInt(MusicVolumeKey, milli);
                _isDirty = true;
            }
        }

        public void SetSfxVolume(float normalizedVolume)
        {
            float clamped = Mathf.Clamp01(normalizedVolume);
            int milli = Mathf.RoundToInt(clamped * VolumeScale);

            if (_hasSfxMilli && milli == _currentSfxMilli)
            {
                return;
            }

            _currentSfxMilli = milli;
            _hasSfxMilli = true;

            _mixerService?.SetChannelVolume(AudioChannels.SoundFXVolume, milli / (float)VolumeScale);

            if (_store != null)
            {
                _store.SetInt(SfxVolumeKey, milli);
                _isDirty = true;
            }
        }

        public void Flush()
        {
            if (!_isDirty)
            {
                return;
            }

            _store?.Save();
            _isDirty = false;
        }
    }
}
