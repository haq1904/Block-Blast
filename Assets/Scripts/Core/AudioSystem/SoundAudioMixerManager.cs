
using UnityEngine;
using UnityEngine.Audio;

public class SoundAudioMixerManager : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;


    public void ChangeMasterVolume(float value)
    {
        audioMixer.SetFloat("masterVolume", Mathf.Log10(value) * 20f);
    }

    public void ChangeSoundFXVolume(float value)
    {
        audioMixer.SetFloat("soundFXVolume", Mathf.Log10(value) * 20f);
    }

    public void ChangeMusicVolume(float value)
    {
        audioMixer.SetFloat("musicVolume", Mathf.Log10(value) * 20f);
    }


}
