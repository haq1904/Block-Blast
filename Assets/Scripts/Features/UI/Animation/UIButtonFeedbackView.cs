using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BlockBlast.Features.UI.Animation
{
    public sealed class UIButtonFeedbackView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform _target;
        [SerializeField, Range(.8f, 1f)] private float _pressedScale = .96f;
        [SerializeField, Min(.01f)] private float _duration = .08f;

        private Vector3 _initialScale;

        private void Awake()
        {
            if (_target == null) _target = transform as RectTransform;
            _initialScale = _target != null ? _target.localScale : Vector3.one;
        }

        public void OnPointerDown(PointerEventData eventData) => TweenTo(_initialScale * _pressedScale);
        public void OnPointerUp(PointerEventData eventData) => TweenTo(_initialScale);
        public void OnPointerExit(PointerEventData eventData) => TweenTo(_initialScale);

        private void OnDisable()
        {
            if (_target == null) return;
            _target.DOKill();
            _target.localScale = _initialScale;
        }

        private void TweenTo(Vector3 scale)
        {
            if (_target == null) return;
            _target.DOKill();
            _target.DOScale(scale, _duration).SetEase(Ease.OutQuad).SetTarget(_target).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }
    }
}
