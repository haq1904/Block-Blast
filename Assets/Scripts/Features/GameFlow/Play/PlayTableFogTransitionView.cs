using System;
using System.Threading;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace BlockBlast.Features.GameFlow.Play
{
    /// <summary>
    /// Passive presentation View controlling two fog curtains moving in from the left and right
    /// to cover and reveal the 8x8 play table.
    /// </summary>
    public sealed class PlayTableFogTransitionView : MonoBehaviour
    {
        [Header("Transforms")]
        [Tooltip("Root transform of the left fog curtain.")]
        [SerializeField] private Transform _leftFogRoot;

        [Tooltip("Root transform of the right fog curtain.")]
        [SerializeField] private Transform _rightFogRoot;

        [Tooltip("Optional center anchor for table world position.")]
        [SerializeField] private Transform _tableCenterAnchor;

        [Header("Particle Systems")]
        [Tooltip("Particle system for the left fog curtain.")]
        [SerializeField] private ParticleSystem _leftParticles;

        [Tooltip("Particle system for the right fog curtain.")]
        [SerializeField] private ParticleSystem _rightParticles;

        [Header("Dimensions")]
        [Tooltip("Width of the 8x8 gameplay grid.")]
        [SerializeField, Min(1f)] private float _gridWidth = 8f;

        [Tooltip("Depth of the 8x8 gameplay grid.")]
        [SerializeField, Min(1f)] private float _gridDepth = 8f;

        [Tooltip("Side spawn offset outside grid edges in units.")]
        [SerializeField, Range(2f, 10f)] private float _sideSpawnOffset = 2.5f;

        [Tooltip("Center offset when covered, allowing a slight overlap in the middle.")]
        [SerializeField, Min(0f)] private float _coveredCenterOffset = 2f;

        [Header("Timing")]
        [Tooltip("Duration in seconds for the fog to cover the table.")]
        [SerializeField, Min(0.01f)] private float _coverDuration = 0.75f;

        [Tooltip("Hold duration in seconds after PlayScene becomes the active scene before revealing.")]
        [SerializeField, Min(0f)] private float _holdAfterPlaySceneActive = 0.45f;

        [Tooltip("Duration in seconds for the fog to reveal the table.")]
        [SerializeField, Min(0.01f)] private float _revealDuration = 0.9f;

        [Header("Easing")]
        [Tooltip("Ease function for covering animation.")]
        [SerializeField] private Ease _coverEase = Ease.InOutSine;

        [Tooltip("Ease function for revealing animation.")]
        [SerializeField] private Ease _revealEase = Ease.InOutSine;

        private Sequence _coverSequence;
        private Sequence _revealSequence;
        private TaskCompletionSource<bool> _coverTcs;
        private bool _isCovered;

        public Transform LeftFogRoot
        {
            get => _leftFogRoot;
            set => _leftFogRoot = value;
        }

        public Transform RightFogRoot
        {
            get => _rightFogRoot;
            set => _rightFogRoot = value;
        }

        public Transform TableCenterAnchor
        {
            get => _tableCenterAnchor;
            set => _tableCenterAnchor = value;
        }

        public ParticleSystem LeftParticles
        {
            get => _leftParticles;
            set => _leftParticles = value;
        }

        public ParticleSystem RightParticles
        {
            get => _rightParticles;
            set => _rightParticles = value;
        }

        public float GridWidth
        {
            get => _gridWidth;
            set => _gridWidth = Mathf.Max(1f, value);
        }

        public float GridDepth
        {
            get => _gridDepth;
            set => _gridDepth = Mathf.Max(1f, value);
        }

        public float SideSpawnOffset
        {
            get => _sideSpawnOffset;
            set => _sideSpawnOffset = Mathf.Clamp(value, 2f, 10f);
        }

        public float CoveredCenterOffset
        {
            get => _coveredCenterOffset;
            set => _coveredCenterOffset = Mathf.Max(0f, value);
        }

        public float CoverDuration
        {
            get => _coverDuration;
            set => _coverDuration = Mathf.Max(0.01f, value);
        }

        public float HoldAfterPlaySceneActive
        {
            get => _holdAfterPlaySceneActive;
            set => _holdAfterPlaySceneActive = Mathf.Max(0f, value);
        }

        public float RevealDuration
        {
            get => _revealDuration;
            set => _revealDuration = Mathf.Max(0.01f, value);
        }

        public bool IsCovered => _isCovered;

        public float HalfWidth => _gridWidth * 0.5f;
        public float StartDistance => HalfWidth + _sideSpawnOffset;
        public Vector3 LeftStartPosition => new Vector3(-StartDistance, 0f, 0f);
        public Vector3 RightStartPosition => new Vector3(StartDistance, 0f, 0f);
        public Vector3 LeftCoverPosition => new Vector3(-_coveredCenterOffset, 0f, 0f);
        public Vector3 RightCoverPosition => new Vector3(_coveredCenterOffset, 0f, 0f);

        private static readonly Vector3 InitialScale = new Vector3(0.92f, 0.92f, 0.92f);

        private void Awake()
        {
            ResetImmediate();
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
#endif
            ResetImmediate();
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
#endif
            ResetImmediate();
        }

        /// <summary>
        /// Plays the cover animation, moving both fog curtains from the sides toward the table center.
        /// Resolves when the table is fully covered.
        /// </summary>
        public Task PlayCoverAsync(CancellationToken cancellationToken = default)
        {
            KillSequences();
            _isCovered = false;

            if (cancellationToken.IsCancellationRequested)
            {
                ResetImmediate();
                return Task.FromCanceled(cancellationToken);
            }

            // Prepare transforms
            if (_leftFogRoot != null)
            {
                _leftFogRoot.localPosition = LeftStartPosition;
                _leftFogRoot.localScale = InitialScale;
            }

            if (_rightFogRoot != null)
            {
                _rightFogRoot.localPosition = RightStartPosition;
                _rightFogRoot.localScale = InitialScale;
            }

            // Start particles
            if (_leftParticles != null)
            {
                RestartParticles(_leftParticles);
            }

            if (_rightParticles != null)
            {
                RestartParticles(_rightParticles);
            }

            _coverTcs = new TaskCompletionSource<bool>();
            CancellationTokenRegistration registration = default;
            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() =>
                {
                    KillCoverSequence();
                    ResetImmediate();
                    _coverTcs?.TrySetCanceled(cancellationToken);
                });
            }

            _coverSequence = DOTween.Sequence();
            _coverSequence.SetTarget(gameObject);
            _coverSequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (_leftFogRoot != null)
            {
                _coverSequence.Append(_leftFogRoot.DOLocalMove(LeftCoverPosition, _coverDuration).SetEase(_coverEase));
                _coverSequence.Join(_leftFogRoot.DOScale(Vector3.one, _coverDuration).SetEase(_coverEase));
            }

            if (_rightFogRoot != null)
            {
                _coverSequence.Join(_rightFogRoot.DOLocalMove(RightCoverPosition, _coverDuration).SetEase(_coverEase));
                _coverSequence.Join(_rightFogRoot.DOScale(Vector3.one, _coverDuration).SetEase(_coverEase));
            }

            _coverSequence.OnComplete(() =>
            {
                registration.Dispose();
                _isCovered = true;
                _coverTcs?.TrySetResult(true);
            });

            _coverSequence.OnKill(() =>
            {
                registration.Dispose();
                _coverTcs?.TrySetCanceled();
            });

            return _coverTcs.Task;
        }

        /// <summary>
        /// Plays the reveal sequence: waits for configured hold duration, stops emission,
        /// and pulls both curtains back to their starting positions.
        /// </summary>
        public void PlayRevealAfterActivation()
        {
            KillRevealSequence();

            _revealSequence = DOTween.Sequence();
            _revealSequence.SetTarget(gameObject);
            _revealSequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (_holdAfterPlaySceneActive > 0f)
            {
                _revealSequence.AppendInterval(_holdAfterPlaySceneActive);
            }

            _revealSequence.AppendCallback(() =>
            {
                if (_leftParticles != null)
                {
                    _leftParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }

                if (_rightParticles != null)
                {
                    _rightParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            });

            if (_leftFogRoot != null)
            {
                _revealSequence.Append(_leftFogRoot.DOLocalMove(LeftStartPosition, _revealDuration).SetEase(_revealEase));
                _revealSequence.Join(_leftFogRoot.DOScale(InitialScale, _revealDuration).SetEase(_revealEase));
            }

            if (_rightFogRoot != null)
            {
                _revealSequence.Join(_rightFogRoot.DOLocalMove(RightStartPosition, _revealDuration).SetEase(_revealEase));
                _revealSequence.Join(_rightFogRoot.DOScale(InitialScale, _revealDuration).SetEase(_revealEase));
            }

            _revealSequence.OnComplete(() =>
            {
                ResetImmediate();
            });
        }

        /// <summary>
        /// Immediately stops all sequences and particle emissions, restoring canonical starting poses.
        /// </summary>
        public void ResetImmediate()
        {
            KillSequences();
            _isCovered = false;

            if (_leftParticles != null)
            {
                _leftParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (_rightParticles != null)
            {
                _rightParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (_leftFogRoot != null)
            {
                try
                {
                    _leftFogRoot.DOKill();
                }
                catch
                {
                    // Ignore during teardown
                }

                _leftFogRoot.localPosition = LeftStartPosition;
                _leftFogRoot.localScale = InitialScale;
            }

            if (_rightFogRoot != null)
            {
                try
                {
                    _rightFogRoot.DOKill();
                }
                catch
                {
                    // Ignore during teardown
                }

                _rightFogRoot.localPosition = RightStartPosition;
                _rightFogRoot.localScale = InitialScale;
            }
        }

        private void KillSequences()
        {
            KillCoverSequence();
            KillRevealSequence();
        }

        private static void RestartParticles(ParticleSystem particles)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Play(true);
        }

        private void KillCoverSequence()
        {
            if (_coverSequence != null)
            {
                try
                {
                    if (_coverSequence.IsActive())
                    {
                        _coverSequence.Kill();
                    }
                }
                catch
                {
                    // Ignore during teardown
                }

                _coverSequence = null;
            }

            _coverTcs?.TrySetCanceled();
            _coverTcs = null;
        }

        private void KillRevealSequence()
        {
            if (_revealSequence != null)
            {
                try
                {
                    if (_revealSequence.IsActive())
                    {
                        _revealSequence.Kill();
                    }
                }
                catch
                {
                    // Ignore during teardown
                }

                _revealSequence = null;
            }
        }
    }
}
