using BlockBlast.Core.Persistence;

namespace BlockBlast.Features.PlayerData
{
    public static class PlayerDataLocalStoreFactory
    {
        public static ILocalPlayerDataStore CreateDefault()
        {
            return new LocalPlayerDataStore(new PlayerPrefsKeyValueStore());
        }
    }
}
