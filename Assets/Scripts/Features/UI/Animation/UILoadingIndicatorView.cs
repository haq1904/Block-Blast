using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace BlockBlast.Features.UI.Animation
{
    public sealed class UILoadingIndicatorView : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private Sprite[] _frames;
        [SerializeField, Min(.02f)] private float _frameDuration = .10f;

        private Sequence _sequence;

        private void OnEnable() => Play();
        private void OnDisable() => StopAndReset();

        public void Play()
        {
            StopAndReset();
            if (_image == null || _frames == null || _frames.Length == 0) return;
            _sequence = DOTween.Sequence().SetTarget(_image).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            for (int i = 0; i < _frames.Length; i++)
            {
                int frameIndex = i;
                _sequence.AppendCallback(() => _image.sprite = _frames[frameIndex]);
                _sequence.AppendInterval(_frameDuration);
            }
            _sequence.SetLoops(-1, LoopType.Restart);
        }

        public void StopAndReset()
        {
            _sequence?.Kill();
            _sequence = null;
            if (_image != null)
            {
                _image.DOKill();
                if (_frames != null && _frames.Length > 0) _image.sprite = _frames[0];
            }
        }
    }
}
