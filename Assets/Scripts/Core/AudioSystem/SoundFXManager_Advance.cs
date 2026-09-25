using System;
using UnityEngine;

public enum SoundFXType
{
    Click,
    BlockSpawn,
    BlockPlace,
    LineClear,
    GameOver
}

[RequireComponent(typeof(AudioSource))]
public class SoundFXManager : MonoBehaviour, ISoundFXService
{
    [SerializeField] private SoundList[] soundList;
    private AudioSource audioSource;

    private void Awake()
    {
        ServiceLocator.Register<ISoundFXService>(this);
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<ISoundFXService>();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        string[] name = Enum.GetNames(typeof(SoundFXType));

        if (soundList == null)
        {
            soundList = new SoundList[name.Length];
        }
        else if (soundList.Length != name.Length)
        {
            Array.Resize(ref soundList, name.Length);
        }

        for (int i = 0; i < name.Length; i++)
        {
            soundList[i].name = name[i];
        }
    }
#endif

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }



    public void PlaySound(SoundFXType soundType)
    {
        PlaySound(soundType, 1f);
    }

    public void PlaySound(SoundFXType soundType, float volume)
    {
        if (soundList == null || (int)soundType < 0 || (int)soundType >= soundList.Length) return;
        AudioClip[] clips = soundList[(int)soundType].Sounds;
        if (clips == null || clips.Length == 0) return;
        AudioClip clipToPlay = clips[UnityEngine.Random.Range(0, clips.Length)];
        if (clipToPlay != null && audioSource != null)
        {
            audioSource.PlayOneShot(clipToPlay, Mathf.Clamp01(volume));
        }
    }

    public void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, Mathf.Clamp01(volume));
        }
    }
}
[Serializable]
public struct SoundList
{
    public AudioClip[] Sounds { get => sounds; }
    [HideInInspector] public string name;
    [SerializeField] private AudioClip[] sounds;
}
