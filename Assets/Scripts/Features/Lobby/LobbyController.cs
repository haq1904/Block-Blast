using System;
using System.Threading.Tasks;
using BlockBlast.Features.GameFlow.Navigation;
using UnityEngine;

namespace BlockBlast.Features.Lobby
{
    /// <summary>
    /// Master controller for the Lobby scene, managing navigation tabs, high score display, and transition to PlayScene.
    /// </summary>
    public sealed class LobbyController : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] private LobbyNavigationView _navigationView;
        [SerializeField] private LobbyTransitionView _transitionView;
        [SerializeField] private LobbyBackgroundView _backgroundView;

        [Header("Transition Config")]
        [Tooltip("Minimum visible duration in seconds for the loading screen transition into PlayScene.")]
        [SerializeField] private float _minimumLoadingDuration = 0.5f;

        private IGameSceneNavigator _navigator;
        private LobbyTabId _currentTab = LobbyTabId.Home;
        private bool _playRequested;

        public LobbyTabId CurrentTab => _currentTab;

        private void Awake()
        {
            ServiceLocator.TryGet<IGameSceneNavigator>(out _navigator);
        }

        private void Start()
        {
            if (_transitionView != null)
            {
                _transitionView.InitializePanels();
                if (_transitionView.HomeView != null)
                {
                    _transitionView.HomeView.PlayRequested += OnPlayRequested;
                }
            }

            if (_navigationView != null)
            {
                _navigationView.TabSelected += OnTabSelected;
            }

            UpdateHighScoreDisplay();
        }

        private void UpdateHighScoreDisplay()
        {
            int highScore = 0;
            if (ServiceLocator.TryGet<IScoreService>(out var scoreService))
            {
                highScore = scoreService.HighScore;
            }
            else
            {
                highScore = PlayerPrefs.GetInt("HighScore", 0);
            }

            if (_transitionView != null && _transitionView.HomeView != null)
            {
                _transitionView.HomeView.SetHighScore(highScore);
            }
        }

        private void OnTabSelected(LobbyTabId tabId)
        {
            if (_playRequested) return;

            _currentTab = tabId;
            if (_transitionView != null)
            {
                _transitionView.SwitchTab(tabId);
            }
        }

        public void OpenOverlay(LobbyOverlayId overlayId)
        {
            if (_playRequested) return;

            if (_transitionView != null)
            {
                _transitionView.ShowOverlay(overlayId);
            }
        }

        public void CloseOverlay()
        {
            if (_transitionView != null)
            {
                _transitionView.HideAllOverlays();
            }
        }

        private void OnPlayRequested()
        {
            _ = RequestPlayAsync();
        }

        public async Task RequestPlayAsync()
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
                        SceneTransitionPresentation.LoadingScreen,
                        _minimumLoadingDuration);
                }
                else
                {
                    Debug.LogError("[LobbyController] IGameSceneNavigator not found in ServiceLocator.", this);
                    ResetInputState();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                ResetInputState();
            }
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
        }

        private void OnDisable()
        {
            if (_transitionView != null && _transitionView.HomeView != null)
            {
                _transitionView.HomeView.PlayRequested -= OnPlayRequested;
            }

            if (_navigationView != null)
            {
                _navigationView.TabSelected -= OnTabSelected;
            }
        }
    }
}
