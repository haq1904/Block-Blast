using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlockBlast.Features.Lobby.GameCenter
{
    /// <summary>
    /// Presentation view component for the Player Center panel in Lobby.
    /// Exposes navigation events and manages panel visibility lifecycle.
    /// Does not contain data loading, cloud syncing, or gameplay business logic.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class LobbyGameCenterPanelView : LobbyPanelView
    {
        [Header("Controls")]
        [SerializeField] private Button _backButton;

        public event Action BackRequested;

        public Button BackButton
        {
            get => _backButton;
            set => _backButton = value;
        }

        protected override void Awake()
        {
            base.Awake();

            if (_backButton != null)
            {
                _backButton.onClick.AddListener(HandleBackClicked);
            }
        }

        private void OnDestroy()
        {
            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(HandleBackClicked);
            }
        }

        private void HandleBackClicked()
        {
            BackRequested?.Invoke();
        }
    }
}
