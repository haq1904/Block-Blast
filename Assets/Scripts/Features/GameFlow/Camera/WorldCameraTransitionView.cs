using System;
using System.Threading;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace BlockBlast.Features.GameFlow.Camera
{
    /// <summary>
    /// Passive View animating the canonical World camera between Lobby and Play poses using DOTween.
    /// </summary>
    public sealed class WorldCameraTransitionView : MonoBehaviour
    {
        [Header("Camera & Poses")]
        [Tooltip("Canonical World camera transform.")]
        [SerializeField] private Transform _cameraTransform;

        [Tooltip("Transform marker for Lobby pose.")]
        [SerializeField] private Transform _lobbyPose;

        [Tooltip("Transform marker for Play pose.")]
        [SerializeField] private Transform _playPose;

        [Header("Animation Config")]
        [Tooltip("Transition duration in seconds.")]
        [SerializeField, Min(0.01f)] private float _duration = 1.1f;

        [Tooltip("Tween easing function.")]
        [SerializeField] private Ease _ease = Ease.InOutSine;

        [Tooltip("Overshoot strength used by InBack, OutBack, and InOutBack ease types.")]
        [SerializeField, Range(0f, 5f)] private float _backOvershoot = 2.5f;

        private Sequence _sequence;

        public bool IsMoving => _sequence != null && _sequence.IsActive() && _sequence.IsPlaying();

        public Transform CameraTransform
        {
            get => _cameraTransform;
            set => _cameraTransform = value;
        }

        public Transform LobbyPose
        {
            get => _lobbyPose;
            set => _lobbyPose = value;
        }

        public Transform PlayPose
        {
            get => _playPose;
            set => _playPose = value;
        }

        public float Duration
        {
            get => _duration;
            set => _duration = Mathf.Max(0.01f, value);
        }

        public Ease Ease
        {
            get => _ease;
            set => _ease = value;
        }

        public float BackOvershoot
        {
            get => _backOvershoot;
            set => _backOvershoot = Mathf.Clamp(value, 0f, 5f);
        }

        public Task MoveToPlayAsync(CancellationToken cancellationToken = default) =>
            MoveToPoseAsync(_playPose, cancellationToken);

        public Task MoveToLobbyAsync(CancellationToken cancellationToken = default) =>
            MoveToPoseAsync(_lobbyPose, cancellationToken);

        public void SnapToLobby()
        {
            KillActiveTween();

            if (_cameraTransform == null || _lobbyPose == null)
            {
                Debug.LogWarning("[WorldCameraTransitionView] Cannot snap to Lobby pose: CameraTransform or LobbyPose is null.", this);
                return;
            }

            _cameraTransform.position = _lobbyPose.position;
            _cameraTransform.rotation = _lobbyPose.rotation;
        }

        private Task MoveToPoseAsync(Transform targetPose, CancellationToken cancellationToken)
        {
            if (_cameraTransform == null)
            {
                throw new InvalidOperationException("[WorldCameraTransitionView] CameraTransform is not assigned.");
            }

            if (targetPose == null)
            {
                throw new InvalidOperationException("[WorldCameraTransitionView] Target pose transform is null.");
            }

            KillActiveTween();

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }

            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            bool completed = false;

            Sequence localSeq = DOTween.Sequence();
            _sequence = localSeq;

            localSeq.SetTarget(_cameraTransform);
            localSeq.SetLink(gameObject, LinkBehaviour.KillOnDisable);
            localSeq.SetUpdate(true);

            localSeq.Append(ApplyConfiguredEase(_cameraTransform.DOMove(targetPose.position, _duration)));
            localSeq.Join(ApplyConfiguredEase(_cameraTransform.DORotateQuaternion(targetPose.rotation, _duration)));

            CancellationTokenRegistration registration = default;

            void Cleanup()
            {
                registration.Dispose();
                if (ReferenceEquals(_sequence, localSeq))
                {
                    _sequence = null;
                }
            }

            localSeq.OnComplete(() =>
            {
                completed = true;
                Cleanup();
                tcs.TrySetResult(true);
            });

            localSeq.OnKill(() =>
            {
                if (!completed)
                {
                    Cleanup();
                    if (cancellationToken.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled(cancellationToken);
                    }
                    else
                    {
                        tcs.TrySetException(new OperationCanceledException("Camera transition sequence was killed unexpectedly before completion."));
                    }
                }
            });

            if (cancellationToken.CanBeCanceled)
            {
                registration = cancellationToken.Register(() =>
                {
                    if (!completed && localSeq != null && localSeq.IsActive())
                    {
                        localSeq.Kill();
                    }
                });
            }

            return tcs.Task;
        }

        private void KillActiveTween()
        {
            if (_sequence != null)
            {
                if (_sequence.IsActive())
                {
                    _sequence.Kill();
                }
                _sequence = null;
            }

            if (_cameraTransform != null)
            {
                _cameraTransform.DOKill();
            }
        }

        private T ApplyConfiguredEase<T>(T tween) where T : Tween
        {
            return IsBackEase(_ease)
                ? tween.SetEase(_ease, _backOvershoot)
                : tween.SetEase(_ease);
        }

        private static bool IsBackEase(Ease ease)
        {
            return ease == Ease.InBack || ease == Ease.OutBack || ease == Ease.InOutBack;
        }

        private void OnDisable()
        {
            KillActiveTween();
        }

        private void OnDestroy()
        {
            KillActiveTween();
        }
    }
}
