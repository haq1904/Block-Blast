using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace BlockBlast.Features.Lobby
{
    /// <summary>
    /// Passive View orchestrating Style 2: "Gentle Breeze & Float" for the title text.
    /// Features:
    /// - Phase 1: Entrance dropping from Top Border to resting position on the wooden board, fade-in, and 2-step bounce settle.
    /// - Phase 2: Per-character mesh wave floating (Y) and local tilt (Z) using TMP vertex manipulation.
    /// - Phase 3: Domino breeze gust from left to right, character shimmer highlight, and environmental cloud response.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LobbyTitleBreezeMotionView : MonoBehaviour
    {
        [Header("Targets")]
        [Tooltip("The TextMeshPro component displaying the title.")]
        [SerializeField] private TMP_Text _titleText;

        [Tooltip("The RectTransform component defining the boundary (Width: 20, Height: 5).")]
        [SerializeField] private RectTransform _rectTransform;

        [Tooltip("Environmental cloud transforms that subtly sway in sync with breeze gusts.")]
        [SerializeField] private Transform[] _environmentalClouds;

        [Header("Phase 1: Entrance (Top Border -> Board)")]
        [Tooltip("Whether to trigger the entrance drop animation upon OnEnable.")]
        [SerializeField] private bool _playEntranceOnEnable = true;

        [Tooltip("Delay in seconds before the entrance drop begins (e.g. waiting for scene loading/camera to settle).")]
        [SerializeField, Min(0f)] private float _entranceDelay = 0.50f;

        [Tooltip("Vertical drop distance from Top Border down to canonical resting position on the board.")]
        [SerializeField, Min(0f)] private float _topDropDistance = 1.80f;

        [Tooltip("Duration of the initial fade-in from 0% to 100% alpha.")]
        [SerializeField, Min(0.01f)] private float _fadeInDuration = 0.25f;

        [Tooltip("Duration of the downward gravity fall to the board.")]
        [SerializeField, Min(0.01f)] private float _dropDuration = 0.50f;

        [Tooltip("Rebound height for the primary bounce upon hitting the board.")]
        [SerializeField, Min(0.01f)] private float _bounce1Height = 0.35f;

        [Tooltip("Duration of each leg (up/down) of the primary bounce.")]
        [SerializeField, Min(0.01f)] private float _bounce1Duration = 0.10f;

        [Tooltip("Rebound height for the secondary subtle cushion settle.")]
        [SerializeField, Min(0.01f)] private float _bounce2Height = 0.10f;

        [Tooltip("Duration of each leg (up/down) of the secondary bounce.")]
        [SerializeField, Min(0.01f)] private float _bounce2Duration = 0.08f;

        [Header("Phase 2: Per-Character Idle Wave")]
        [Tooltip("Duration in seconds to smoothly blend into the idle wave and tilt after entrance settles.")]
        [SerializeField, Min(0.01f)] private float _idleBlendInDuration = 0.18f;

        [Tooltip("Vertical half-amplitude in mesh units for each character's sinusoidal wave.")]
        [SerializeField, Min(0.001f)] private float _charWaveAmplitudeY = 0.03f;

        [Tooltip("Maximum roll angle around local Z for each character's tilt.")]
        [SerializeField, Min(0f)] private float _charSwayAngleZ = 1.8f;

        [Tooltip("Phase stagger delay in seconds per character (creates piano key wave effect).")]
        [SerializeField, Min(0.01f)] private float _charPhaseStagger = 0.10f;

        [Tooltip("Total period in seconds for one complete wave breathing cycle.")]
        [SerializeField, Min(0.1f)] private float _wavePeriod = 2.80f;

        [Header("Phase 3: Breeze Gust & Environment")]
        [Tooltip("Interval in seconds between periodic breeze gusts.")]
        [SerializeField, Min(1f)] private float _gustInterval = 9.0f;

        [Tooltip("Delay between successive characters when the breeze domino wave sweeps left-to-right.")]
        [SerializeField, Min(0.01f)] private float _gustDominoDelay = 0.05f;

        [Tooltip("Duration in seconds that each character feels the gust push and elastic recovery.")]
        [SerializeField, Min(0.01f)] private float _gustCharDuration = 0.60f;

        [Tooltip("Extra roll angle around local Z imparted on each character by the gust.")]
        [SerializeField] private float _gustExtraTiltZ = 3.0f;

        [Tooltip("Temporary drift along local X imparted on each character by the gust.")]
        [SerializeField] private float _gustDriftX = 0.04f;

        [Tooltip("Sunlight shimmer tint applied to the characters catching the breeze.")]
        [SerializeField] private Color _shimmerColor = new Color(1.30f, 1.25f, 0.90f, 1.0f);

        [Tooltip("Subtle horizontal drift in local units applied to environmental clouds during gusts.")]
        [SerializeField] private float _cloudDriftX = 0.05f;

        // Canonical baseline states
        private Vector2 _canonicalAnchoredPos;
        private float _canonicalAlpha = 1.0f;
        private Color _canonicalColor = Color.white;
        private Vector3[] _canonicalCloudPositions;
        private Vector3[][] _cachedMeshVertices;
        private Color32[][] _cachedMeshColors;

        // Active animation states
        private bool _isInitialized;
        private bool _isWaveActive;
        private float _wavePhase;
        private float _idleBlend;
        private float _gustTimer = -1f;
        private float _totalGustTime;

        // Active tween handles
        private Sequence _entranceSequence;
        private Tween _waveTween;
        private Tween _idleBlendTween;
        private Sequence _gustSequence;
        private Tween _gustProgressTween;

        public float IdleBlendInDuration
        {
            get => _idleBlendInDuration;
            set => _idleBlendInDuration = Mathf.Max(0.01f, value);
        }

        public float IdleBlend => _idleBlend;

        private void Awake()
        {
            InitializeReferences();
        }

        private void OnEnable()
        {
            if (!_isInitialized)
            {
                InitializeReferences();
            }

            StartMotionLifecycle();
        }

        private void OnDisable()
        {
            KillAllMotions();
            ResetToCanonicalState();
        }

        private void LateUpdate()
        {
            if (_isWaveActive && _titleText != null)
            {
                AnimateCharacterVertices();
            }
        }

        private void InitializeReferences()
        {
            if (_rectTransform == null)
            {
                _rectTransform = GetComponent<RectTransform>();
            }

            if (_titleText == null)
            {
                _titleText = GetComponent<TMP_Text>();
            }

            if (_rectTransform != null)
            {
                // The position in the scene is the canonical resting position on the wooden board
                _canonicalAnchoredPos = _rectTransform.anchoredPosition;
            }

            if (_titleText != null)
            {
                // Ensure canonical alpha is always fully opaque (fallback to 1 if scene was saved transparent)
                _canonicalAlpha = _titleText.alpha > 0.05f ? _titleText.alpha : 1.0f;
                _canonicalColor = _titleText.color;
                if (_canonicalColor.a < 0.05f)
                {
                    _canonicalColor.a = 1.0f;
                }
                _titleText.alpha = _canonicalAlpha;
                _titleText.color = _canonicalColor;

                CacheMeshBaseData();
            }

            AutoDetectCloudsIfEmpty();
            CacheCloudBaselines();

            _isInitialized = true;
        }

        private void AutoDetectCloudsIfEmpty()
        {
            if (_environmentalClouds != null && _environmentalClouds.Length > 0) return;

            if (transform.parent != null)
            {
                var detected = new List<Transform>();
                for (int i = 0; i < transform.parent.childCount; i++)
                {
                    Transform sibling = transform.parent.GetChild(i);
                    if (sibling != transform && sibling.name.ToLowerInvariant().Contains("cloud"))
                    {
                        detected.Add(sibling);
                    }
                }

                if (detected.Count > 0)
                {
                    _environmentalClouds = detected.ToArray();
                }
            }
        }

        private void CacheCloudBaselines()
        {
            if (_environmentalClouds != null && _environmentalClouds.Length > 0)
            {
                _canonicalCloudPositions = new Vector3[_environmentalClouds.Length];
                for (int i = 0; i < _environmentalClouds.Length; i++)
                {
                    if (_environmentalClouds[i] != null)
                    {
                        _canonicalCloudPositions[i] = _environmentalClouds[i].localPosition;
                    }
                }
            }
        }

        private void CacheMeshBaseData()
        {
            if (_titleText == null) return;

            _titleText.ForceMeshUpdate();
            TMP_TextInfo textInfo = _titleText.textInfo;
            if (textInfo == null || textInfo.meshInfo == null || textInfo.meshInfo.Length == 0) return;

            _cachedMeshVertices = new Vector3[textInfo.meshInfo.Length][];
            _cachedMeshColors = new Color32[textInfo.meshInfo.Length][];

            for (int m = 0; m < textInfo.meshInfo.Length; m++)
            {
                int vertCount = textInfo.meshInfo[m].vertices.Length;
                _cachedMeshVertices[m] = new Vector3[vertCount];
                _cachedMeshColors[m] = new Color32[vertCount];

                Array.Copy(textInfo.meshInfo[m].vertices, _cachedMeshVertices[m], vertCount);
                Array.Copy(textInfo.meshInfo[m].colors32, _cachedMeshColors[m], vertCount);
            }
        }

        private void StartMotionLifecycle()
        {
            KillAllMotions();

            if (_playEntranceOnEnable && _rectTransform != null && _titleText != null)
            {
                PlayEntrance(() =>
                {
                    if (isActiveAndEnabled)
                    {
                        StartWaveAndGusts();
                    }
                });
            }
            else
            {
                if (_rectTransform != null)
                {
                    _rectTransform.anchoredPosition = _canonicalAnchoredPos;
                }
                if (_titleText != null)
                {
                    _titleText.alpha = _canonicalAlpha;
                }
                StartWaveAndGusts();
            }
        }

        private void PlayEntrance(Action onComplete)
        {
            _isWaveActive = false;

            if (_rectTransform == null || _titleText == null)
            {
                onComplete?.Invoke();
                return;
            }

            _rectTransform.DOKill();
            _titleText.DOKill();

            // Start placed at Top Border (above canonical resting position) with 0% alpha
            Vector2 startPos = new Vector2(_canonicalAnchoredPos.x, _canonicalAnchoredPos.y + _topDropDistance);
            _rectTransform.anchoredPosition = startPos;
            _titleText.alpha = 0f;

            _entranceSequence = DOTween.Sequence();
            _entranceSequence.SetTarget(gameObject);
            _entranceSequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (_entranceDelay > 0f)
            {
                _entranceSequence.AppendInterval(_entranceDelay);
            }

            // Slide Down to Board & Fade In using direct TMP_Text.alpha tween
            _entranceSequence.Append(DOTween.To(() => _titleText.alpha, a => _titleText.alpha = a, _canonicalAlpha, _fadeInDuration).SetEase(Ease.OutQuad));
            _entranceSequence.Join(_rectTransform.DOAnchorPosY(_canonicalAnchoredPos.y, _dropDuration).SetEase(Ease.InQuad));

            // Bounce 1: Primary bounce upon hitting the board
            float b1PeakY = _canonicalAnchoredPos.y + _bounce1Height;
            _entranceSequence.Append(_rectTransform.DOAnchorPosY(b1PeakY, _bounce1Duration).SetEase(Ease.OutQuad));
            _entranceSequence.Append(_rectTransform.DOAnchorPosY(_canonicalAnchoredPos.y, _bounce1Duration).SetEase(Ease.InQuad));

            // Bounce 2: Cushion settle
            float b2PeakY = _canonicalAnchoredPos.y + _bounce2Height;
            _entranceSequence.Append(_rectTransform.DOAnchorPosY(b2PeakY, _bounce2Duration).SetEase(Ease.OutQuad));
            _entranceSequence.Append(_rectTransform.DOAnchorPosY(_canonicalAnchoredPos.y, _bounce2Duration).SetEase(Ease.InOutSine));

            _entranceSequence.OnComplete(() =>
            {
                _rectTransform.anchoredPosition = _canonicalAnchoredPos;
                _titleText.alpha = _canonicalAlpha;
                onComplete?.Invoke();
            });
        }

        private void StartWaveAndGusts()
        {
            _isWaveActive = true;
            _wavePhase = 0f;
            _idleBlend = 0f;

            if (_idleBlendTween != null && _idleBlendTween.IsActive())
            {
                _idleBlendTween.Kill();
            }

            if (_idleBlendInDuration > 0f)
            {
                _idleBlendTween = DOTween.To(() => _idleBlend, x => _idleBlend = x, 1f, _idleBlendInDuration)
                    .SetEase(Ease.OutSine)
                    .SetTarget(gameObject)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }
            else
            {
                _idleBlend = 1f;
            }

            // Drive sinusoidal wave phase smoothly via DOTween
            _waveTween = DOTween.To(() => _wavePhase, x => _wavePhase = x, Mathf.PI * 2f, _wavePeriod)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental)
                .SetTarget(gameObject)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            // Schedule recurring breeze gusts
            _gustSequence = DOTween.Sequence();
            _gustSequence.SetTarget(gameObject);
            _gustSequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);
            _gustSequence.AppendInterval(_gustInterval);
            _gustSequence.AppendCallback(TriggerBreezeGust);
            _gustSequence.SetLoops(-1);
        }

        private void TriggerBreezeGust()
        {
            if (!isActiveAndEnabled || _titleText == null) return;

            int charCount = _titleText.textInfo != null ? _titleText.textInfo.characterCount : 14;
            _totalGustTime = (charCount * _gustDominoDelay) + _gustCharDuration;

            if (_gustProgressTween != null && _gustProgressTween.IsActive())
            {
                _gustProgressTween.Kill();
            }

            _gustTimer = 0f;
            _gustProgressTween = DOTween.To(() => _gustTimer, x => _gustTimer = x, _totalGustTime, _totalGustTime)
                .SetEase(Ease.Linear)
                .SetTarget(gameObject)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() => _gustTimer = -1f);

            // Environmental Cloud response
            TriggerCloudSway();
        }

        private void TriggerCloudSway()
        {
            if (_environmentalClouds == null || _canonicalCloudPositions == null) return;

            for (int i = 0; i < _environmentalClouds.Length; i++)
            {
                Transform cloud = _environmentalClouds[i];
                if (cloud == null) continue;

                Vector3 basePos = _canonicalCloudPositions[i];
                cloud.DOKill();

                Sequence cloudSeq = DOTween.Sequence();
                cloudSeq.SetTarget(cloud);
                cloudSeq.SetLink(cloud.gameObject, LinkBehaviour.KillOnDisable);
                cloudSeq.Append(cloud.DOLocalMoveX(basePos.x + _cloudDriftX, 0.50f).SetEase(Ease.OutQuad));
                cloudSeq.Append(cloud.DOLocalMoveX(basePos.x, 0.80f).SetEase(Ease.InOutSine).SetDelay(0.10f));
            }
        }

        private void AnimateCharacterVertices()
        {
            if (_titleText == null)
            {
                _titleText = GetComponent<TMP_Text>();
                if (_titleText == null) return;
            }

            if (_cachedMeshVertices == null || _cachedMeshVertices.Length == 0)
            {
                CacheMeshBaseData();
                if (_cachedMeshVertices == null) return;
            }

            TMP_TextInfo textInfo = _titleText.textInfo;
            if (textInfo == null || textInfo.characterCount == 0) return;

            int characterCount = textInfo.characterCount;

            for (int i = 0; i < characterCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int matIndex = charInfo.materialReferenceIndex;
                int vertIndex = charInfo.vertexIndex;

                if (matIndex >= _cachedMeshVertices.Length || vertIndex + 3 >= _cachedMeshVertices[matIndex].Length)
                {
                    continue;
                }

                Vector3[] srcVerts = _cachedMeshVertices[matIndex];
                Vector3[] dstVerts = textInfo.meshInfo[matIndex].vertices;
                Color32[] dstColors = textInfo.meshInfo[matIndex].colors32;
                Color32 baseColor = _cachedMeshColors[matIndex][vertIndex];

                // Center of the character in local text mesh space
                Vector3 center = (srcVerts[vertIndex + 0] + srcVerts[vertIndex + 2]) * 0.5f;

                // Phase 2: Per-character sinusoidal wave (Y-float & Z-sway)
                float phase = _wavePhase + i * _charPhaseStagger;
                float waveY = Mathf.Sin(phase) * _charWaveAmplitudeY;
                float swayZ = Mathf.Sin(phase) * _charSwayAngleZ;

                // Phase 3: Domino Breeze Gust & Shimmer
                float gustDriftX = 0f;
                float gustExtraTiltZ = 0f;
                float shimmerBlend = 0f;

                if (_gustTimer >= 0f)
                {
                    float charGustStart = i * _gustDominoDelay;
                    float elapsed = _gustTimer - charGustStart;

                    if (elapsed >= 0f && elapsed <= _gustCharDuration)
                    {
                        float u = elapsed / _gustCharDuration;
                        // Sinusoidal bell pulse for wind push and elastic return
                        float pulse = Mathf.Sin(u * Mathf.PI);

                        gustDriftX = pulse * _gustDriftX;
                        gustExtraTiltZ = pulse * _gustExtraTiltZ;
                        shimmerBlend = pulse * 0.85f;
                    }
                }

                // Transform synthesis blended from canonical mesh pose
                float blend = _idleBlend;
                float totalTiltZ = (swayZ + gustExtraTiltZ) * blend;
                Vector3 totalOffset = new Vector3(gustDriftX, waveY, 0f) * blend;
                shimmerBlend *= blend;

                Quaternion rot = Quaternion.Euler(0f, 0f, totalTiltZ);

                Color32 targetColor = shimmerBlend > 0f
                    ? Color32.Lerp(baseColor, (Color32)_shimmerColor, shimmerBlend)
                    : baseColor;

                for (int v = 0; v < 4; v++)
                {
                    Vector3 orig = srcVerts[vertIndex + v];
                    dstVerts[vertIndex + v] = center + totalOffset + rot * (orig - center);
                    dstColors[vertIndex + v] = targetColor;
                }
            }

            // Push deformed vertices and shimmer colors to the TMP mesh renderers
            for (int m = 0; m < textInfo.meshInfo.Length; m++)
            {
                var meshInfo = textInfo.meshInfo[m];
                meshInfo.mesh.vertices = meshInfo.vertices;
                meshInfo.mesh.colors32 = meshInfo.colors32;
                _titleText.UpdateGeometry(meshInfo.mesh, m);
            }
        }

        private void KillAllMotions()
        {
            _isWaveActive = false;

            if (_entranceSequence != null && _entranceSequence.IsActive())
            {
                _entranceSequence.Kill();
            }
            _entranceSequence = null;

            if (_waveTween != null && _waveTween.IsActive())
            {
                _waveTween.Kill();
            }
            _waveTween = null;

            if (_idleBlendTween != null && _idleBlendTween.IsActive())
            {
                _idleBlendTween.Kill();
            }
            _idleBlendTween = null;

            if (_gustSequence != null && _gustSequence.IsActive())
            {
                _gustSequence.Kill();
            }
            _gustSequence = null;

            if (_gustProgressTween != null && _gustProgressTween.IsActive())
            {
                _gustProgressTween.Kill();
            }
            _gustProgressTween = null;

            if (_rectTransform != null)
            {
                _rectTransform.DOKill();
            }

            if (_titleText != null)
            {
                _titleText.DOKill();
            }

            if (_environmentalClouds != null)
            {
                for (int i = 0; i < _environmentalClouds.Length; i++)
                {
                    if (_environmentalClouds[i] != null)
                    {
                        _environmentalClouds[i].DOKill();
                    }
                }
            }
        }

        private void ResetToCanonicalState()
        {
            _idleBlend = 0f;
            _wavePhase = 0f;

            if (_rectTransform != null && _canonicalAnchoredPos != Vector2.zero)
            {
                _rectTransform.anchoredPosition = _canonicalAnchoredPos;
            }

            if (_titleText != null)
            {
                _titleText.alpha = _canonicalAlpha > 0.05f ? _canonicalAlpha : 1.0f;
                _titleText.color = _canonicalColor;
                _titleText.ForceMeshUpdate();
            }

            if (_environmentalClouds != null && _canonicalCloudPositions != null)
            {
                for (int i = 0; i < _environmentalClouds.Length; i++)
                {
                    if (_environmentalClouds[i] != null && i < _canonicalCloudPositions.Length)
                    {
                        _environmentalClouds[i].localPosition = _canonicalCloudPositions[i];
                    }
                }
            }
        }
    }
}
