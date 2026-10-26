using UnityEngine;

namespace BlockBlast.Features.Feedback
{
    /// <summary>
    /// PlayerPrefs-backed store for feedback settings preferences with deferred saving.
    /// Defers PlayerPrefs.Save() until Flush() is explicitly called.
    /// </summary>
    public sealed class PlayerPrefsFeedbackSettingsStore : IFeedbackSettingsStore
    {
        private bool _isDirty;

        public bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                return defaultValue;
            }

            return PlayerPrefs.GetInt(key, defaultValue ? 1 : 0) == 1;
        }

        public void SetBool(string key, bool value)
        {
            int intValue = value ? 1 : 0;
            if (!PlayerPrefs.HasKey(key) || PlayerPrefs.GetInt(key) != intValue)
            {
                PlayerPrefs.SetInt(key, intValue);
                _isDirty = true;
            }
        }

        public void Flush()
        {
            if (_isDirty)
            {
                PlayerPrefs.Save();
                _isDirty = false;
            }
        }
    }
}
