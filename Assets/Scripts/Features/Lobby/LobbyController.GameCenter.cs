namespace BlockBlast.Features.Lobby
{
    public sealed partial class LobbyController
    {
        public void SetGameCenterInteractionActive(bool isActive)
        {
            if (_playRequested) return;
            SetWorldButtonsInteractable(!isActive);
        }

        public void ReturnHomeFromGameCenter()
        {
            if (_playRequested) return;
            OnTabSelected(LobbyTabId.Home);
            SetWorldButtonsInteractable(true);
        }
    }
}
