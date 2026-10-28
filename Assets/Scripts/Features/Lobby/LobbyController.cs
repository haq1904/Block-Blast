using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BlockBlast.Core.Feedback;
using BlockBlast.Features.Feedback;
using BlockBlast.Features.Feedback.Audio;
using BlockBlast.Features.GameFlow.Audio;
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
        [SerializeField] private LobbySettingsOverlayView _settingsOverlay;
        [SerializeField] private LobbyWorldCustomizationOverlayView _worldCustomizationOverlay;

        [Header("3D World Buttons")]
        [Tooltip("The four 3D interactive prop buttons in Lobby scene.")]
        [SerializeField] private LobbyWorldButtonView[] _worldButtons;

        private IGameSceneNavigator _navigator;
        private IAudioVolumeSettingsService _audioVolumeSettingsService;
        private IFeedbackSettingsService _feedbackSettingsService;
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

        public LobbySettingsOverlayView SettingsOverlay
        {
            get => _settingsOverlay != null ? _settingsOverlay : _transitionView?.SettingsOverlay;
            set => _settingsOverlay = value;
        }

        public LobbyWorldCustomizationOverlayView WorldCustomizationOverlay
        {
            get => _worldCustomizationOverlay != null ? _worldCustomizationOverlay : _transitionView?.WorldCustomizationOverlay;
            set => _worldCustomizationOverlay = value;
        }

        private void Awake()
        {
            ServiceLocator.TryGet<IGameSceneNavigator>(out _navigator);
            ServiceLocator.TryGet<IAudioVolumeSettingsService>(out _audioVolumeSettingsService);
            ServiceLocator.TryGet<IFeedbackSettingsService>(out _feedbackSettingsService);
            if (_transitionView != null)
            {
                _transitionView.InitializePanels();
            }
            if (_worldButtons != null && _worldButtons.Length > 0)
            {
                ValidateWorldButtons();
            }
        }

        public bool ValidateWorldButtons(bool isEditorValidation = false, bool requireEnabled = true, bool logFailure = true)
        {
            return ValidateWorldButtons(_worldButtons, requireEnabled, logFailure, isEditorValidation);
        }

        public bool ValidateWorldButtons(IReadOnlyList<LobbyWorldButtonView> buttons, bool requireEnabled = true, bool logFailure = true, bool isEditorValidation = false)
        {
            if (buttons == null || buttons.Count == 0)
            {
                if (isEditorValidation)
                {
                    return true;
                }

                if (logFailure)
                {
                    LogValidation("[LobbyController] _worldButtons must contain exactly 4 prop buttons.", isEditorValidation);
                }
                return false;
            }

            if (buttons.Count != 4)
            {
                if (logFailure)
                {
                    LogValidation("[LobbyController] _worldButtons must contain exactly 4 prop buttons.", isEditorValidation);
                }
                return false;
            }

            var uniqueViews = new HashSet<LobbyWorldButtonView>();
            var uniqueActions = new HashSet<LobbyWorldButtonAction>();
            int expectedLayer = LayerMask.NameToLayer("LobbyInteractable");
            if (expectedLayer < 0)
            {
                if (logFailure)
                {
                    LogValidation("[LobbyController] Required layer 'LobbyInteractable' is missing.", isEditorValidation);
                }
                return false;
            }

            for (int i = 0; i < buttons.Count; i++)
            {
                var btn = buttons[i];
                if (btn == null)
                {
                    if (logFailure) LogValidation($"[LobbyController] World button at index {i} is null.", isEditorValidation);
                    return false;
                }

                if (!uniqueViews.Add(btn))
                {
                    if (logFailure) LogValidation($"[LobbyController] Duplicate button view reference '{btn.name}' at index {i}.", isEditorValidation);
                    return false;
                }

                if (!uniqueActions.Add(btn.Action))
                {
                    if (logFailure) LogValidation($"[LobbyController] Duplicate action '{btn.Action}' found on world button '{btn.name}'.", isEditorValidation);
                    return false;
                }

                if (btn.HitCollider == null)
                {
                    if (logFailure) LogValidation($"[LobbyController] World button '{btn.name}' is missing a Collider.", isEditorValidation);
                    return false;
                }

                if (btn.HitCollider.gameObject != btn.gameObject)
                {
                    if (logFailure) LogValidation($"[LobbyController] World button '{btn.name}' collider belongs to another GameObject ('{btn.HitCollider.gameObject.name}').", isEditorValidation);
                    return false;
                }

                if (requireEnabled && !btn.HitCollider.enabled)
                {
                    if (logFailure) LogValidation($"[LobbyController] World button '{btn.name}' collider is disabled.", isEditorValidation);
                    return false;
                }

                if (btn.gameObject.layer != expectedLayer)
                {
                    if (logFailure) LogValidation($"[LobbyController] World button '{btn.name}' is on layer '{LayerMask.LayerToName(btn.gameObject.layer)}', expected 'LobbyInteractable'.", isEditorValidation);
                    return false;
                }
            }

            if (!uniqueActions.Contains(LobbyWorldButtonAction.Play) ||
                !uniqueActions.Contains(LobbyWorldButtonAction.Customization) ||
                !uniqueActions.Contains(LobbyWorldButtonAction.GameCenter) ||
                !uniqueActions.Contains(LobbyWorldButtonAction.Settings))
            {
                if (logFailure) LogValidation("[LobbyController] _worldButtons must contain actions: Play, Customization, GameCenter, and Settings.", isEditorValidation);
                return false;
            }

            return true;
        }

        public bool ValidateButtonsList(IReadOnlyList<LobbyWorldButtonView> buttons, bool isEditorValidation = false, bool requireEnabled = true, bool logFailure = true)
        {
            return ValidateWorldButtons(buttons, requireEnabled, logFailure, isEditorValidation);
        }

        public void BindWorldButtonClicks(IReadOnlyList<LobbyWorldButtonView> buttons)
        {
            if (buttons == null) return;
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i] != null)
                {
                    buttons[i].Clicked -= OnWorldButtonClicked;
                    buttons[i].Clicked += OnWorldButtonClicked;
                }
            }
        }

        public void UnbindWorldButtonClicks(IReadOnlyList<LobbyWorldButtonView> buttons)
        {
            if (buttons == null) return;
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttons[i] != null)
                {
                    buttons[i].Clicked -= OnWorldButtonClicked;
                }
            }
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

            BindViews();

            if (_hasBoundValidButtons && _destroyCts != null)
            {
                _ = GateWorldButtonsUntilNavigationIdleAsync(_destroyCts.Token);
            }
        }

        private void Start()
        {
            if (ServiceLocator.TryGet<IWorldCameraTransitionService>(out var cameraService))
            {
                cameraService.SnapToLobby();
            }

            if (ServiceLocator.TryGet<IGameMusicService>(out var musicService))
            {
                musicService.PlayLobby();
            }

            UpdateHighScoreDisplay();
        }

        private void BindViews()
        {
            if (_isBound) return;
            _isBound = true;

            BindStaticViews();
            BindWorldCustomization();

            if (_worldButtons != null && _worldButtons.Length > 0)
            {
                if (ValidateWorldButtons(requireEnabled: !_hasBoundValidButtons, logFailure: true))
                {
                    BindWorldButtonClicks(_worldButtons);
                    _hasBoundValidButtons = true;
                }
            }
        }

        private void BindStaticViews()
        {
            if (_transitionView != null && _transitionView.HomeView != null)
            {
                _transitionView.HomeView.PlayRequested -= OnPlayRequested;
                _transitionView.HomeView.PlayRequested += OnPlayRequested;
            }

            if (_navigationView != null)
            {
                _navigationView.TabSelected -= OnTabSelected;
                _navigationView.TabSelected += OnTabSelected;
            }

            var settings = SettingsOverlay;
            if (settings != null)
            {
                settings.CloseRequested -= CloseOverlay;
                settings.CloseRequested += CloseOverlay;
                settings.MusicVolumeChanged -= HandleMusicVolumeChanged;
                settings.MusicVolumeChanged += HandleMusicVolumeChanged;
                settings.SfxVolumeChanged -= HandleSfxVolumeChanged;
                settings.SfxVolumeChanged += HandleSfxVolumeChanged;
                settings.HapticEnabledChanged -= HandleHapticEnabledChanged;
                settings.HapticEnabledChanged += HandleHapticEnabledChanged;
                settings.CameraShakeEnabledChanged -= HandleCameraShakeEnabledChanged;
                settings.CameraShakeEnabledChanged += HandleCameraShakeEnabledChanged;
            }

            var customization = WorldCustomizationOverlay;
            if (customization != null)
            {
                customization.CloseRequested -= CloseOverlay;
                customization.CloseRequested += CloseOverlay;
            }

            SyncAudioSettingsFromService();
            SyncFeedbackSettingsToView();
        }

        private void UnbindViews()
        {
            if (!_isBound) return;
            _isBound = false;

            var settings = SettingsOverlay;
            if (settings != null)
            {
                settings.CloseRequested -= CloseOverlay;
                settings.MusicVolumeChanged -= HandleMusicVolumeChanged;
                settings.SfxVolumeChanged -= HandleSfxVolumeChanged;
                settings.HapticEnabledChanged -= HandleHapticEnabledChanged;
                settings.CameraShakeEnabledChanged -= HandleCameraShakeEnabledChanged;
            }

            var customization = WorldCustomizationOverlay;
            if (customization != null)
            {
                customization.CloseRequested -= CloseOverlay;
            }
            UnbindWorldCustomization();

            if (_transitionView != null && _transitionView.HomeView != null)
            {
                _transitionView.HomeView.PlayRequested -= OnPlayRequested;
            }

            if (_navigationView != null)
            {
                _navigationView.TabSelected -= OnTabSelected;
            }

            UnbindWorldButtonClicks(_worldButtons);
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
                highScore = GetLocalPlayerDataStore().LoadHighScore();
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
                    OpenOverlay(LobbyOverlayId.Customization);
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

            if (overlayId == LobbyOverlayId.Setting)
            {
                SyncAudioSettingsFromService();
                SyncFeedbackSettingsToView();
            }

            SetWorldButtonsInteractable(false);

            if (_transitionView != null)
            {
                _transitionView.ShowOverlay(overlayId);
            }
        }

        public void CloseOverlay()
        {
            FlushAudioSettings();
            FlushFeedbackSettings();

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
            FlushAudioSettings();
            FlushFeedbackSettings();

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
            FlushAudioSettings();
            FlushFeedbackSettings();

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
