using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlockBlast.Features.Lobby
{
    /// <summary>
    /// Presentation component managing bottom navigation buttons and dispatching TabSelected events.
    /// </summary>
    public sealed class LobbyNavigationView : MonoBehaviour
    {
        [Header("Tab Buttons")]
        [SerializeField] private Button _homeButton;
        [SerializeField] private Button _customizationButton;
        [SerializeField] private Button _gameCenterButton;
        [SerializeField] private Button _menuButton;

        public event Action<LobbyTabId> TabSelected;

        private void Awake()
        {
            if (_homeButton != null) _homeButton.onClick.AddListener(() => OnTabClicked(LobbyTabId.Home));
            if (_customizationButton != null) _customizationButton.onClick.AddListener(() => OnTabClicked(LobbyTabId.Customization));
            if (_gameCenterButton != null) _gameCenterButton.onClick.AddListener(() => OnTabClicked(LobbyTabId.GameCenter));
            if (_menuButton != null) _menuButton.onClick.AddListener(() => OnTabClicked(LobbyTabId.Menu));
        }

        private void OnTabClicked(LobbyTabId tabId)
        {
            TabSelected?.Invoke(tabId);
        }

        public void SetNavigationEnabled(bool isEnabled)
        {
            if (_homeButton != null) _homeButton.interactable = isEnabled;
            if (_customizationButton != null) _customizationButton.interactable = isEnabled;
            if (_gameCenterButton != null) _gameCenterButton.interactable = isEnabled;
            if (_menuButton != null) _menuButton.interactable = isEnabled;
        }

        private void OnDestroy()
        {
            if (_homeButton != null) _homeButton.onClick.RemoveAllListeners();
            if (_customizationButton != null) _customizationButton.onClick.RemoveAllListeners();
            if (_gameCenterButton != null) _gameCenterButton.onClick.RemoveAllListeners();
            if (_menuButton != null) _menuButton.onClick.RemoveAllListeners();
            TabSelected = null;
        }
    }
}
