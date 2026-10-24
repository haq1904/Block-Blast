using UnityEngine;

namespace BlockBlast.Features.Lobby
{
    /// <summary>
    /// Presentation component managing panel switches and transition input blocking in the Lobby.
    /// </summary>
    public sealed class LobbyTransitionView : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private LobbyHomeView _homeView;
        [SerializeField] private LobbyPanelView _homePanel;
        [SerializeField] private LobbyPanelView _customizationPanel;
        [SerializeField] private LobbyPanelView _gameCenterPanel;
        [SerializeField] private LobbyPanelView _menuPanel;

        [Header("Modal / Sheet Overlays")]
        [SerializeField] private LobbyPanelView _highScoreOverlay;
        [SerializeField] private LobbyPanelView _statisticOverlay;
        [SerializeField] private LobbyPanelView _settingOverlay;
        [SerializeField] private LobbyPanelView _customizationOverlay;

        [Header("Blocker")]
        [Tooltip("Full-screen CanvasGroup blocking interaction during scene transitions.")]
        [SerializeField] private CanvasGroup _transitionBlocker;

        public LobbyHomeView HomeView => _homeView;
        public LobbySettingsOverlayView SettingsOverlay
        {
            get => _settingOverlay as LobbySettingsOverlayView;
            set => _settingOverlay = value;
        }
        public LobbyWorldCustomizationOverlayView WorldCustomizationOverlay
        {
            get => _customizationOverlay as LobbyWorldCustomizationOverlayView;
            set => _customizationOverlay = value;
        }

        public void InitializePanels()
        {
            if (_homePanel != null) _homePanel.SetImmediate(true);
            if (_customizationPanel != null) _customizationPanel.SetImmediate(false);
            if (_gameCenterPanel != null) _gameCenterPanel.SetImmediate(false);
            if (_menuPanel != null) _menuPanel.SetImmediate(false);

            if (_highScoreOverlay != null) _highScoreOverlay.SetImmediate(false);
            if (_statisticOverlay != null) _statisticOverlay.SetImmediate(false);
            if (_settingOverlay != null) _settingOverlay.SetImmediate(false);
            if (_customizationOverlay != null) _customizationOverlay.SetImmediate(false);

            SetTransitionBlockerActive(false);
        }

        public void SwitchTab(LobbyTabId tabId)
        {
            if (_homePanel != null)
            {
                if (tabId == LobbyTabId.Home) _homePanel.Show(); else _homePanel.Hide();
            }

            if (_customizationPanel != null)
            {
                if (tabId == LobbyTabId.Customization) _customizationPanel.Show(); else _customizationPanel.Hide();
            }

            if (_gameCenterPanel != null)
            {
                if (tabId == LobbyTabId.GameCenter) _gameCenterPanel.Show(); else _gameCenterPanel.Hide();
            }

            if (_menuPanel != null)
            {
                if (tabId == LobbyTabId.Menu) _menuPanel.Show(); else _menuPanel.Hide();
            }
        }

        public void ShowOverlay(LobbyOverlayId overlayId)
        {
            if (_highScoreOverlay != null)
            {
                if (overlayId == LobbyOverlayId.HighScore) _highScoreOverlay.Show(); else _highScoreOverlay.Hide();
            }

            if (_statisticOverlay != null)
            {
                if (overlayId == LobbyOverlayId.Statistic) _statisticOverlay.Show(); else _statisticOverlay.Hide();
            }

            if (_settingOverlay != null)
            {
                if (overlayId == LobbyOverlayId.Setting) _settingOverlay.Show(); else _settingOverlay.Hide();
            }

            if (_customizationOverlay != null)
            {
                if (overlayId == LobbyOverlayId.Customization) _customizationOverlay.Show(); else _customizationOverlay.Hide();
            }
        }

        public void HideAllOverlays()
        {
            if (_highScoreOverlay != null) _highScoreOverlay.Hide();
            if (_statisticOverlay != null) _statisticOverlay.Hide();
            if (_settingOverlay != null) _settingOverlay.Hide();
            if (_customizationOverlay != null) _customizationOverlay.Hide();
        }

        public void SetTransitionBlockerActive(bool isBlocked)
        {
            if (_transitionBlocker != null)
            {
                _transitionBlocker.alpha = isBlocked ? 1f : 0f;
                _transitionBlocker.interactable = isBlocked;
                _transitionBlocker.blocksRaycasts = isBlocked;
            }
        }
    }
}
