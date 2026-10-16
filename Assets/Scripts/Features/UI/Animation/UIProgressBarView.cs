using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BlockBlast.Features.UI.Animation
{
    public sealed class UIProgressBarView : MonoBehaviour
    {
        [SerializeField] private Image _fill;
        [SerializeField, Min(.01f)] private float _duration = .25f;

        private Tween _tween;

        public void SetProgress(float normalizedValue, bool animate = true)
        {
            if (_fill == null) return;
            float target = Mathf.Clamp01(normalizedValue);
            _tween?.Kill();
            _fill.DOKill();
            if (!animate) { _fill.fillAmount = target; return; }
            _tween = _fill.DOFillAmount(target, _duration).SetEase(Ease.OutQuad).SetTarget(_fill).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void OnDisable()
        {
            _tween?.Kill();
            _tween = null;
            if (_fill != null) _fill.DOKill();
        }
    }
}
