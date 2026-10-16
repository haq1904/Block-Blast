using DG.Tweening;
using UnityEngine;

namespace BlockBlast.Features.Lobby
{
    /// <summary>
    /// Base presentation component for Lobby panels/overlays providing clean DOTween fade transitions.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class LobbyPanelView : MonoBehaviour
    {
        private CanvasGroup _canvasGroup;
        private Tween _fadeTween;

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        public virtual void Show(float duration = 0.25f)
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();

            _fadeTween?.Kill();
            gameObject.SetActive(true);

            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;

            if (duration <= 0f)
            {
                _canvasGroup.alpha = 1f;
                return;
            }

            _fadeTween = _canvasGroup.DOFade(1f, duration)
                .SetTarget(gameObject)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        public virtual void Hide(float duration = 0.2f)
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();

            _fadeTween?.Kill();
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            if (duration <= 0f)
            {
                _canvasGroup.alpha = 0f;
                gameObject.SetActive(false);
                return;
            }

            _fadeTween = _canvasGroup.DOFade(0f, duration)
                .SetTarget(gameObject)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() => gameObject.SetActive(false));
        }

        public virtual void SetImmediate(bool isVisible)
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();

            _fadeTween?.Kill();
            _canvasGroup.alpha = isVisible ? 1f : 0f;
            _canvasGroup.interactable = isVisible;
            _canvasGroup.blocksRaycasts = isVisible;
            gameObject.SetActive(isVisible);
        }

        protected virtual void OnDisable()
        {
            _fadeTween?.Kill();
            _fadeTween = null;
        }
    }
}
