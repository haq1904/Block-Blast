using UnityEngine;
using UnityEngine.Audio;

public enum AudioChannels
{
    MasterVolume,
    SoundFXVolume,
    BGMusicVolume
}

public class SoundAudioMixerManager : MonoBehaviour, IAudioMixerService
{
    private const string MasterVolumeParam = "masterVolume";
    private const string BgmVolumeParam = "bgmVolume";
    private const string SoundFxVolumeParam = "soundFXVolume";
    private const float MinDb = -80f;

    [SerializeField] private AudioMixer audioMixer;

    private void Awake()
    {
        ServiceLocator.Register<IAudioMixerService>(this);
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<IAudioMixerService>();
    }

    public void SetChannelVolume(AudioChannels channel, float normalizedVolume)
    {
        if (audioMixer == null)
        {
            return;
        }

        string paramName = GetParameterName(channel);
        if (string.IsNullOrEmpty(paramName))
        {
            return;
        }

        float clamped = Mathf.Clamp01(normalizedVolume);
        float dbValue = clamped <= 0.0001f ? MinDb : Mathf.Log10(clamped) * 20f;
        audioMixer.SetFloat(paramName, dbValue);
    }

    public float GetChannelVolume(AudioChannels channel)
    {
        if (audioMixer == null)
        {
            return 1f;
        }

        string paramName = GetParameterName(channel);
        if (string.IsNullOrEmpty(paramName) || !audioMixer.GetFloat(paramName, out float dbValue))
        {
            return 1f;
        }

        if (dbValue <= MinDb)
        {
            return 0f;
        }

        return Mathf.Clamp01(Mathf.Pow(10f, dbValue / 20f));
    }

    private static string GetParameterName(AudioChannels channel)
    {
        switch (channel)
        {
            case AudioChannels.MasterVolume:
                return MasterVolumeParam;
            case AudioChannels.BGMusicVolume:
                return BgmVolumeParam;
            case AudioChannels.SoundFXVolume:
                return SoundFxVolumeParam;
            default:
                return null;
        }
    }
}
