using System;
using System.Threading;
using System.Threading.Tasks;
using BlockBlast.Features.GameFlow.Camera;
using BlockBlast.Features.GameFlow.Navigation;
using BlockBlast.Features.GameFlow.Play;
using UnityEngine;

namespace BlockBlast.Features.Lobby
{
    public sealed partial class LobbyController : MonoBehaviour
    {
        [Header("Transition VFX")]
        [Tooltip("Feedback view for the Play world button, awaited for press motion before starting transition.")]
        [SerializeField] private LobbyWorldButtonFeedbackView _playWorldButtonFeedback;

        [Tooltip("Exit view that shrinks and hides the four world buttons after fog covers table center.")]
        [SerializeField] private LobbyWorldButtonsExitView _worldButtonsExitView;

        [Header("Transition Config")]
        [Tooltip("Realtime delay in seconds before the camera moves into Play.")]
        [SerializeField, Min(0f)] private float _playTransitionDelay = 1f;

        [Tooltip("Realtime delay after Play world-button press motion completes before fog and camera transition begin.")]
        [SerializeField, Min(0f)] private float _worldPlayDelayAfterPress = 0.05f;

        private bool _loggedMissingVfxServiceWarning;

        public float PlayTransitionDelay
        {
            get => _playTransitionDelay;
            set => _playTransitionDelay = Mathf.Max(0f, value);
        }

        public float WorldPlayDelayAfterPress
        {
            get => _worldPlayDelayAfterPress;
            set => _worldPlayDelayAfterPress = Mathf.Max(0f, value);
        }

        public LobbyWorldButtonFeedbackView PlayWorldButtonFeedback
        {
            get => _playWorldButtonFeedback;
            set => _playWorldButtonFeedback = value;
        }

        public LobbyWorldButtonsExitView WorldButtonsExitView
        {
            get => _worldButtonsExitView;
            set => _worldButtonsExitView = value;
        }

        public async Task RequestPlayAsync(bool isWorldPlay = false)
        {
            if (_playRequested)
            {
                return;
            }

            if (_navigator != null && _navigator.IsTransitioning)
            {
                return;
            }

            _playRequested = true;

            if (_navigationView != null)
            {
                _navigationView.SetNavigationEnabled(false);
            }

            if (_transitionView != null)
            {
                if (_transitionView.HomeView != null)
                {
                    _transitionView.HomeView.SetPlayInteractable(false);
                }
                _transitionView.SetTransitionBlockerActive(true);
            }

            SetWorldButtonsInteractable(false);

            IWorldCameraTransitionService cameraService = null;
            if (!ServiceLocator.TryGet(out cameraService))
            {
                Debug.LogError("[LobbyController] IWorldCameraTransitionService not found in ServiceLocator.", this);
                ResetInputState();
                return;
            }

            IPlaySceneTransitionVfxService transitionVfx = null;
            if (!ServiceLocator.TryGet(out transitionVfx))
            {
                if (!_loggedMissingVfxServiceWarning)
                {
                    _loggedMissingVfxServiceWarning = true;
                    Debug.LogWarning("[LobbyController] IPlaySceneTransitionVfxService not found in ServiceLocator. Proceeding with camera-only transition.", this);
                }
            }

            CancellationToken token = _destroyCts != null ? _destroyCts.Token : CancellationToken.None;

            try
            {
                if (isWorldPlay && _playWorldButtonFeedback != null)
                {
                    await _playWorldButtonFeedback.WaitForCurrentPressMotionAsync(token);
                    await WaitForDelayAsync(_worldPlayDelayAfterPress, token);
                }
                else
                {
                    await WaitForDelayAsync(_playTransitionDelay, token);
                }

                Task fogCoverTask = transitionVfx != null
                    ? transitionVfx.CoverTableAsync(token)
                    : Task.CompletedTask;

                Task cameraTask = cameraService.MoveToPlayAsync(token);
                Task buttonExitTask = Task.CompletedTask;

                if (transitionVfx != null && _worldButtonsExitView != null)
                {
                    await fogCoverTask;
                    buttonExitTask = _worldButtonsExitView.PlayExitAsync(token);
                }

                await Task.WhenAll(cameraTask, fogCoverTask, buttonExitTask);
            }
            catch (OperationCanceledException)
            {
                transitionVfx?.ResetImmediate();
                _worldButtonsExitView?.ResetImmediate();
                cameraService.SnapToLobby();
                if (isActiveAndEnabled)
                {
                    ResetInputState();
                }
                return;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LobbyController] Play transition failed: {ex.Message}. Snapping camera back to Lobby.", this);
                transitionVfx?.ResetImmediate();
                _worldButtonsExitView?.ResetImmediate();
                cameraService.SnapToLobby();
                if (isActiveAndEnabled)
                {
                    ResetInputState();
                }
                return;
            }

            try
            {
                if (_navigator == null)
                {
                    ServiceLocator.TryGet<IGameSceneNavigator>(out _navigator);
                }

                if (_navigator != null)
                {
                    await _navigator.LoadAsync(
                        GameSceneRoute.Play,
                        SceneTransitionPresentation.None,
                        0f,
                        token);
                }
                else
                {
                    Debug.LogError("[LobbyController] IGameSceneNavigator not found in ServiceLocator.", this);
                    transitionVfx?.ResetImmediate();
                    _worldButtonsExitView?.ResetImmediate();
                    if (cameraService != null)
                    {
                        await cameraService.MoveToLobbyAsync(CancellationToken.None);
                    }
                    if (isActiveAndEnabled)
                    {
                        ResetInputState();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                transitionVfx?.ResetImmediate();
                _worldButtonsExitView?.ResetImmediate();
                if (cameraService != null)
                {
                    cameraService.SnapToLobby();
                }
                if (isActiveAndEnabled)
                {
                    ResetInputState();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                transitionVfx?.ResetImmediate();
                _worldButtonsExitView?.ResetImmediate();
                if (cameraService != null)
                {
                    try
                    {
                        await cameraService.MoveToLobbyAsync(CancellationToken.None);
                    }
                    catch
                    {
                        cameraService.SnapToLobby();
                    }
                }
                if (isActiveAndEnabled)
                {
                    ResetInputState();
                }
            }
        }

        private static Task WaitForDelayAsync(float seconds, CancellationToken cancellationToken)
        {
            float delay = Mathf.Max(0f, seconds);
            return delay > 0f
                ? Task.Delay(TimeSpan.FromSeconds(delay), cancellationToken)
                : Task.CompletedTask;
        }

        private void ResetInputState()
        {
            _playRequested = false;

            if (_navigationView != null)
            {
                _navigationView.SetNavigationEnabled(true);
            }

            if (_transitionView != null)
            {
                if (_transitionView.HomeView != null)
                {
                    _transitionView.HomeView.SetPlayInteractable(true);
                }
                _transitionView.SetTransitionBlockerActive(false);
            }

            SetWorldButtonsInteractable(true);
        }
    }
}
