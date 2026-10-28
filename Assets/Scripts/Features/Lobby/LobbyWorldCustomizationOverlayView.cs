using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlockBlast.Features.Lobby
{
    /// <summary>
    /// Passive presentation-only overlay view for Lobby World Customization,
    /// emitting close events from the close 'X' button, the dimmer background, and the 'VIEW WORLD IN LOBBY' CTA.
    /// Does not invoke any services, persistence (PlayerPrefs), audio, or gameplay effects.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class LobbyWorldCustomizationOverlayView : LobbyPanelView
    {
        [Header("Close Controls")]
        [Tooltip("Close button in the header.")]
        [SerializeField] private Button _closeButton;

        [Tooltip("Full-screen dimmer button behind the dialog card.")]
        [SerializeField] private Button _dimmerButton;

        [Tooltip("Primary CTA button to view the world in the lobby and close.")]
        [SerializeField] private Button _viewWorldInLobbyButton;

        public event Action CloseRequested;
        public event Action<string> ConfirmRequested;

        [Tooltip("The stable target world ID to confirm when CTA is clicked.")]
        [SerializeField] private string _targetWorldId = "default";

        public string TargetWorldId
        {
            get => _targetWorldId;
            set => _targetWorldId = value;
        }

        public Button CloseButton
        {
            get => _closeButton;
            set
            {
                if (_closeButton != null)
                {
                    _closeButton.onClick.RemoveListener(HandleCloseClicked);
                }
                _closeButton = value;
                if (_closeButton != null)
                {
                    _closeButton.onClick.RemoveListener(HandleCloseClicked);
                    _closeButton.onClick.AddListener(HandleCloseClicked);
                }
            }
        }

        public Button DimmerButton
        {
            get => _dimmerButton;
            set
            {
                if (_dimmerButton != null)
                {
                    _dimmerButton.onClick.RemoveListener(HandleCloseClicked);
                }
                _dimmerButton = value;
                if (_dimmerButton != null)
                {
                    _dimmerButton.onClick.RemoveListener(HandleCloseClicked);
                    _dimmerButton.onClick.AddListener(HandleCloseClicked);
                }
            }
        }

        public Button ViewWorldInLobbyButton
        {
            get => _viewWorldInLobbyButton;
            set
            {
                if (_viewWorldInLobbyButton != null)
                {
                    _viewWorldInLobbyButton.onClick.RemoveListener(HandleViewWorldClicked);
                }
                _viewWorldInLobbyButton = value;
                if (_viewWorldInLobbyButton != null)
                {
                    _viewWorldInLobbyButton.onClick.RemoveListener(HandleViewWorldClicked);
                    _viewWorldInLobbyButton.onClick.AddListener(HandleViewWorldClicked);
                }
            }
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
            ConfirmRequested = null;
        }

        public void RequestClose()
        {
            HandleCloseClicked();
        }

        public void SetReferences(Button closeButton, Button viewWorldInLobbyButton)
        {
            SetReferences(closeButton, viewWorldInLobbyButton, _dimmerButton);
        }

        public void SetReferences(Button closeButton, Button viewWorldInLobbyButton, Button dimmerButton)
        {
            UnregisterListeners();
            _closeButton = closeButton;
            _viewWorldInLobbyButton = viewWorldInLobbyButton;
            _dimmerButton = dimmerButton;
            RegisterListeners();
        }

        private void RegisterListeners()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(HandleCloseClicked);
                _closeButton.onClick.AddListener(HandleCloseClicked);
            }

            if (_dimmerButton != null)
            {
                _dimmerButton.onClick.RemoveListener(HandleCloseClicked);
                _dimmerButton.onClick.AddListener(HandleCloseClicked);
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

            if (_dimmerButton != null)
            {
                _dimmerButton.onClick.RemoveListener(HandleCloseClicked);
            }

            if (_viewWorldInLobbyButton != null)
            {
                _viewWorldInLobbyButton.onClick.RemoveListener(HandleViewWorldClicked);
            }
        }

        public void SetInteractable(bool interactable)
        {
            var cg = GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.interactable = interactable;
                cg.blocksRaycasts = interactable;
            }
            if (_closeButton != null) _closeButton.interactable = interactable;
            if (_dimmerButton != null) _dimmerButton.interactable = interactable;
            if (_viewWorldInLobbyButton != null) _viewWorldInLobbyButton.interactable = interactable;
        }

        private void HandleCloseClicked()
        {
            CloseRequested?.Invoke();
        }

        private void HandleViewWorldClicked()
        {
            if (ConfirmRequested != null)
            {
                ConfirmRequested.Invoke(_targetWorldId);
            }
            else
            {
                CloseRequested?.Invoke();
            }
        }
    }
}
