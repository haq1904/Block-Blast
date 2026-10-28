using BlockBlast.Core.Persistence;
using UnityEngine;

namespace BlockBlast.Features.Feedback.Audio
{
    /// <summary>
    /// Lifecycle host attached to SoundAudioMixerManager that initializes, registers,
    /// and flushes <see cref="IAudioVolumeSettingsService"/> across application lifecycles.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SoundAudioMixerManager))]
    [DefaultExecutionOrder(-100)]
    public sealed class AudioVolumeSettingsServiceHost : MonoBehaviour
    {
        private AudioVolumeSettingsService _service;
        private bool _hasAppliedStoredVolumes;
        private bool _isRegistered;

        public IAudioVolumeSettingsService Service => _service;

        private void Awake()
        {
            var mixerService = GetComponent<IAudioMixerService>();
            var store = new PlayerPrefsKeyValueStore();
            _service = new AudioVolumeSettingsService(mixerService, store);

            ServiceLocator.Register<IAudioVolumeSettingsService>(_service);
            _isRegistered = true;
        }

        private void Start()
        {
            if (_hasAppliedStoredVolumes || _service == null)
            {
                return;
            }

            _service.LoadAndApply();
            _hasAppliedStoredVolumes = true;
        }

        private void OnDestroy()
        {
            _service?.Flush();

            if (_isRegistered && _service != null)
            {
                ServiceLocator.Unregister<IAudioVolumeSettingsService>(_service);
                _isRegistered = false;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                _service?.Flush();
            }
        }

        private void OnApplicationQuit()
        {
            _service?.Flush();
        }
    }
}
