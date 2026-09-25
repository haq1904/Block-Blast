public interface ISoundFXService
{
    void PlaySound(SoundFXType soundType);
    void PlaySound(SoundFXType soundType, float volume);
    void PlaySound(UnityEngine.AudioClip clip, float volume = 1f);
}
