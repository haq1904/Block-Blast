namespace BlockBlast.Features.Feedback
{
    /// <summary>
    /// Contract for storing and retrieving boolean preferences for feedback features.
    /// Exposes only boolean accessors and deferred persistence flush.
    /// </summary>
    public interface IFeedbackSettingsStore
    {
        bool HasKey(string key);
        bool GetBool(string key, bool defaultValue = false);
        void SetBool(string key, bool value);
        void Flush();
    }
}
