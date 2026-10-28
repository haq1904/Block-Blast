namespace BlockBlast.Features.Feedback
{
    /// <summary>
    /// Service interface for reading, modifying, and flushing haptic and camera shake feedback settings.
    /// </summary>
    public interface IFeedbackSettingsService
    {
        /// <summary>
        /// Whether the underlying haptic service is currently available in the runtime environment.
        /// </summary>
        bool IsHapticAvailable { get; }

        /// <summary>
        /// Current enabled state of player haptics.
        /// </summary>
        bool IsHapticEnabled { get; }

        /// <summary>
        /// Whether the underlying camera shake service is currently available in the runtime environment.
        /// </summary>
        bool IsCameraShakeAvailable { get; }

        /// <summary>
        /// Current enabled state of player camera shake.
        /// </summary>
        bool IsCameraShakeEnabled { get; }

        /// <summary>
        /// Sets the haptic enabled state and records the preference in the store.
        /// </summary>
        void SetHapticEnabled(bool enabled);

        /// <summary>
        /// Sets the camera shake enabled state and records the preference in the store.
        /// </summary>
        void SetCameraShakeEnabled(bool enabled);

        /// <summary>
        /// Attempts to bind any newly registered core feedback services (e.g. IHapticService or ICameraShakeService)
        /// and applies stored preferences to them without duplicate binding.
        /// </summary>
        void TryBindAvailableServices();

        /// <summary>
        /// Flushes pending changes to persistent storage if dirty.
        /// </summary>
        void Flush();
    }
}
