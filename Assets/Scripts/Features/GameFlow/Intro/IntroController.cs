using System;
using System.Threading.Tasks;
using BlockBlast.Features.GameFlow.Navigation;
using UnityEngine;

namespace BlockBlast.Features.GameFlow.Intro
{
    /// <summary>
    /// Lightweight controller managing the intro scene duration and transition into LobbyScene.
    /// </summary>
    public sealed class IntroController : MonoBehaviour
    {
        [Header("View")]
        [SerializeField] private IntroLoadingView _loadingView;

        [Header("Timing Config")]
        [Tooltip("Visible duration in seconds before automatically loading LobbyScene.")]
        [SerializeField, Min(0.5f)] private float _displayDuration = 2.0f;

        [Tooltip("Minimum visible duration in seconds for the loading screen transition into Lobby.")]
        [SerializeField, Min(0.1f)] private float _minimumLoadingDuration = 0.5f;

        [Tooltip("Whether tapping the screen skips the remaining intro delay immediately.")]
        [SerializeField] private bool _allowTapToSkip = true;

        private IGameSceneNavigator _navigator;
        private bool _transitionRequested;
        private float _timer;

        public event Action IntroCompleted;

        private void Awake()
        {
            ServiceLocator.TryGet<IGameSceneNavigator>(out _navigator);
        }

        private void Start()
        {
            if (_loadingView != null)
            {
                _loadingView.StartSpinning();
                if (_allowTapToSkip)
                {
                    _loadingView.ScreenTapped += OnScreenTapped;
                }
            }

            _timer = 0f;
            _transitionRequested = false;
        }

        private void Update()
        {
            if (_transitionRequested)
            {
                return;
            }

            _timer += Time.deltaTime;
            if (_timer >= _displayDuration)
            {
                _ = RequestLobbyTransitionAsync();
            }
        }

        private void OnScreenTapped()
        {
            if (!_transitionRequested)
            {
                _ = RequestLobbyTransitionAsync();
            }
        }

        public async Task RequestLobbyTransitionAsync()
        {
            if (_transitionRequested)
            {
                return;
            }

            if (_navigator != null && _navigator.IsTransitioning)
            {
                return;
            }

            _transitionRequested = true;

            if (_loadingView != null)
            {
                _loadingView.StopSpinning();
            }

            IntroCompleted?.Invoke();

            try
            {
                if (_navigator == null)
                {
                    ServiceLocator.TryGet<IGameSceneNavigator>(out _navigator);
                }

                if (_navigator != null)
                {
                    await _navigator.LoadAsync(
                        GameSceneRoute.Lobby,
                        SceneTransitionPresentation.LoadingScreen,
                        _minimumLoadingDuration);
                }
                else
                {
                    Debug.LogError("[IntroController] IGameSceneNavigator not found in ServiceLocator.", this);
                    _transitionRequested = false;
                }
            }
            catch (Exception exception)
            {
                _transitionRequested = false;
                Debug.LogException(exception, this);
            }
        }

        private void OnDisable()
        {
            if (_loadingView != null && _allowTapToSkip)
            {
                _loadingView.ScreenTapped -= OnScreenTapped;
            }
        }
    }
}
