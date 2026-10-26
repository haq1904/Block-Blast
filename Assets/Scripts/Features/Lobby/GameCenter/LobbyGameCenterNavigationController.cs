using UnityEngine;

namespace BlockBlast.Features.Lobby.GameCenter
{
    /// <summary>
    /// Coordinates navigation between the Player Center panel and Lobby home.
    /// Manages world button interaction state during panel activation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LobbyGameCenterNavigationController : MonoBehaviour
    {
        [SerializeField] private LobbyGameCenterPanelView _panelView;
        [SerializeField] private LobbyController _lobbyController;

        public LobbyGameCenterPanelView PanelView
        {
            get => _panelView;
            set => _panelView = value;
        }

        public LobbyController LobbyController
        {
            get => _lobbyController;
            set => _lobbyController = value;
        }

        private void Awake()
        {
            if (_panelView == null)
            {
                _panelView = GetComponent<LobbyGameCenterPanelView>();
            }

            if (_lobbyController == null)
            {
                _lobbyController = Object.FindAnyObjectByType<LobbyController>();
            }
        }

        private void OnEnable()
        {
            if (_panelView != null)
            {
                _panelView.BackRequested += HandleBackRequested;
            }

            if (_lobbyController != null)
            {
                _lobbyController.SetGameCenterInteractionActive(true);
            }
        }

        private void OnDisable()
        {
            if (_panelView != null)
            {
                _panelView.BackRequested -= HandleBackRequested;
            }

            if (_lobbyController != null)
            {
                _lobbyController.SetGameCenterInteractionActive(false);
            }
        }

        public void Construct(LobbyGameCenterPanelView panelView, LobbyController lobbyController)
        {
            _panelView = panelView;
            _lobbyController = lobbyController;
        }

        private void HandleBackRequested()
        {
            if (_lobbyController != null)
            {
                _lobbyController.ReturnHomeFromGameCenter();
            }
        }
    }
}
