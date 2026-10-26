namespace BlockBlast.Core.Persistence
{
    public interface IKeyValueStore
    {
        bool HasKey(string key);
        int GetInt(string key, int defaultValue = 0);
        void SetInt(string key, int value);
        string GetString(string key, string defaultValue = null);
        void SetString(string key, string value);
        void DeleteKey(string key);
        void Save();
    }
}
