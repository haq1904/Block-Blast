using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlockBlast.Features.Lobby
{
    /// <summary>
    /// Passive presentation-only overlay view for Lobby World Customization,
    /// emitting close events from the close 'X' button and the 'VIEW WORLD IN LOBBY' CTA.
    /// Does not invoke any services, persistence (PlayerPrefs), audio, or gameplay effects.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class LobbyWorldCustomizationOverlayView : LobbyPanelView
    {
        [Header("Close Controls")]
        [Tooltip("Close button in the header.")]
        [SerializeField] private Button _closeButton;

        [Tooltip("Primary CTA button to view the world in the lobby and close.")]
        [SerializeField] private Button _viewWorldInLobbyButton;

        public event Action CloseRequested;

        public Button CloseButton
        {
            get => _closeButton;
            set => _closeButton = value;
        }

        public Button ViewWorldInLobbyButton
        {
            get => _viewWorldInLobbyButton;
            set => _viewWorldInLobbyButton = value;
        }

        protected override void Awake()
        {
            base.Awake();
            RegisterListeners();
        }

        private void OnDestroy()
        {
            UnregisterListeners();
            CloseRequested = null;
        }

        public void RequestClose()
        {
            HandleCloseClicked();
        }

        public void SetReferences(Button closeButton, Button viewWorldInLobbyButton)
        {
            UnregisterListeners();
            _closeButton = closeButton;
            _viewWorldInLobbyButton = viewWorldInLobbyButton;
            RegisterListeners();
        }

        private void RegisterListeners()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
                _closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (_viewWorldInLobbyButton != null)
            {
                _viewWorldInLobbyButton.onClick.RemoveListener(HandleViewWorldClicked);
                _viewWorldInLobbyButton.onClick.AddListener(HandleViewWorldClicked);
            }
        }

        private void UnregisterListeners()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
            }

            if (_viewWorldInLobbyButton != null)
            {
                _viewWorldInLobbyButton.onClick.RemoveListener(HandleViewWorldClicked);
            }
        }

        private void HandleCloseClicked()
        {
            CloseRequested?.Invoke();
        }

        private void HandleViewWorldClicked()
        {
            CloseRequested?.Invoke();
        }
    }
}
