using System;
using System.Threading;
using System.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace BlockBlast.Features.Lobby
{
    /// <summary>
    /// Passive View handling idle bobbing, press squash/bounce, marking flash, and four-edge particle burst VFX
    /// for a single 3D Lobby world button.
    /// </summary>
    public sealed class LobbyWorldButtonFeedbackView : MonoBehaviour
    {
        [System.Serializable]
        public sealed class EdgeEffect
        {
            [Tooltip("Anchor transform for this edge.")]
            [SerializeField] private Transform _anchor;

            [Tooltip("Wood chips debris particle system along this edge.")]
            [SerializeField] private ParticleSystem _debris;

            [Tooltip("Smoke puff particle system along this edge.")]
            [SerializeField] private ParticleSystem _smoke;

            public Transform Anchor { get => _anchor; set => _anchor = value; }
            public ParticleSystem Debris { get => _debris; set => _debris = value; }
            public ParticleSystem Smoke { get => _smoke; set => _smoke = value; }
        }

        [Header("Targets")]
        [Tooltip("The source world button view to listen for click events.")]
        [SerializeField] private LobbyWorldButtonView _buttonView;

        [Tooltip("The root transform animated during idle and press.")]
        [SerializeField] private Transform _animatedRoot;

        [Tooltip("The face label TMP_Text that flashes upon press.")]
        [SerializeField] private TMP_Text _markingText;

        [Header("Edge VFX")]
        [Tooltip("Exact four edge effects: Front (+Z), Back (-Z), Right (+X), Left (-X).")]
        [SerializeField] private EdgeEffect[] _edgeEffects = new EdgeEffect[4];

        [Header("Idle Motion")]
        [Tooltip("Absolute world-Y movement bounds [min, max] for independent idle travel.")]
        [SerializeField] private Vector2 _idleWorldYRange = new Vector2(-0.20f, 1.00f);

        [Tooltip("Duration range [min, max] in seconds for each independent idle travel leg.")]
        [SerializeField, Min(0.01f)] private Vector2 _idleLegDurationRange = new Vector2(0.85f, 1.35f);

        [Header("Press Motion")]
        [Tooltip("Downward vertical distance in meters during press.")]
        [SerializeField, Min(0f)] private float _pressDepth = 0.070f;

        [Tooltip("Relative local scale squash during maximum compression.")]
        [SerializeField] private Vector3 _pressSquashScale = new Vector3(1.025f, 0.88f, 1.025f);

        [Tooltip("Compression duration in seconds.")]
        [SerializeField, Min(0.01f)] private float _pressDownDuration = 0.07f;

        [Tooltip("Upward rebound overshoot beyond base Y in meters.")]
        [SerializeField, Min(0f)] private float _reboundOvershoot = 0.018f;

        [Tooltip("Rebound duration in seconds.")]
        [SerializeField, Min(0.01f)] private float _reboundDuration = 0.16f;

        [Tooltip("Settling duration back to base pose in seconds.")]
        [SerializeField, Min(0.10f)] private float _settleDuration = 0.10f;

        [Header("Marking Flash")]
        [Tooltip("Warm flash color for marking label upon maximum compression.")]
        [SerializeField] private Color _markingFlashColor = new Color(1f, 0.96f, 0.82f, 1f);

        [Tooltip("Flash in duration in seconds.")]
        [SerializeField, Min(0.01f)] private float _markingFlashInDuration = 0.06f;

        [Tooltip("Flash out duration in seconds.")]
        [SerializeField, Min(0.01f)] private float _markingFlashOutDuration = 0.18f;

        private Vector3 _baseWorldPosition;
        private Vector3 _baseLocalScale;
        private Color _baseMarkingColor = Color.white;
        private bool _baseStateCaptured;
        private bool _idleTargetsUpperBound;
        private bool _isPressing;
        private float _currentPressStartY;

        private Tween _idleTween;
        private Sequence _pressSequence;
        private Sequence _markingSequence;
        private TaskCompletionSource<bool> _pressMotionCompletion;

        public LobbyWorldButtonView ButtonView { get => _buttonView; set => _buttonView = value; }
        public Transform AnimatedRoot { get => _animatedRoot; set => _animatedRoot = value; }
        public TMP_Text MarkingText { get => _markingText; set => _markingText = value; }
        public EdgeEffect[] EdgeEffects { get => _edgeEffects; set => _edgeEffects = value; }
        public Vector2 IdleWorldYRange { get => _idleWorldYRange; set => _idleWorldYRange = value; }
        public Vector2 IdleLegDurationRange { get => _idleLegDurationRange; set => _idleLegDurationRange = value; }

        public Vector2 GetIdleWorldYRange()
        {
            float min = Mathf.Min(_idleWorldYRange.x, _idleWorldYRange.y);
            float max = Mathf.Max(_idleWorldYRange.x, _idleWorldYRange.y);
            return new Vector2(min, max);
        }

        public Vector2 GetIdleLegDurationRange()
        {
            float rawMin = Mathf.Min(_idleLegDurationRange.x, _idleLegDurationRange.y);
            float rawMax = Mathf.Max(_idleLegDurationRange.x, _idleLegDurationRange.y);
            float min = Mathf.Max(0.01f, rawMin);
            float max = Mathf.Max(min, rawMax);
            return new Vector2(min, max);
        }

        public float GetRandomIdleLegDuration()
        {
            Vector2 range = GetIdleLegDurationRange();
            return UnityEngine.Random.Range(range.x, range.y);
        }

        public void SetWorldY(float y)
        {
            if (_animatedRoot == null)
            {
                return;
            }

            _animatedRoot.position = new Vector3(_baseWorldPosition.x, y, _baseWorldPosition.z);
        }

        public void RandomizeIdleStartPosition()
        {
            if (_animatedRoot == null)
            {
                return;
            }

            Vector2 range = GetIdleWorldYRange();
            float randomY = UnityEngine.Random.Range(range.x, range.y);
            SetWorldY(randomY);
            _animatedRoot.localScale = _baseLocalScale;
        }

        private void Awake()
        {
            InitializeReferences();
            CaptureBaseState();
        }

        private void OnEnable()
        {
            CaptureBaseState();

            if (_buttonView != null)
            {
                _buttonView.Pressed -= HandlePressed;
                _buttonView.Pressed += HandlePressed;
            }

            StartIdle();
        }

        private void OnDisable()
        {
            if (_buttonView != null)
            {
                _buttonView.Pressed -= HandlePressed;
            }

            StopFeedbackAndRestoreBaseState();
            ClearAllEdgeParticles();
        }

        private void OnDestroy()
        {
            if (_buttonView != null)
            {
                _buttonView.Pressed -= HandlePressed;
            }

            StopFeedbackAndRestoreBaseState();
            ClearAllEdgeParticles();
        }

        public void InitializeReferences()
        {
            if (_buttonView == null)
            {
                _buttonView = GetComponent<LobbyWorldButtonView>();
            }

            if (_animatedRoot == null)
            {
                _animatedRoot = transform;
            }

            if (_markingText == null)
            {
                _markingText = GetComponentInChildren<TMP_Text>(true);
            }

        }

        public void CaptureBaseState(bool force = false)
        {
            if (_baseStateCaptured && !force)
            {
                return;
            }

            if (_animatedRoot == null)
            {
                _animatedRoot = transform;
            }

            _baseWorldPosition = _animatedRoot.position;
            _baseLocalScale = _animatedRoot.localScale;

            if (_markingText != null)
            {
                _baseMarkingColor = _markingText.color;
            }

            _baseStateCaptured = true;
        }

        public void StartIdle()
        {
            if (!isActiveAndEnabled || _animatedRoot == null)
            {
                return;
            }

            KillIdleTween();

            RandomizeIdleStartPosition();
            _idleTargetsUpperBound = UnityEngine.Random.value > 0.5f;
            PlayNextIdleLeg();
        }

        private void PlayNextIdleLeg()
        {
            if (!isActiveAndEnabled || _animatedRoot == null)
            {
                return;
            }

            KillIdleTween();

            Vector2 yRange = GetIdleWorldYRange();
            float targetY = _idleTargetsUpperBound ? yRange.y : yRange.x;
            _idleTargetsUpperBound = !_idleTargetsUpperBound;

            _idleTween = _animatedRoot.DOMoveY(targetY, GetRandomIdleLegDuration())
                .SetEase(Ease.InOutSine)
                .SetTarget(_animatedRoot)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(PlayNextIdleLeg);
        }

        private void HandlePressed()
        {
            PlayPressSequence();
        }

        public void PlayPressSequence()
        {
            if (_animatedRoot == null)
            {
                return;
            }

            KillIdleTween();
            KillPressSequence();
            KillMarkingSequence();

            if (_markingText != null && _baseStateCaptured)
            {
                _markingText.color = _baseMarkingColor;
            }

            if (!_isPressing)
            {
                _currentPressStartY = _animatedRoot.position.y;
            }
            _isPressing = true;

            _animatedRoot.localScale = _baseLocalScale;
            SetWorldY(_currentPressStartY);

            _pressMotionCompletion = new TaskCompletionSource<bool>();

            Vector3 compressedScale = new Vector3(
                _baseLocalScale.x * _pressSquashScale.x,
                _baseLocalScale.y * _pressSquashScale.y,
                _baseLocalScale.z * _pressSquashScale.z);

            _pressSequence = DOTween.Sequence();
            _pressSequence.SetTarget(_animatedRoot);
            _pressSequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);

            // Phase 1: Compression down and squash
            _pressSequence.Append(_animatedRoot.DOMoveY(_currentPressStartY - _pressDepth, _pressDownDuration).SetEase(Ease.InQuad));
            _pressSequence.Join(_animatedRoot.DOScale(compressedScale, _pressDownDuration).SetEase(Ease.InQuad));

            // Phase 2: Maximum compression burst and marking flash
            _pressSequence.AppendCallback(() =>
            {
                PlayParticleBurst();
                PlayMarkingFlash();
            });

            // Phase 3: Rebound overshoot
            _pressSequence.Append(_animatedRoot.DOMoveY(_currentPressStartY + _reboundOvershoot, _reboundDuration).SetEase(Ease.OutBack));
            _pressSequence.Join(_animatedRoot.DOScale(_baseLocalScale, _reboundDuration).SetEase(Ease.OutBack));

            // Phase 4: Settle to canonical pose
            _pressSequence.Append(_animatedRoot.DOMoveY(_currentPressStartY, _settleDuration).SetEase(Ease.InSine));

            // Phase 5: Complete & resume idle
            _pressSequence.OnComplete(() =>
            {
                _isPressing = false;
                if (_animatedRoot != null)
                {
                    SetWorldY(_currentPressStartY);
                    _animatedRoot.localScale = _baseLocalScale;
                }

                Vector2 yRange = GetIdleWorldYRange();
                if (_currentPressStartY >= yRange.y - 0.001f)
                {
                    _idleTargetsUpperBound = false;
                }
                else if (_currentPressStartY <= yRange.x + 0.001f)
                {
                    _idleTargetsUpperBound = true;
                }

                PlayNextIdleLeg();
                _pressMotionCompletion?.TrySetResult(true);
            });
        }

        private void PlayParticleBurst()
        {
            if (_edgeEffects != null)
            {
                for (int i = 0; i < _edgeEffects.Length; i++)
                {
                    var edge = _edgeEffects[i];
                    if (edge == null) continue;
                    RestartParticle(edge.Debris);
                    RestartParticle(edge.Smoke);
                }
            }
        }

        private static void RestartParticle(ParticleSystem ps)
        {
            if (ps == null) return;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
        }

        private void ClearAllEdgeParticles()
        {
            if (_edgeEffects != null)
            {
                for (int i = 0; i < _edgeEffects.Length; i++)
                {
                    var edge = _edgeEffects[i];
                    if (edge == null) continue;
                    if (edge.Debris != null)
                    {
                        edge.Debris.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                    if (edge.Smoke != null)
                    {
                        edge.Smoke.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                }
            }
        }

        private void PlayMarkingFlash()
        {
            if (_markingText == null)
            {
                return;
            }

            KillMarkingSequence();
            _markingText.color = _baseMarkingColor;

            _markingSequence = DOTween.Sequence();
            _markingSequence.SetTarget(_animatedRoot);
            _markingSequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);
            _markingSequence.Append(_markingText.DOColor(_markingFlashColor, _markingFlashInDuration).SetEase(Ease.InQuad));
            _markingSequence.Append(_markingText.DOColor(_baseMarkingColor, _markingFlashOutDuration).SetEase(Ease.OutQuad));
            _markingSequence.OnKill(() =>
            {
                if (_markingText != null)
                {
                    _markingText.color = _baseMarkingColor;
                }
            });
        }

        public void StopFeedbackAndRestoreBaseState()
        {
            _isPressing = false;
            KillIdleTween();
            KillPressSequence();
            KillMarkingSequence();

            if (_animatedRoot != null)
            {
                _animatedRoot.DOKill();
                if (_baseStateCaptured)
                {
                    _animatedRoot.position = _baseWorldPosition;
                    _animatedRoot.localScale = _baseLocalScale;
                }
            }

            if (_markingText != null)
            {
                _markingText.DOKill();
                if (_baseStateCaptured)
                {
                    _markingText.color = _baseMarkingColor;
                }
            }
        }

        /// <summary>
        /// Awaits completion of the current press transform motion.
        /// Edge particles and marking flash do not delay this task.
        /// Returns immediately if no press is running.
        /// </summary>
        public Task WaitForCurrentPressMotionAsync(CancellationToken cancellationToken = default)
        {
            Task pressMotionTask = _pressMotionCompletion?.Task;
            if (pressMotionTask == null || pressMotionTask.IsCompleted)
            {
                return Task.CompletedTask;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            if (!cancellationToken.CanBeCanceled)
            {
                return pressMotionTask;
            }

            var tcs = new TaskCompletionSource<bool>();
            var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            pressMotionTask.ContinueWith(t =>
            {
                registration.Dispose();
                if (t.IsCanceled)
                {
                    tcs.TrySetCanceled();
                }
                else if (t.IsFaulted)
                {
                    tcs.TrySetException(t.Exception.InnerExceptions);
                }
                else
                {
                    tcs.TrySetResult(true);
                }
            }, TaskScheduler.Default);

            return tcs.Task;
        }

        private void KillIdleTween()
        {
            if (_idleTween != null && _idleTween.IsActive())
            {
                _idleTween.Kill();
            }

            _idleTween = null;
        }

        private void KillPressSequence()
        {
            if (_pressSequence != null && _pressSequence.IsActive())
            {
                _pressSequence.Kill();
            }

            _pressSequence = null;
            _pressMotionCompletion?.TrySetCanceled();
            _pressMotionCompletion = null;
        }

        private void KillMarkingSequence()
        {
            if (_markingSequence != null && _markingSequence.IsActive())
            {
                _markingSequence.Kill();
            }

            _markingSequence = null;
        }
    }
}
