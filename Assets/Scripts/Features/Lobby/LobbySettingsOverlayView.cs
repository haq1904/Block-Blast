using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlockBlast.Features.Lobby
{
    /// <summary>
    /// Presentation overlay view for Lobby Settings, managing close events, passive slider/toggle controls, and volume sync.
    /// Does not invoke any services, persistence (PlayerPrefs), or gameplay effects directly.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class LobbySettingsOverlayView : LobbyPanelView
    {
        [Header("Canvas & Close Controls")]
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _dimmerButton;

        [Header("Audio")]
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;

        [Header("Feedback (Presentation Only)")]
        [SerializeField] private Toggle _vibrationToggle;
        [SerializeField] private Toggle _cameraShakeToggle;

        [Header("Version Display")]
        [SerializeField] private TMP_Text _versionText;

        public event Action CloseRequested;
        public event Action<float> MusicVolumeChanged;
        public event Action<float> SfxVolumeChanged;

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
            MusicVolumeChanged = null;
            SfxVolumeChanged = null;
        }

        public void RequestClose()
        {
            HandleCloseRequested();
        }

        public void SetAudioSliderValues(float music, float sfx)
        {
            _musicSlider?.SetValueWithoutNotify(Mathf.Clamp01(music));
            _sfxSlider?.SetValueWithoutNotify(Mathf.Clamp01(sfx));
        }

        public void SetSlidersForTest(Slider musicSlider, Slider sfxSlider)
        {
            UnregisterListeners();
            _musicSlider = musicSlider;
            _sfxSlider = sfxSlider;
            RegisterListeners();
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

            if (_musicSlider != null)
            {
                _musicSlider.onValueChanged.AddListener(HandleMusicSliderChanged);
            }

            if (_sfxSlider != null)
            {
                _sfxSlider.onValueChanged.AddListener(HandleSfxSliderChanged);
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

            if (_musicSlider != null)
            {
                _musicSlider.onValueChanged.RemoveListener(HandleMusicSliderChanged);
            }

            if (_sfxSlider != null)
            {
                _sfxSlider.onValueChanged.RemoveListener(HandleSfxSliderChanged);
            }
        }

        private void HandleCloseRequested()
        {
            CloseRequested?.Invoke();
        }

        private void HandleMusicSliderChanged(float value)
        {
            MusicVolumeChanged?.Invoke(value);
        }

        private void HandleSfxSliderChanged(float value)
        {
            SfxVolumeChanged?.Invoke(value);
        }
    }
}
