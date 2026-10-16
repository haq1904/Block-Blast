using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace BlockBlast.Features.Lobby
{
    /// <summary>
    /// Presentation component managing gentle floating idle motion for the 3D Lobby background environment.
    /// Strictly tracks and kills infinite loops on lifecycle transitions.
    /// </summary>
    public sealed class LobbyBackgroundView : MonoBehaviour
    {
        [Header("Floating Elements")]
        [Tooltip("Target transforms for slow floating idle bobbing.")]
        [SerializeField] private Transform[] _floatingRoots;

        [Header("Bobbing Parameters")]
        [SerializeField] private float _bobDistance = 0.15f;
        [SerializeField] private float _bobDuration = 3.0f;

        private readonly List<Tween> _activeTweens = new List<Tween>();
        private readonly List<Vector3> _initialPositions = new List<Vector3>();

        private void Awake()
        {
            if (_floatingRoots == null) return;
            for (int i = 0; i < _floatingRoots.Length; i++)
            {
                if (_floatingRoots[i] != null)
                {
                    _initialPositions.Add(_floatingRoots[i].localPosition);
                }
            }
        }

        private void OnEnable()
        {
            StartIdleAnimations();
        }

        private void OnDisable()
        {
            StopAndResetAnimations();
        }

        public void StartIdleAnimations()
        {
            StopAndResetAnimations();

            if (_floatingRoots == null) return;

            for (int i = 0; i < _floatingRoots.Length; i++)
            {
                Transform target = _floatingRoots[i];
                if (target == null) continue;

                float delay = i * 0.4f;
                Vector3 initialPos = i < _initialPositions.Count ? _initialPositions[i] : target.localPosition;

                Tween tween = target.DOLocalMoveY(initialPos.y + _bobDistance, _bobDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetDelay(delay)
                    .SetTarget(target)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);

                _activeTweens.Add(tween);
            }
        }

        public void StopAndResetAnimations()
        {
            for (int i = 0; i < _activeTweens.Count; i++)
            {
                _activeTweens[i]?.Kill();
            }
            _activeTweens.Clear();

            if (_floatingRoots == null) return;
            for (int i = 0; i < _floatingRoots.Length; i++)
            {
                if (_floatingRoots[i] != null && i < _initialPositions.Count)
                {
                    _floatingRoots[i].DOKill();
                    _floatingRoots[i].localPosition = _initialPositions[i];
                }
            }
        }
    }
}
