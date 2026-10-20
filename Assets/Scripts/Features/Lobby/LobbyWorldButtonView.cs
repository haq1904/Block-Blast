using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BlockBlast.Features.Lobby
{
    /// <summary>
    /// Thin View forwarding 3D pointer clicks from collider to LobbyController.
    /// </summary>
    public sealed class LobbyWorldButtonView : MonoBehaviour, IPointerClickHandler
    {
        [Tooltip("The action triggered when this 3D prop is clicked.")]
        [SerializeField] private LobbyWorldButtonAction _action;

        [Tooltip("The interaction collider used for raycasting.")]
        [SerializeField] private Collider _hitCollider;

        private bool _interactable = true;

        public event Action<LobbyWorldButtonAction> Clicked;

        /// <summary>
        /// Presentation event fired immediately when valid click occurs, before Clicked.
        /// </summary>
        public event Action Pressed;

        public LobbyWorldButtonAction Action
        {
            get => _action;
            set => _action = value;
        }

        public Collider HitCollider
        {
            get => _hitCollider;
            set => _hitCollider = value;
        }

        private bool _loggedMissingColliderError;

        public bool IsInteractable => _interactable && _hitCollider != null && _hitCollider.enabled && _hitCollider.gameObject == gameObject;

        private void Awake()
        {
            EnsureCollider();
        }

        public bool EnsureCollider()
        {
            if (_hitCollider == null)
            {
                _hitCollider = GetComponent<Collider>();
            }

            if (_hitCollider == null)
            {
                if (!_loggedMissingColliderError)
                {
                    _loggedMissingColliderError = true;
                    Debug.LogError($"[LobbyWorldButtonView] Missing Collider on {name}. Button is disabled.", this);
                }
                return false;
            }

            if (_hitCollider.gameObject != gameObject)
            {
                if (!_loggedMissingColliderError)
                {
                    _loggedMissingColliderError = true;
                    Debug.LogError($"[LobbyWorldButtonView] Collider on {name} belongs to another GameObject ({_hitCollider.gameObject.name}). Button is disabled.", this);
                }
                return false;
            }

            return true;
        }

        public void SetInteractable(bool interactable)
        {
            _interactable = interactable;
            if (_hitCollider != null && _hitCollider.gameObject == gameObject)
            {
                _hitCollider.enabled = interactable;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!EnsureCollider())
            {
                return;
            }

            if (!IsInteractable || (Application.isPlaying && !isActiveAndEnabled))
            {
                return;
            }

            if (eventData != null && eventData.dragging)
            {
                return;
            }

            Pressed?.Invoke();
            Clicked?.Invoke(_action);
        }
    }
}
