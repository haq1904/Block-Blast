using DG.Tweening;
using UnityEngine;

namespace BlockBlast.Features.UI.Animation
{
    public sealed class UIPanelTransitionView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _panel;
        [SerializeField, Min(.01f)] private float _showDuration = .24f;
        [SerializeField, Min(.01f)] private float _hideDuration = .18f;
        [SerializeField, Range(.7f, 1f)] private float _hiddenScale = .92f;

        private Vector3 _initialScale;
        private Sequence _sequence;

        private void Awake()
        {
            if (_panel == null) _panel = transform as RectTransform;
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            _initialScale = _panel != null ? _panel.localScale : Vector3.one;
        }

        public void Show()
        {
            KillSequence();
            gameObject.SetActive(true);
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _panel.localScale = _initialScale * _hiddenScale;
            _sequence = DOTween.Sequence().SetTarget(_panel).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            _sequence.Join(_canvasGroup.DOFade(1f, _showDuration));
            _sequence.Join(_panel.DOScale(_initialScale, _showDuration).SetEase(Ease.OutBack));
            _sequence.OnComplete(() => { _canvasGroup.interactable = true; _canvasGroup.blocksRaycasts = true; });
        }

        public void Hide()
        {
            KillSequence();
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            _sequence = DOTween.Sequence().SetTarget(_panel).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            _sequence.Join(_canvasGroup.DOFade(0f, _hideDuration));
            _sequence.Join(_panel.DOScale(_initialScale * _hiddenScale, _hideDuration).SetEase(Ease.InQuad));
            _sequence.OnComplete(() => gameObject.SetActive(false));
        }

        public void SetImmediate(bool isVisible)
        {
            KillSequence();
            gameObject.SetActive(isVisible);
            _canvasGroup.alpha = isVisible ? 1f : 0f;
            _canvasGroup.interactable = isVisible;
            _canvasGroup.blocksRaycasts = isVisible;
            _panel.localScale = isVisible ? _initialScale : _initialScale * _hiddenScale;
        }

        private void OnDisable()
        {
            KillSequence();
            if (_panel != null) _panel.localScale = _initialScale;
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
        }

        private void KillSequence()
        {
            _sequence?.Kill();
            _sequence = null;
            if (_panel != null) _panel.DOKill();
            if (_canvasGroup != null) _canvasGroup.DOKill();
        }
    }
}
