using DG.Tweening;
using UnityEngine;

namespace BlockBlast.Features.WorldLighting
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    public sealed class DirectionalSunCycleView : MonoBehaviour
    {
        [Header("Cycle")]
        [Tooltip("Duration in seconds for a full sunrise-to-sunset-to-sunrise loop.")]
        [SerializeField, Min(2f)] private float _fullCycleDuration = 120f;
        [Tooltip("Continue the cycle while Time.timeScale is zero.")]
        [SerializeField] private bool _useUnscaledTime = true;

        [Header("Sun Arc")]
        [Tooltip("Rotation X at sunrise and sunset.")]
        [SerializeField, Range(0f, 90f)] private float _horizonPitch = 20f;
        [Tooltip("Rotation X at the highest point of the sun arc.")]
        [SerializeField, Range(0f, 90f)] private float _noonPitch = 55f;
        [Tooltip("Rotation Y at sunrise.")]
        [SerializeField] private float _sunriseYaw = -55f;
        [Tooltip("Rotation Y at sunset.")]
        [SerializeField] private float _sunsetYaw = 37f;

        [Header("Color Temperature")]
        [Tooltip("Warm color temperature at sunrise and sunset.")]
        [SerializeField, Min(1000f)] private float _warmTemperature = 4000f;
        [Tooltip("Cool color temperature at the highest point of the sun arc.")]
        [SerializeField, Min(1000f)] private float _noonTemperature = 6000f;

        private Light _directionalLight;
        private Quaternion _canonicalLocalRotation;
        private bool _canonicalUseColorTemperature;
        private float _canonicalColorTemperature;
        private Tweener _cycleTween;
        private float _progress;
        private bool _hasCanonicalState;

        private void Awake()
        {
            _directionalLight = GetComponent<Light>();

            if (_directionalLight == null || _directionalLight.type != LightType.Directional)
            {
                Debug.LogError("DirectionalSunCycleView requires a Directional Light.", this);
                enabled = false;
                return;
            }

            _canonicalLocalRotation = transform.localRotation;
            _canonicalUseColorTemperature = _directionalLight.useColorTemperature;
            _canonicalColorTemperature = _directionalLight.colorTemperature;
            _hasCanonicalState = true;
        }

        private void OnEnable()
        {
            if (_directionalLight == null || !_hasCanonicalState)
            {
                return;
            }

            StartCycle();
        }

        private void OnDisable()
        {
            StopCycleAndRestore();
        }

        private void OnDestroy()
        {
            StopCycleAndRestore();
        }

        private void OnValidate()
        {
            _fullCycleDuration = Mathf.Max(2f, _fullCycleDuration);
            _horizonPitch = Mathf.Clamp(_horizonPitch, 0f, 90f);
            _noonPitch = Mathf.Clamp(_noonPitch, _horizonPitch, 90f);
            _warmTemperature = Mathf.Max(1000f, _warmTemperature);
            _noonTemperature = Mathf.Max(_warmTemperature, _noonTemperature);
        }

        private void StartCycle()
        {
            StopCycleAndRestore();

            _directionalLight.useColorTemperature = true;
            _progress = 0f;
            ApplySunState(_progress);

            _cycleTween = DOTween.To(
                    () => _progress,
                    value =>
                    {
                        _progress = value;
                        ApplySunState(value);
                    },
                    1f,
                    _fullCycleDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(_useUnscaledTime)
                .SetTarget(transform)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void StopCycleAndRestore()
        {
            _cycleTween?.Kill();
            _cycleTween = null;
            _progress = 0f;

            if (!_hasCanonicalState || _directionalLight == null)
            {
                return;
            }

            transform.localRotation = _canonicalLocalRotation;
            _directionalLight.useColorTemperature = _canonicalUseColorTemperature;
            _directionalLight.colorTemperature = _canonicalColorTemperature;
        }

        private void ApplySunState(float progress)
        {
            float height01 = Mathf.Sin(progress * Mathf.PI);
            float pitch = Mathf.Lerp(_horizonPitch, _noonPitch, height01);
            float yaw = Mathf.Lerp(_sunriseYaw, _sunsetYaw, progress);

            transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
            _directionalLight.colorTemperature = Mathf.Lerp(
                _warmTemperature,
                _noonTemperature,
                height01);
        }
    }
}
