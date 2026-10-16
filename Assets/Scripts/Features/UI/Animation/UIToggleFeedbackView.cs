using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlockBlast.Features.UI.Animation
{
    public sealed class UIToggleFeedbackView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Toggle _toggle;
        [SerializeField] private Image _track;
        [SerializeField] private Image _knob;
        [SerializeField] private Sprite _offSprite;
        [SerializeField] private Sprite _onSprite;
        [SerializeField] private Vector2 _offPosition = new(-43f, 0f);
        [SerializeField] private Vector2 _onPosition = new(43f, 0f);
        [SerializeField, Min(.01f)] private float _duration = .16f;

        private void Awake()
        {
            if (_toggle == null) _toggle = GetComponent<Toggle>();
            ApplyImmediate(_toggle != null && _toggle.isOn);
        }

        private void OnEnable()
        {
            if (_toggle == null) _toggle = GetComponent<Toggle>();
            if (_toggle == null) return;
            _toggle.onValueChanged.RemoveListener(SetState);
            _toggle.onValueChanged.AddListener(SetState);
            ApplyImmediate(_toggle.isOn);
        }

        private void OnDisable()
        {
            if (_toggle != null) _toggle.onValueChanged.RemoveListener(SetState);
            KillTweens();
            if (_toggle != null) ApplyImmediate(_toggle.isOn);
        }

        public void SetState(bool isOn)
        {
            if (_track != null) _track.sprite = isOn ? _onSprite : _offSprite;
            if (_knob == null) return;
            _knob.rectTransform.DOKill();
            _knob.rectTransform.DOAnchorPos(isOn ? _onPosition : _offPosition, _duration)
                .SetEase(Ease.OutBack).SetTarget(_knob.rectTransform).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        public void OnPointerDown(PointerEventData eventData) => ScaleKnob(.92f);
        public void OnPointerUp(PointerEventData eventData) => ScaleKnob(1f);
        public void OnPointerExit(PointerEventData eventData) => ScaleKnob(1f);

        private void ApplyImmediate(bool isOn)
        {
            if (_track != null) _track.sprite = isOn ? _onSprite : _offSprite;
            if (_knob == null) return;
            _knob.rectTransform.anchoredPosition = isOn ? _onPosition : _offPosition;
            _knob.rectTransform.localScale = Vector3.one;
        }

        private void ScaleKnob(float scale)
        {
            if (_knob == null) return;
            _knob.rectTransform.DOKill();
            _knob.rectTransform.DOScale(scale, .08f).SetEase(Ease.OutQuad)
                .SetTarget(_knob.rectTransform).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void KillTweens()
        {
            if (_knob == null) return;
            _knob.rectTransform.DOKill();
            _knob.rectTransform.localScale = Vector3.one;
        }
    }
}
