using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BlockBlast.Features.GameFlow.Intro
{
    /// <summary>
    /// Passive view managing the static poster presentation (color/sprite) and the continuous loading shape spin animation.
    /// </summary>
    public sealed class IntroLoadingView : MonoBehaviour
    {
        [Header("UI Elements")]
        [Tooltip("Full-bleed background image representing the poster (supports solid color or texture).")]
        [SerializeField] private Image _posterImage;

        [Tooltip("UI root of the block shape acting as the loading spinner.")]
        [SerializeField] private RectTransform _spinningShape;

        [Tooltip("Optional full-screen button or tap area to detect screen taps.")]
        [SerializeField] private Button _tapArea;

        [Header("Animation Config")]
        [Tooltip("Duration in seconds for one full 360-degree rotation.")]
        [SerializeField, Min(0.1f)] private float _spinDuration = 1.2f;

        [Tooltip("Rotation direction (negative Z is clockwise).")]
        [SerializeField] private Vector3 _spinRotation = new Vector3(0f, 0f, -360f);

        private Tween _spinTween;

        public event Action ScreenTapped;

        private void Awake()
        {
            if (_posterImage != null)
            {
                _posterImage.raycastTarget = false;
            }

            if (_tapArea != null)
            {
                _tapArea.onClick.AddListener(NotifyScreenTapped);
            }
        }

        public void StartSpinning()
        {
            StopSpinning();

            if (_spinningShape == null)
            {
                return;
            }

            _spinningShape.localRotation = Quaternion.identity;
            _spinTween = _spinningShape
                .DORotate(_spinRotation, _spinDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetTarget(_spinningShape)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .SetUpdate(true);
        }

        public void StopSpinning()
        {
            if (_spinTween != null)
            {
                _spinTween.Kill();
                _spinTween = null;
            }
        }

        public void NotifyScreenTapped()
        {
            ScreenTapped?.Invoke();
        }

        private void OnDisable()
        {
            StopSpinning();
        }

        private void OnDestroy()
        {
            if (_tapArea != null)
            {
                _tapArea.onClick.RemoveListener(NotifyScreenTapped);
            }

            StopSpinning();
        }
    }
}
