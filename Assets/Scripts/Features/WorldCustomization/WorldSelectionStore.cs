using BlockBlast.Core.Persistence;

namespace BlockBlast.Features.WorldCustomization
{
    public sealed class WorldSelectionStore : IWorldSelectionStore
    {
        public const string Key = "BlockBlast.WorldCustomization.SelectedWorld.v1";
        private readonly IKeyValueStore _store;

        public WorldSelectionStore(IKeyValueStore store = null)
        {
            _store = store ?? new PlayerPrefsKeyValueStore();
        }

        public string GetSelectedWorldId(string defaultWorldId)
        {
            if (_store == null) return defaultWorldId;
            string value = _store.GetString(Key, null);
            return string.IsNullOrEmpty(value) ? defaultWorldId : value;
        }

        public void SaveSelectedWorldId(string worldId)
        {
            if (_store == null || string.IsNullOrEmpty(worldId)) return;
            _store.SetString(Key, worldId);
            _store.Save();
        }
    }
}
