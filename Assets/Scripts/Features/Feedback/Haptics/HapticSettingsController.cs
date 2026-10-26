using BlockBlast.Core.Feedback;

namespace BlockBlast.Features.Feedback
{
    /// <summary>
    /// Controller for managing player haptic settings and preference persistence.
    /// </summary>
    public sealed class HapticSettingsController
    {
        public const string PreferenceKey = "Settings.Haptics.Enabled";

        private readonly IHapticService _service;
        private readonly IFeedbackSettingsStore _store;

        public bool IsAvailable => _service != null;
        public bool IsEnabled => _service != null && _service.Enabled;

        public HapticSettingsController(IHapticService service, IFeedbackSettingsStore store)
        {
            _service = service;
            _store = store;
        }

        public bool LoadAndApply()
        {
            if (_service == null)
            {
                return false;
            }

            bool enabled;
            if (_store != null && _store.HasKey(PreferenceKey))
            {
                enabled = _store.GetBool(PreferenceKey, _service.Enabled);
            }
            else
            {
                enabled = _service.Enabled;
            }

            _service.Enabled = enabled;
            return enabled;
        }

        public void SetEnabled(bool enabled)
        {
            if (_service != null)
            {
                _service.Enabled = enabled;
            }

            _store?.SetBool(PreferenceKey, enabled);
        }

        public void Flush()
        {
            _store?.Flush();
        }
    }
}
