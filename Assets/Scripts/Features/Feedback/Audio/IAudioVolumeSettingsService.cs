namespace BlockBlast.Features.Feedback.Audio
{
    /// <summary>
    /// Service interface for reading, modifying, and flushing music and SFX volume settings.
    /// </summary>
    public interface IAudioVolumeSettingsService
    {
        /// <summary>
        /// Current normalized music volume (0..1).
        /// </summary>
        float MusicVolume { get; }

        /// <summary>
        /// Current normalized sound effects volume (0..1).
        /// </summary>
        float SfxVolume { get; }

        /// <summary>
        /// Sets the normalized music volume (0..1) and marks settings dirty if changed.
        /// </summary>
        void SetMusicVolume(float normalizedVolume);

        /// <summary>
        /// Sets the normalized sound effects volume (0..1) and marks settings dirty if changed.
        /// </summary>
        void SetSfxVolume(float normalizedVolume);

        /// <summary>
        /// Flushes pending changes to persistent storage if dirty.
        /// </summary>
        void Flush();
    }
}
