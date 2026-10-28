using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace BlockBlast.Features.Lobby
{
    /// <summary>
    /// Controls the shrink and deactivation animation of the four wood world buttons
    /// after the fog curtain covers the center of the table during Lobby-to-Play transition.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LobbyWorldButtonsExitView : MonoBehaviour
    {
        [Header("Button References")]
        [Tooltip("The four world button feedback views in Lobby scene.")]
        [SerializeField] private LobbyWorldButtonFeedbackView[] _feedbackViews;

        [Header("Exit Animation Timing")]
        [Tooltip("Subtle pre-shrink scale factor relative to canonical local scale.")]
        [SerializeField] private Vector3 _preShrinkScale = new Vector3(1.04f, 0.96f, 1.04f);

        [Tooltip("Duration in seconds of subtle pre-shrink squeeze before collapsing.")]
        [SerializeField, Min(0f)] private float _preShrinkDuration = 0.06f;

        [Tooltip("Duration in seconds to shrink button scale to zero.")]
        [SerializeField, Min(0f)] private float _shrinkDuration = 0.18f;

        [Tooltip("Stagger delay between consecutive button shrinks in seconds.")]
        [SerializeField, Min(0f)] private float _staggerDelay = 0.025f;

        [Header("Easing")]
        [Tooltip("Ease curve for pre-shrink squeeze.")]
        [SerializeField] private Ease _preShrinkEase = Ease.OutQuad;

        [Tooltip("Ease curve for collapsing scale to zero.")]
        [SerializeField] private Ease _shrinkEase = Ease.InBack;

        private struct RootState
        {
            public Transform Transform;
            public GameObject GameObject;
            public Vector3 BaseLocalScale;
            public bool BaseActiveSelf;
        }

        private RootState[] _cachedRoots;
        private bool _rootsCaptured;
        private Sequence _exitSequence;
        private TaskCompletionSource<bool> _exitTcs;
        private CancellationTokenRegistration _cancellationRegistration;
        private bool _isExitCompleted;

        public LobbyWorldButtonFeedbackView[] FeedbackViews
        {
            get => _feedbackViews;
            set
            {
                _feedbackViews = value;
                _rootsCaptured = false;
            }
        }

        public void SetFeedbackViews(IReadOnlyList<LobbyWorldButtonFeedbackView> views)
        {
            ResetImmediate();
            if (views == null || views.Count == 0)
            {
                _feedbackViews = Array.Empty<LobbyWorldButtonFeedbackView>();
                _cachedRoots = null;
                _rootsCaptured = false;
                return;
            }

            _feedbackViews = new LobbyWorldButtonFeedbackView[views.Count];
            for (int i = 0; i < views.Count; i++)
            {
                _feedbackViews[i] = views[i];
            }
            _cachedRoots = null;
            _rootsCaptured = false;
            CaptureRootsIfNeeded();
        }

        public Vector3 PreShrinkScale
        {
            get => _preShrinkScale;
            set => _preShrinkScale = value;
        }

        public float PreShrinkDuration
        {
            get => _preShrinkDuration;
            set => _preShrinkDuration = Mathf.Max(0f, value);
        }

        public float ShrinkDuration
        {
            get => _shrinkDuration;
            set => _shrinkDuration = Mathf.Max(0f, value);
        }

        public float StaggerDelay
        {
            get => _staggerDelay;
            set => _staggerDelay = Mathf.Max(0f, value);
        }

        public Ease PreShrinkEase
        {
            get => _preShrinkEase;
            set => _preShrinkEase = value;
        }

        public Ease ShrinkEase
        {
            get => _shrinkEase;
            set => _shrinkEase = value;
        }

        public int PlayExitCallCount { get; private set; }
        public int ResetCallCount { get; private set; }
        public bool IsExitRunning => _exitTcs != null && !_exitTcs.Task.IsCompleted;
        public bool IsExitCompleted => _isExitCompleted;

        private void Awake()
        {
            CaptureRootsIfNeeded();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ValidateFeedbackViews(isEditorValidation: true);
        }
#endif

        public bool ValidateFeedbackViews(bool isEditorValidation = false)
        {
            if (_feedbackViews == null || _feedbackViews.Length == 0)
            {
                if (isEditorValidation)
                {
                    return true;
                }
                LogValidation("[LobbyWorldButtonsExitView] _feedbackViews must contain exactly 4 references.", isEditorValidation: false);
                return false;
            }

            if (_feedbackViews.Length != 4)
            {
                LogValidation("[LobbyWorldButtonsExitView] _feedbackViews must contain exactly 4 references.", isEditorValidation);
                return false;
            }

            var uniqueViews = new HashSet<LobbyWorldButtonFeedbackView>();
            for (int i = 0; i < _feedbackViews.Length; i++)
            {
                var fb = _feedbackViews[i];
                if (fb == null)
                {
                    LogValidation($"[LobbyWorldButtonsExitView] Feedback view at index {i} is null.", isEditorValidation);
                    return false;
                }

                if (!uniqueViews.Add(fb))
                {
                    LogValidation($"[LobbyWorldButtonsExitView] Duplicate feedback view reference '{fb.name}' at index {i}.", isEditorValidation);
                    return false;
                }

                if (fb.AnimatedRoot == null)
                {
                    LogValidation($"[LobbyWorldButtonsExitView] Feedback view '{fb.name}' is missing AnimatedRoot.", isEditorValidation);
                    return false;
                }
            }

            return true;
        }

        private void LogValidation(string message, bool isEditorValidation)
        {
            if (isEditorValidation)
            {
                Debug.LogWarning(message, this);
            }
            else
            {
                Debug.LogError(message, this);
            }
        }

        public void CaptureRootsIfNeeded()
        {
            if (_rootsCaptured)
            {
                return;
            }

            if (_feedbackViews == null || _feedbackViews.Length == 0)
            {
                return;
            }

            _cachedRoots = new RootState[_feedbackViews.Length];
            for (int i = 0; i < _feedbackViews.Length; i++)
            {
                var fb = _feedbackViews[i];
                if (fb != null && fb.AnimatedRoot != null)
                {
                    Transform rootTransform = fb.AnimatedRoot;
                    _cachedRoots[i] = new RootState
                    {
                        Transform = rootTransform,
                        GameObject = rootTransform.gameObject,
                        BaseLocalScale = rootTransform.localScale,
                        BaseActiveSelf = rootTransform.gameObject.activeSelf
                    };
                }
            }

            _rootsCaptured = true;
        }

        /// <summary>
        /// Plays the shrink-to-zero exit animation for all four buttons with bounded stagger,
        /// then deactivates the button root GameObjects upon completion.
        /// </summary>
        public Task PlayExitAsync(CancellationToken cancellationToken = default)
        {
            PlayExitCallCount++;

            if (cancellationToken.IsCancellationRequested)
            {
                ResetImmediate();
                return Task.FromCanceled(cancellationToken);
            }

            ResetExitSequenceOnly();
            CaptureRootsIfNeeded();
            StopFeedbackAndRestoreRoots();

            if (_cachedRoots == null || _cachedRoots.Length == 0)
            {
                return Task.CompletedTask;
            }

            _exitTcs = new TaskCompletionSource<bool>();
            if (cancellationToken.CanBeCanceled)
            {
                _cancellationRegistration = cancellationToken.Register(() =>
                {
                    ResetImmediate();
                });
            }

            _exitSequence = DOTween.Sequence();
            _exitSequence.SetTarget(gameObject);
            _exitSequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);

            for (int i = 0; i < _cachedRoots.Length; i++)
            {
                Transform root = _cachedRoots[i].Transform;
                if (root == null)
                {
                    continue;
                }

                float startTime = i * _staggerDelay;
                Vector3 baseScale = _cachedRoots[i].BaseLocalScale;
                Vector3 preShrinkTarget = Vector3.Scale(baseScale, _preShrinkScale);

                if (_preShrinkDuration > 0f)
                {
                    _exitSequence.Insert(startTime, root.DOScale(preShrinkTarget, _preShrinkDuration).SetEase(_preShrinkEase));
                    _exitSequence.Insert(startTime + _preShrinkDuration, root.DOScale(Vector3.zero, _shrinkDuration).SetEase(_shrinkEase));
                }
                else
                {
                    _exitSequence.Insert(startTime, root.DOScale(Vector3.zero, _shrinkDuration).SetEase(_shrinkEase));
                }
            }

            _exitSequence.OnComplete(() =>
            {
                _cancellationRegistration.Dispose();
                _cancellationRegistration = default;
                DeactivateRoots();
                _isExitCompleted = true;
                _exitTcs?.TrySetResult(true);
            });

            _exitSequence.Play();
            return _exitTcs.Task;
        }

        /// <summary>
        /// Kills any running exit animation and cancels pending exit task,
        /// then restores all captured root scales and active states.
        /// </summary>
        public void ResetImmediate()
        {
            ResetCallCount++;
            _isExitCompleted = false;

            _cancellationRegistration.Dispose();
            _cancellationRegistration = default;

            if (_exitSequence != null && _exitSequence.IsActive())
            {
                _exitSequence.Kill();
            }
            _exitSequence = null;

            if (_exitTcs != null && !_exitTcs.Task.IsCompleted)
            {
                _exitTcs.TrySetCanceled();
            }
            _exitTcs = null;

            RestoreRoots();
            ResumeFeedbackIfNeeded();
        }

        private void ResumeFeedbackIfNeeded()
        {
            if (_feedbackViews == null)
            {
                return;
            }

            for (int i = 0; i < _feedbackViews.Length; i++)
            {
                var fb = _feedbackViews[i];
                if (fb != null && fb.isActiveAndEnabled)
                {
                    fb.StartIdle();
                }
            }
        }

        private void ResetExitSequenceOnly()
        {
            _isExitCompleted = false;

            _cancellationRegistration.Dispose();
            _cancellationRegistration = default;

            if (_exitSequence != null && _exitSequence.IsActive())
            {
                _exitSequence.Kill();
            }
            _exitSequence = null;

            if (_exitTcs != null && !_exitTcs.Task.IsCompleted)
            {
                _exitTcs.TrySetCanceled();
            }
            _exitTcs = null;
        }

        private void StopFeedbackAndRestoreRoots()
        {
            if (_feedbackViews != null)
            {
                for (int i = 0; i < _feedbackViews.Length; i++)
                {
                    var fb = _feedbackViews[i];
                    if (fb != null)
                    {
                        fb.StopFeedbackAndRestoreBaseState();
                    }
                }
            }

            RestoreRoots();
        }

        private void DeactivateRoots()
        {
            if (_cachedRoots == null)
            {
                return;
            }

            for (int i = 0; i < _cachedRoots.Length; i++)
            {
                var root = _cachedRoots[i];
                if (root.GameObject != null)
                {
                    root.GameObject.SetActive(false);
                }
            }
        }

        private void RestoreRoots()
        {
            if (_cachedRoots == null)
            {
                return;
            }

            for (int i = 0; i < _cachedRoots.Length; i++)
            {
                var root = _cachedRoots[i];
                if (root.Transform != null)
                {
                    root.Transform.localScale = root.BaseLocalScale;
                }
                if (root.GameObject != null)
                {
                    root.GameObject.SetActive(root.BaseActiveSelf);
                }
            }
        }

        private void OnDisable()
        {
            if (!_isExitCompleted)
            {
                ResetImmediate();
            }
        }

        private void OnDestroy()
        {
            ResetImmediate();
        }
    }
}
