using BlockBlast.Features.PlayerData;

namespace BlockBlast.Features.Lobby
{
    public sealed partial class LobbyController
    {
        private ILocalPlayerDataStore _localPlayerDataStore;

        private ILocalPlayerDataStore GetLocalPlayerDataStore()
        {
            if (_localPlayerDataStore == null)
            {
                _localPlayerDataStore = PlayerDataLocalStoreFactory.CreateDefault();
            }
            return _localPlayerDataStore;
        }

        public void SetLocalPlayerDataStoreForTest(ILocalPlayerDataStore store)
        {
            _localPlayerDataStore = store;
        }

        public void RefreshHighScoreForTest()
        {
            UpdateHighScoreDisplay();
        }
    }
}
