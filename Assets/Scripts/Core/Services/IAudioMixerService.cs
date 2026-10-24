public interface IAudioMixerService
{
    void SetChannelVolume(AudioChannels channel, float normalizedVolume);
    float GetChannelVolume(AudioChannels channel);
}
