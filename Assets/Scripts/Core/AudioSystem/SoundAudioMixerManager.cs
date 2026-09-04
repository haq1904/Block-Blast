using UnityEngine;
using UnityEngine.Audio;

public enum AudioChannels
{
    MasterVolume,
    SoundFXVolume,
    MusicVolume
}

public class SoundAudioMixerManager : MonoBehaviour, IAudioMixerService
{
    [SerializeField] private AudioMixer audioMixer;

    private void Awake()
    {
        ServiceLocator.Register<IAudioMixerService>(this);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<IAudioMixerService>();
    }

    public void ChangeChannelVolume(AudioChannels channel, float volume)
    {
        // Prevent log(0) which is -Infinity
        float clampedVolume = Mathf.Clamp(volume, 0.0001f, 1f);
        float dbValue = Mathf.Log10(clampedVolume) * 20f;

        switch (channel)
        {
            case AudioChannels.MasterVolume:
                audioMixer.SetFloat("masterVolume", dbValue);
                break;
            case AudioChannels.SoundFXVolume:
                audioMixer.SetFloat("soundFXVolume", dbValue);
                break;
            case AudioChannels.MusicVolume:
                audioMixer.SetFloat("musicVolume", dbValue);
                break;
        }
    }
}
