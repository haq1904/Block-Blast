using DG.Tweening;
using UnityEngine;

namespace BlockBlast.Features.GameFlow.Play
{
    /// <summary>
    /// Plays the one-shot rise entrance for the three trail rocks when the gameplay environment activates.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlaySceneRockRiseEntranceView : MonoBehaviour
    {
        private const int RockCount = 3;
        private const float CenterTolerance = 0.001f;

        [Header("Rocks")]
        [Tooltip("Trail rocks ordered by entrance timing: center, left, right.")]
        [SerializeField] private Transform[] _rocks = new Transform[RockCount];

        [Header("Initial Spawn Entrance")]
        [Tooltip("Delay before the center rock starts rising after PlayScene becomes active.")]
        [SerializeField, Min(0f)] private float _initialDelay = 0.45f;

        [Tooltip("Additional delay between the center, left, and right rock entrances.")]
        [SerializeField, Min(0f)] private float _stagger = 0.08f;

        [Tooltip("Local-space distance below each rock's canonical pose from which it rises.")]
        [SerializeField, Min(0f)] private float _riseDistance = 1.25f;

        [Tooltip("Duration of the upward motion for each rock.")]
        [SerializeField, Min(0.01f)] private float _riseDuration = 0.38f;

        [Tooltip("Duration of the final scale settle after each rock reaches its canonical pose.")]
        [SerializeField, Min(0.01f)] private float _settleDuration = 0.10f;

        [Tooltip("Scale multiplier used while each rock starts below its canonical pose.")]
        [SerializeField] private Vector3 _startScaleMultiplier = new Vector3(0.82f, 1.14f, 0.82f);

        [Tooltip("Scale multiplier at the top of the rise before the final settle.")]
        [SerializeField] private Vector3 _overshootScaleMultiplier = new Vector3(1.06f, 0.94f, 1.06f);

        [Tooltip("Maximum local roll angle applied to the left and right rocks during their entrance.")]
        [SerializeField, Min(0f)] private float _tiltAngle = 4f;

        private Sequence _entranceSequence;
        private Vector3[] _canonicalLocalPositions;
        private Quaternion[] _canonicalLocalRotations;
        private Vector3[] _canonicalLocalScales;
        private bool _isConfigured;
        private bool _hasPlayed;

        private void Awake()
        {
            _isConfigured = CacheCanonicalPoses();
            if (_isConfigured)
            {
                ApplyBuriedPoses();
            }
        }

        private void OnEnable()
        {
            if (!_isConfigured || _hasPlayed)
            {
                return;
            }

            _hasPlayed = true;
            PlayEntrance();
        }

        private void OnDisable()
        {
            KillEntranceAndRestore();
        }

        private void OnDestroy()
        {
            KillEntranceAndRestore();
        }

        private void PlayEntrance()
        {
            KillEntranceAndRestore();
            ApplyBuriedPoses();

            _entranceSequence = DOTween.Sequence();
            _entranceSequence.SetTarget(gameObject);
            _entranceSequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);

            for (int index = 0; index < RockCount; index++)
            {
                InsertRiseAndSettle(index, _initialDelay + index * _stagger);
            }

            _entranceSequence.OnComplete(() =>
            {
                RestoreCanonicalPoses();
                _entranceSequence = null;
            });
        }

        private bool CacheCanonicalPoses()
        {
            if (_rocks == null || _rocks.Length != RockCount)
            {
                Debug.LogError($"[{nameof(PlaySceneRockRiseEntranceView)}] Exactly {RockCount} rock transforms are required.", this);
                return false;
            }

            _canonicalLocalPositions = new Vector3[RockCount];
            _canonicalLocalRotations = new Quaternion[RockCount];
            _canonicalLocalScales = new Vector3[RockCount];

            for (int index = 0; index < RockCount; index++)
            {
                Transform rock = _rocks[index];
                if (rock == null)
                {
                    Debug.LogError($"[{nameof(PlaySceneRockRiseEntranceView)}] Rock target at index {index} is missing.", this);
                    return false;
                }

                for (int previousIndex = 0; previousIndex < index; previousIndex++)
                {
                    if (rock == _rocks[previousIndex])
                    {
                        Debug.LogError($"[{nameof(PlaySceneRockRiseEntranceView)}] Rock targets must not be duplicated.", this);
                        return false;
                    }
                }

                _canonicalLocalPositions[index] = rock.localPosition;
                _canonicalLocalRotations[index] = rock.localRotation;
                _canonicalLocalScales[index] = rock.localScale;
            }

            return true;
        }

        private void ApplyBuriedPoses()
        {
            for (int index = 0; index < RockCount; index++)
            {
                Transform rock = _rocks[index];
                rock.localPosition = _canonicalLocalPositions[index] + Vector3.down * _riseDistance;
                rock.localScale = Vector3.Scale(_canonicalLocalScales[index], _startScaleMultiplier);
                rock.localRotation = _canonicalLocalRotations[index] * Quaternion.Euler(0f, 0f, GetEntranceTilt(index));
            }
        }

        private void InsertRiseAndSettle(int index, float startTime)
        {
            Transform rock = _rocks[index];
            float settleStartTime = startTime + _riseDuration;
            Vector3 overshootScale = Vector3.Scale(_canonicalLocalScales[index], _overshootScaleMultiplier);

            _entranceSequence.Insert(
                startTime,
                rock.DOLocalMoveY(_canonicalLocalPositions[index].y, _riseDuration).SetEase(Ease.OutCubic));
            _entranceSequence.Insert(
                startTime,
                rock.DOScale(overshootScale, _riseDuration).SetEase(Ease.OutBack));
            _entranceSequence.Insert(
                startTime,
                rock.DOLocalRotateQuaternion(_canonicalLocalRotations[index], _riseDuration).SetEase(Ease.OutQuad));
            _entranceSequence.Insert(
                settleStartTime,
                rock.DOScale(_canonicalLocalScales[index], _settleDuration).SetEase(Ease.OutQuad));
        }

        private float GetEntranceTilt(int index)
        {
            float localX = _canonicalLocalPositions[index].x;
            if (Mathf.Abs(localX) <= CenterTolerance)
            {
                return 0f;
            }

            return localX < 0f ? -_tiltAngle : _tiltAngle;
        }

        private void KillEntranceAndRestore()
        {
            if (_entranceSequence != null && _entranceSequence.IsActive())
            {
                _entranceSequence.Kill();
            }

            _entranceSequence = null;
            RestoreCanonicalPoses();
        }

        private void RestoreCanonicalPoses()
        {
            if (!_isConfigured)
            {
                return;
            }

            for (int index = 0; index < RockCount; index++)
            {
                Transform rock = _rocks[index];
                if (rock == null)
                {
                    continue;
                }

                rock.localPosition = _canonicalLocalPositions[index];
                rock.localRotation = _canonicalLocalRotations[index];
                rock.localScale = _canonicalLocalScales[index];
            }
        }
    }
}
