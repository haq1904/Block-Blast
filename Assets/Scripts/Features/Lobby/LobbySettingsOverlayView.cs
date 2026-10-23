using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlockBlast.Features.Lobby
{
    /// <summary>
    /// Presentation-only overlay view for Lobby Settings, managing close events and local visual slider/toggle state.
    /// Does not invoke any services, persistence (PlayerPrefs), or gameplay effects.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class LobbySettingsOverlayView : LobbyPanelView
    {
        [Header("Canvas & Close Controls")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _dimmerButton;

        [Header("Audio (Presentation Only)")]
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;

        [Header("Feedback (Presentation Only)")]
        [SerializeField] private Toggle _vibrationToggle;
        [SerializeField] private Toggle _cameraShakeToggle;

        [Header("Version Display")]
        [SerializeField] private TMP_Text _versionText;

        public event Action CloseRequested;

        public Canvas Canvas => _canvas;
        public Button CloseButton => _closeButton;
        public Button DimmerButton => _dimmerButton;
        public Slider MusicSlider => _musicSlider;
        public Slider SfxSlider => _sfxSlider;
        public Toggle VibrationToggle => _vibrationToggle;
        public Toggle CameraShakeToggle => _cameraShakeToggle;
        public TMP_Text VersionText => _versionText;

        protected override void Awake()
        {
            base.Awake();

            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>();
            }

            EnsureCameraAssigned();
            InitializeDefaultPresentationState();
            RegisterListeners();
        }

        public override void Show(float duration = 0.25f)
        {
            EnsureCameraAssigned();
            base.Show(duration);
        }

        protected virtual void OnDestroy()
        {
            UnregisterListeners();
            CloseRequested = null;
        }

        public void RequestClose()
        {
            HandleCloseRequested();
        }

        public void SetVersionText(TMP_Text versionText)
        {
            _versionText = versionText;
            if (_versionText != null)
            {
                _versionText.text = $"v{Application.version}";
            }
        }

        private void EnsureCameraAssigned()
        {
            if (_canvas != null && _canvas.worldCamera == null)
            {
                var mainCam = Camera.main;
                if (mainCam != null)
                {
                    _canvas.worldCamera = mainCam;
                    _canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    _canvas.planeDistance = 10f;
                }
            }
        }

        private void InitializeDefaultPresentationState()
        {
            if (_musicSlider != null)
            {
                _musicSlider.value = 0.75f;
            }

            if (_sfxSlider != null)
            {
                _sfxSlider.value = 0.75f;
            }

            if (_vibrationToggle != null)
            {
                _vibrationToggle.isOn = true;
            }

            if (_cameraShakeToggle != null)
            {
                _cameraShakeToggle.isOn = true;
            }

            if (_versionText != null)
            {
                _versionText.text = $"v{Application.version}";
            }
        }

        private void RegisterListeners()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(HandleCloseRequested);
            }

            if (_dimmerButton != null)
            {
                _dimmerButton.onClick.AddListener(HandleCloseRequested);
            }

        }

        private void UnregisterListeners()
        {
            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(HandleCloseRequested);
            }

            if (_dimmerButton != null)
            {
                _dimmerButton.onClick.RemoveListener(HandleCloseRequested);
            }

        }

        private void HandleCloseRequested()
        {
            CloseRequested?.Invoke();
        }

    }
}
