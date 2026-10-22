using System;
using System.Threading;
using System.Threading.Tasks;
using BlockBlast.Features.GameFlow.Camera;
using BlockBlast.Features.GameFlow.Navigation;
using BlockBlast.Features.GameFlow.Play;
using UnityEngine;

namespace BlockBlast.Features.Lobby
{
    /// <summary>
    /// Master controller for the Lobby scene, managing navigation tabs, high score display, 3D prop buttons, and transition to PlayScene.
    /// </summary>
    public sealed partial class LobbyController : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] private LobbyNavigationView _navigationView;
        [SerializeField] private LobbyTransitionView _transitionView;
        [SerializeField] private LobbyBackgroundView _backgroundView;

        [Header("3D World Buttons")]
        [Tooltip("The four 3D interactive prop buttons in Lobby scene.")]
        [SerializeField] private LobbyWorldButtonView[] _worldButtons;

        private IGameSceneNavigator _navigator;
        private LobbyTabId _currentTab = LobbyTabId.Home;
        private bool _playRequested;
        private bool _isBound;
        private bool _hasBoundValidButtons;
        private bool _ownsInitialWorldButtonGate;
        private CancellationTokenSource _destroyCts;

        public LobbyTabId CurrentTab => _currentTab;

        public LobbyWorldButtonView[] WorldButtons
        {
            get => _worldButtons;
            set => _worldButtons = value;
        }

        private void Awake()
        {
            ServiceLocator.TryGet<IGameSceneNavigator>(out _navigator);
            if (_transitionView != null)
            {
                _transitionView.InitializePanels();
            }
            ValidateWorldButtons();
        }

        public bool ValidateWorldButtons(bool isEditorValidation = false, bool requireEnabled = true)
        {
            if (_worldButtons == null || _worldButtons.Length != 4)
            {
                LogValidation("[LobbyController] _worldButtons must contain exactly 4 prop buttons.", isEditorValidation);
                return false;
            }

            var uniqueViews = new System.Collections.Generic.HashSet<LobbyWorldButtonView>();
            var uniqueActions = new System.Collections.Generic.HashSet<LobbyWorldButtonAction>();
            int expectedLayer = LayerMask.NameToLayer("LobbyInteractable");
            if (expectedLayer < 0)
            {
                LogValidation("[LobbyController] Required layer 'LobbyInteractable' is missing.", isEditorValidation);
                return false;
            }

            for (int i = 0; i < _worldButtons.Length; i++)
            {
                var btn = _worldButtons[i];
                if (btn == null)
                {
                    LogValidation($"[LobbyController] World button at index {i} is null.", isEditorValidation);
                    return false;
                }

                if (!uniqueViews.Add(btn))
                {
                    LogValidation($"[LobbyController] Duplicate button view reference '{btn.name}' at index {i}.", isEditorValidation);
                    return false;
                }

                if (!uniqueActions.Add(btn.Action))
                {
                    LogValidation($"[LobbyController] Duplicate action '{btn.Action}' found on world button '{btn.name}'.", isEditorValidation);
                    return false;
                }

                if (btn.HitCollider == null)
                {
                    LogValidation($"[LobbyController] World button '{btn.name}' is missing a Collider.", isEditorValidation);
                    return false;
                }

                if (btn.HitCollider.gameObject != btn.gameObject)
                {
                    LogValidation($"[LobbyController] World button '{btn.name}' collider belongs to another GameObject ('{btn.HitCollider.gameObject.name}').", isEditorValidation);
                    return false;
                }

                if (requireEnabled && !btn.HitCollider.enabled)
                {
                    LogValidation($"[LobbyController] World button '{btn.name}' collider is disabled.", isEditorValidation);
                    return false;
                }

                if (btn.gameObject.layer != expectedLayer)
                {
                    LogValidation($"[LobbyController] World button '{btn.name}' is on layer '{LayerMask.LayerToName(btn.gameObject.layer)}', expected 'LobbyInteractable'.", isEditorValidation);
                    return false;
                }
            }

            if (!uniqueActions.Contains(LobbyWorldButtonAction.Play) ||
                !uniqueActions.Contains(LobbyWorldButtonAction.Customization) ||
                !uniqueActions.Contains(LobbyWorldButtonAction.GameCenter) ||
                !uniqueActions.Contains(LobbyWorldButtonAction.Settings))
            {
                LogValidation("[LobbyController] _worldButtons must contain actions: Play, Customization, GameCenter, and Settings.", isEditorValidation);
                return false;
            }

            return true;
        }

        private void LogValidation(string message, bool isEditorValidation)
        {
            if (isEditorValidation)
            {
                Debug.LogWarning(message, this);
            }
            else
            {
                Debug.LogError(message, this);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ValidateWorldButtons(isEditorValidation: true);
            ValidatePlayButtonFeedback();
        }

        private void ValidatePlayButtonFeedback()
        {
            if (_playWorldButtonFeedback == null && _worldButtons != null)
            {
                for (int i = 0; i < _worldButtons.Length; i++)
                {
                    if (_worldButtons[i] != null && _worldButtons[i].Action == LobbyWorldButtonAction.Play)
                    {
                        _playWorldButtonFeedback = _worldButtons[i].GetComponent<LobbyWorldButtonFeedbackView>();
                        break;
                    }
                }
            }
        }
#endif

        private void OnEnable()
        {
            _destroyCts?.Dispose();
            _destroyCts = new CancellationTokenSource();

            if (_worldButtonsExitView != null)
            {
                _worldButtonsExitView.ResetImmediate();
            }

            if (!ValidateWorldButtons(requireEnabled: !_hasBoundValidButtons))
            {
                return;
            }

            BindViews();
            _hasBoundValidButtons = true;
            _ = GateWorldButtonsUntilNavigationIdleAsync(_destroyCts.Token);
        }

        private void Start()
        {
            if (ServiceLocator.TryGet<IWorldCameraTransitionService>(out var cameraService))
            {
                cameraService.SnapToLobby();
            }

            UpdateHighScoreDisplay();
        }

        private void BindViews()
        {
            if (_isBound) return;
            _isBound = true;

            if (_worldButtons != null)
            {
                for (int i = 0; i < _worldButtons.Length; i++)
                {
                    if (_worldButtons[i] != null)
                    {
                        _worldButtons[i].Clicked += OnWorldButtonClicked;
                    }
                }
            }

            if (_transitionView != null && _transitionView.HomeView != null)
            {
                _transitionView.HomeView.PlayRequested += OnPlayRequested;
            }

            if (_navigationView != null)
            {
                _navigationView.TabSelected += OnTabSelected;
            }
        }

        private void UnbindViews()
        {
            if (!_isBound) return;
            _isBound = false;

            if (_transitionView != null && _transitionView.HomeView != null)
            {
                _transitionView.HomeView.PlayRequested -= OnPlayRequested;
            }

            if (_navigationView != null)
            {
                _navigationView.TabSelected -= OnTabSelected;
            }

            if (_worldButtons != null)
            {
                for (int i = 0; i < _worldButtons.Length; i++)
                {
                    if (_worldButtons[i] != null)
                    {
                        _worldButtons[i].Clicked -= OnWorldButtonClicked;
                    }
                }
            }
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

        private void OnWorldButtonClicked(LobbyWorldButtonAction action)
        {
            if (_playRequested) return;

            switch (action)
            {
                case LobbyWorldButtonAction.Play:
                    _ = RequestPlayAsync(isWorldPlay: true);
                    break;
                case LobbyWorldButtonAction.Customization:
                    OnTabSelected(LobbyTabId.Customization);
                    break;
                case LobbyWorldButtonAction.GameCenter:
                    OnTabSelected(LobbyTabId.GameCenter);
                    break;
                case LobbyWorldButtonAction.Settings:
                    OpenOverlay(LobbyOverlayId.Setting);
                    break;
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

            SetWorldButtonsInteractable(false);

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

            if (!_playRequested)
            {
                SetWorldButtonsInteractable(true);
            }
        }

        private void SetWorldButtonsInteractable(bool interactable)
        {
            if (_worldButtons != null)
            {
                for (int i = 0; i < _worldButtons.Length; i++)
                {
                    if (_worldButtons[i] != null)
                    {
                        _worldButtons[i].SetInteractable(interactable);
                    }
                }
            }
        }

        private async Task GateWorldButtonsUntilNavigationIdleAsync(CancellationToken cancellationToken)
        {
            if (_navigator == null)
            {
                ServiceLocator.TryGet<IGameSceneNavigator>(out _navigator);
            }

            if (_navigator == null || !_navigator.IsTransitioning)
            {
                return;
            }

            _ownsInitialWorldButtonGate = true;
            SetWorldButtonsInteractable(false);

            try
            {
                while (_navigator.IsTransitioning)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Yield();
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (_ownsInitialWorldButtonGate)
            {
                _ownsInitialWorldButtonGate = false;
                if (!cancellationToken.IsCancellationRequested && this != null && isActiveAndEnabled && !_playRequested)
                {
                    SetWorldButtonsInteractable(true);
                }
            }
        }

        private void OnPlayRequested()
        {
            _ = RequestPlayAsync(isWorldPlay: false);
        }

        private void OnDisable()
        {
            _destroyCts?.Cancel();
            _destroyCts?.Dispose();
            _destroyCts = null;

            if (_ownsInitialWorldButtonGate)
            {
                _ownsInitialWorldButtonGate = false;
                if (!_playRequested)
                {
                    SetWorldButtonsInteractable(true);
                }
            }

            UnbindViews();
        }

        private void OnDestroy()
        {
            _destroyCts?.Cancel();
            _destroyCts?.Dispose();
            _destroyCts = null;

            if (_ownsInitialWorldButtonGate)
            {
                _ownsInitialWorldButtonGate = false;
            }

            UnbindViews();
        }
    }
}
