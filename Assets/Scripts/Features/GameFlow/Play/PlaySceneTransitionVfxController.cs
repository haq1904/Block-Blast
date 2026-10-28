using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockBlast.Features.GameFlow.Play
{
    /// <summary>
    /// Controller managing cross-scene play table fog transition requests,
    /// registering the IPlaySceneTransitionVfxService and reacting to active scene changes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlaySceneTransitionVfxController : MonoBehaviour, IPlaySceneTransitionVfxService
    {
        [Tooltip("The passive fog transition view.")]
        [SerializeField] private PlayTableFogTransitionView _view;

        [Tooltip("Whether this scene instance publishes the play transition VFX service.")]
        [SerializeField] private bool _registerAsTransitionService = true;

        private bool _isServiceRegistered;
        private bool _isActiveSceneSubscribed;

        private bool _coverRequested;
        private bool _coverCompleted;
        private Task _activeCoverTask;

        public PlayTableFogTransitionView View
        {
            get => _view;
            set => _view = value;
        }

        public bool RegisterAsTransitionService
        {
            get => _registerAsTransitionService;
            set => _registerAsTransitionService = value;
        }

        public bool IsServiceRegistered => _isServiceRegistered;
        public bool IsActiveSceneSubscribed => _isActiveSceneSubscribed;

        public bool IsCoverRequested => _coverRequested;
        public bool IsCoverCompleted => _coverCompleted;

        private void Awake()
        {
            if (_view == null)
            {
                _view = GetComponent<PlayTableFogTransitionView>();
            }
        }

        private void OnEnable()
        {
            SubscribeToActiveSceneChangesOnce();
            TryRegisterTransitionService();

            if (!enabled)
            {
                return;
            }

            if (SceneManager.GetActiveScene() == gameObject.scene && _coverRequested && _coverCompleted)
            {
                TriggerReveal();
            }
        }

        private void OnDisable()
        {
            ReleaseLifecycleOwnership();
            ResetImmediate();
        }

        private void OnDestroy()
        {
            ReleaseLifecycleOwnership();
            ResetImmediate();
        }

        private void SubscribeToActiveSceneChangesOnce()
        {
            if (_isActiveSceneSubscribed)
            {
                return;
            }

            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
            _isActiveSceneSubscribed = true;
        }

        private void UnsubscribeFromActiveSceneChangesOnce()
        {
            if (!_isActiveSceneSubscribed)
            {
                return;
            }

            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            _isActiveSceneSubscribed = false;
        }

        private void TryRegisterTransitionService()
        {
            if (!_registerAsTransitionService || _isServiceRegistered)
            {
                return;
            }

            if (ServiceLocator.TryGet<IPlaySceneTransitionVfxService>(out var current) && current != null)
            {
                if (ReferenceEquals(current, this))
                {
                    _isServiceRegistered = true;
                    return;
                }

                Debug.LogWarning("[PlaySceneTransitionVfxController] An active IPlaySceneTransitionVfxService is already registered. Disabling duplicate controller.", this);
                UnsubscribeFromActiveSceneChangesOnce();
                enabled = false;
                return;
            }

            ServiceLocator.Register<IPlaySceneTransitionVfxService>(this);
            _isServiceRegistered = true;
        }

        private void ReleaseLifecycleOwnership()
        {
            UnsubscribeFromActiveSceneChangesOnce();

            if (_isServiceRegistered)
            {
                ServiceLocator.Unregister<IPlaySceneTransitionVfxService>(this);
                _isServiceRegistered = false;
            }
        }

        /// <summary>
        /// Requests the fog curtains to cover the play table.
        /// Idempotent: repeated calls during an ongoing cover return the same task.
        /// </summary>
        public Task CoverTableAsync(CancellationToken cancellationToken = default)
        {
            if (_activeCoverTask != null && !_activeCoverTask.IsCompleted)
            {
                return _activeCoverTask;
            }

            _coverRequested = true;
            _coverCompleted = false;

            if (_view == null)
            {
                _coverCompleted = true;
                return Task.CompletedTask;
            }

            _activeCoverTask = ExecuteCoverAsync(cancellationToken);
            return _activeCoverTask;
        }

        private async Task ExecuteCoverAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _view.PlayCoverAsync(cancellationToken);
                _coverCompleted = true;
            }
            catch (OperationCanceledException)
            {
                ResetImmediate();
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlaySceneTransitionVfxController] Cover failed: {ex.Message}", this);
                ResetImmediate();
                throw;
            }
        }

        /// <summary>
        /// Handles Unity active scene changed signal.
        /// When PlayScene becomes active and cover was armed, triggers reveal.
        /// If opened directly without cover request, ensures fog remains hidden.
        /// </summary>
        public void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
        {
            if (this == null || !isActiveAndEnabled)
            {
                return;
            }

            if (nextScene == gameObject.scene)
            {
                if (_coverRequested && _coverCompleted)
                {
                    TriggerReveal();
                }
                else
                {
                    // Direct-open or no cover armed: keep fog hidden
                    _view?.ResetImmediate();
                }
            }
        }

        private void TriggerReveal()
        {
            _coverRequested = false;
            _coverCompleted = false;
            _activeCoverTask = null;

            if (_view != null)
            {
                _view.PlayRevealAfterActivation();
            }
        }

        /// <summary>
        /// Immediately resets state and restores the fog view to hidden starting state.
        /// </summary>
        public void ResetImmediate()
        {
            _coverRequested = false;
            _coverCompleted = false;
            _activeCoverTask = null;

            if (_view != null)
            {
                _view.ResetImmediate();
            }
        }
    }
}
