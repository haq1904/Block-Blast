using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace BlockBlast.Features.GameFlow.Camera
{
    /// <summary>
    /// Controller publishing IWorldCameraTransitionService to ServiceLocator and orchestrating camera poses.
    /// </summary>
    public sealed class WorldCameraController : MonoBehaviour, IWorldCameraTransitionService
    {
        [Tooltip("View component managing camera animation.")]
        [SerializeField] private WorldCameraTransitionView _transitionView;

        private bool _isRegistered;

        public bool IsMoving => _transitionView != null && _transitionView.IsMoving;

        public WorldCameraTransitionView TransitionView
        {
            get => _transitionView;
            set => _transitionView = value;
        }

        private void Awake()
        {
            if (ServiceLocator.TryGet<IWorldCameraTransitionService>(out var existing) && existing != null && !ReferenceEquals(existing, this))
            {
                Debug.LogWarning("[WorldCameraController] An active IWorldCameraTransitionService is already registered. Disabling duplicate component.", this);
                enabled = false;
                return;
            }

            ServiceLocator.Register<IWorldCameraTransitionService>(this);
            _isRegistered = true;

            SnapToLobby();
        }

        public Task MoveToPlayAsync(CancellationToken cancellationToken = default)
        {
            if (_transitionView == null)
            {
                throw new InvalidOperationException("[WorldCameraController] TransitionView is not assigned.");
            }

            return _transitionView.MoveToPlayAsync(cancellationToken);
        }

        public Task MoveToLobbyAsync(CancellationToken cancellationToken = default)
        {
            if (_transitionView == null)
            {
                throw new InvalidOperationException("[WorldCameraController] TransitionView is not assigned.");
            }

            return _transitionView.MoveToLobbyAsync(cancellationToken);
        }

        public void SnapToLobby()
        {
            if (_transitionView != null)
            {
                _transitionView.SnapToLobby();
            }
        }

        private void OnDestroy()
        {
            if (_isRegistered)
            {
                ServiceLocator.Unregister<IWorldCameraTransitionService>(this);
                _isRegistered = false;
            }
        }
    }
}
