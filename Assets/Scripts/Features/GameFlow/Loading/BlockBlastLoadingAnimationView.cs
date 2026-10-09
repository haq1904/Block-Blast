using DG.Tweening;
using UnityEngine;

public sealed class BlockBlastLoadingAnimationView : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private RectTransform _cloudAspectRoot;

    [Header("Far Layer")]
    [SerializeField] private RectTransform _topFar;
    [SerializeField] private RectTransform _bottomFar;

    [Header("Mid Layer")]
    [SerializeField] private RectTransform _topMid;
    [SerializeField] private RectTransform _bottomMid;

    [Header("Foreground Layer")]
    [SerializeField] private RectTransform _topForeground;
    [SerializeField] private RectTransform _bottomForeground;

    [Header("Shape")]
    [SerializeField] private RectTransform _shapeVisual;
    [SerializeField] private CanvasGroup _shapeCanvasGroup;

    [Header("Open State")]
    [SerializeField, Min(1f)] private float _travelHeightMultiplier = 1.08f;
    [SerializeField, Min(0f)] private float _openMargin = 96f;

    [Header("Cover Timing")]
    [SerializeField, Min(0f)] private float _farCoverStart = 0f;
    [SerializeField, Min(0.01f)] private float _farCoverDuration = 1.12f;
    [SerializeField, Min(0f)] private float _midCoverStart = 0.06f;
    [SerializeField, Min(0.01f)] private float _midCoverDuration = 1.04f;
    [SerializeField, Min(0f)] private float _foregroundCoverStart = 0.14f;
    [SerializeField, Min(0.01f)] private float _foregroundCoverDuration = 0.94f;
    [SerializeField, Min(0f)] private float _bottomGroupDelay = 0.04f;
    [SerializeField] private Ease _cloudEase = Ease.InOutSine;

    [Header("Reveal Timing")]
    [SerializeField, Min(0f)] private float _foregroundRevealStart = 0f;
    [SerializeField, Min(0.01f)] private float _foregroundRevealDuration = 0.78f;
    [SerializeField, Min(0f)] private float _midRevealStart = 0.06f;
    [SerializeField, Min(0.01f)] private float _midRevealDuration = 0.90f;
    [SerializeField, Min(0f)] private float _farRevealStart = 0.12f;
    [SerializeField, Min(0.01f)] private float _farRevealDuration = 1.02f;

    [Header("Shape Timing")]
    [SerializeField, Min(0f)] private float _shapeShowStart = 0.72f;
    [SerializeField, Min(0.01f)] private float _shapeShowDuration = 0.28f;
    [SerializeField, Min(0.01f)] private float _shapeHideDuration = 0.24f;
    [SerializeField, Min(0.01f)] private float _shapeSpinDuration = 1.20f;
    [SerializeField, Range(0.1f, 1f)] private float _shapeHiddenScale = 0.84f;

    [Header("Preview")]
    [Tooltip("When enabled during Play Mode, checking this component starts the preview loop.")]
    [SerializeField] private bool _previewOnEnable = true;
    [SerializeField] private bool _loopPreview = true;
    [SerializeField, Min(0f)] private float _previewHoldDuration = 1.20f;
    [SerializeField, Min(0f)] private float _previewLoopPause = 0.65f;

    private Sequence _activeSequence;
    private Tween _shapeSpinTween;
    private Quaternion _shapeBaseRotation;
    private Vector3 _shapeBaseScale;
    private bool _hasCanonicalShapeState;

    private void Awake()
    {
        CacheCanonicalShapeState();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying || !_previewOnEnable)
        {
            return;
        }

        if (!HasRequiredReferences())
        {
            Debug.LogError("[BlockBlastLoadingAnimationView] Missing required UI references.", this);
            enabled = false;
            return;
        }

        PlayPreview();
    }

    private void OnDisable()
    {
        KillAnimations();

        if (Application.isPlaying && HasRequiredReferences())
        {
            ApplyOpenState();
        }
    }

    private void OnDestroy()
    {
        KillAnimations();
    }

    public Sequence PlayCover()
    {
        if (!PreparePlayback())
        {
            return null;
        }

        ApplyOpenState();
        _activeSequence = CreateSequence();
        InsertCover(_activeSequence, 0f);
        InsertShapeShow(_activeSequence, _shapeShowStart);
        return _activeSequence;
    }

    public Sequence PlayReveal()
    {
        if (!PreparePlayback())
        {
            return null;
        }

        StopShapeSpin();
        _activeSequence = CreateSequence();
        InsertShapeHide(_activeSequence, 0f);
        InsertReveal(_activeSequence, 0f);
        _activeSequence.OnComplete(ApplyOpenState);
        return _activeSequence;
    }

    public void PlayPreview()
    {
        if (!PreparePlayback())
        {
            return;
        }

        ApplyOpenState();

        float coverEnd = GetCoverDuration();
        float revealStart = coverEnd + _previewHoldDuration;
        float revealEnd = revealStart + GetRevealDuration();

        _activeSequence = CreateSequence();
        _activeSequence.InsertCallback(0f, ApplyOpenState);
        InsertCover(_activeSequence, 0f);
        InsertShapeShow(_activeSequence, _shapeShowStart);
        InsertShapeHide(_activeSequence, revealStart);
        InsertReveal(_activeSequence, revealStart);
        _activeSequence.InsertCallback(revealEnd, ApplyOpenState);
        _activeSequence.AppendInterval(_previewLoopPause);

        if (_loopPreview)
        {
            _activeSequence.SetLoops(-1, LoopType.Restart);
        }
    }

    public void ResetOpenImmediate()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        KillAnimations();
        ApplyOpenState();
    }

    public void ResetClosedImmediate()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        KillAnimations();
        ApplyClosedState();
        ShowShapeImmediate();
    }

    private bool PreparePlayback()
    {
        if (!HasRequiredReferences())
        {
            Debug.LogError("[BlockBlastLoadingAnimationView] Missing required UI references.", this);
            return false;
        }

        CacheCanonicalShapeState();
        Canvas.ForceUpdateCanvases();
        KillAnimations();
        return true;
    }

    private Sequence CreateSequence()
    {
        return DOTween.Sequence()
            .SetTarget(gameObject)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable)
            .SetUpdate(true);
    }

    private void InsertCover(Sequence sequence, float offset)
    {
        InsertCloudTween(sequence, offset + _farCoverStart, _topFar, Vector2.zero, _farCoverDuration);
        InsertCloudTween(sequence, offset + _farCoverStart + _bottomGroupDelay, _bottomFar, Vector2.zero, _farCoverDuration);
        InsertCloudTween(sequence, offset + _midCoverStart, _topMid, Vector2.zero, _midCoverDuration);
        InsertCloudTween(sequence, offset + _midCoverStart + _bottomGroupDelay, _bottomMid, Vector2.zero, _midCoverDuration);
        InsertCloudTween(sequence, offset + _foregroundCoverStart, _topForeground, Vector2.zero, _foregroundCoverDuration);
        InsertCloudTween(sequence, offset + _foregroundCoverStart + _bottomGroupDelay, _bottomForeground, Vector2.zero, _foregroundCoverDuration);
    }

    private void InsertReveal(Sequence sequence, float offset)
    {
        float travel = GetOpenTravel();
        Vector2 topOpen = Vector2.up * travel;
        Vector2 bottomOpen = Vector2.down * travel;

        InsertCloudTween(sequence, offset + _foregroundRevealStart, _topForeground, topOpen, _foregroundRevealDuration);
        InsertCloudTween(sequence, offset + _foregroundRevealStart + _bottomGroupDelay, _bottomForeground, bottomOpen, _foregroundRevealDuration);
        InsertCloudTween(sequence, offset + _midRevealStart, _topMid, topOpen, _midRevealDuration);
        InsertCloudTween(sequence, offset + _midRevealStart + _bottomGroupDelay, _bottomMid, bottomOpen, _midRevealDuration);
        InsertCloudTween(sequence, offset + _farRevealStart, _topFar, topOpen, _farRevealDuration);
        InsertCloudTween(sequence, offset + _farRevealStart + _bottomGroupDelay, _bottomFar, bottomOpen, _farRevealDuration);
    }

    private void InsertCloudTween(
        Sequence sequence,
        float start,
        RectTransform target,
        Vector2 destination,
        float duration)
    {
        sequence.Insert(
            start,
            target.DOAnchorPos(destination, duration).SetEase(_cloudEase));
    }

    private void InsertShapeShow(Sequence sequence, float start)
    {
        sequence.InsertCallback(start, StartShapeSpin);
        sequence.Insert(
            start,
            _shapeCanvasGroup.DOFade(1f, _shapeShowDuration).SetEase(Ease.OutSine));
        sequence.Insert(
            start,
            _shapeVisual.DOScale(_shapeBaseScale, _shapeShowDuration).SetEase(Ease.OutSine));
    }

    private void InsertShapeHide(Sequence sequence, float start)
    {
        sequence.InsertCallback(start, StopShapeSpin);
        sequence.Insert(
            start,
            _shapeCanvasGroup.DOFade(0f, _shapeHideDuration).SetEase(Ease.InSine));
        sequence.Insert(
            start,
            _shapeVisual.DOScale(_shapeBaseScale * _shapeHiddenScale, _shapeHideDuration)
                .SetEase(Ease.InSine));
    }

    private void StartShapeSpin()
    {
        StopShapeSpin();
        _shapeSpinTween = _shapeVisual
            .DOLocalRotate(Vector3.forward * 360f, _shapeSpinDuration, RotateMode.LocalAxisAdd)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart)
            .SetTarget(_shapeVisual)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable)
            .SetUpdate(true);
    }

    private void StopShapeSpin()
    {
        _shapeSpinTween?.Kill();
        _shapeSpinTween = null;
    }

    private void ApplyOpenState()
    {
        StopShapeSpin();

        float travel = GetOpenTravel();
        Vector2 topOpen = Vector2.up * travel;
        Vector2 bottomOpen = Vector2.down * travel;

        _topFar.anchoredPosition = topOpen;
        _topMid.anchoredPosition = topOpen;
        _topForeground.anchoredPosition = topOpen;
        _bottomFar.anchoredPosition = bottomOpen;
        _bottomMid.anchoredPosition = bottomOpen;
        _bottomForeground.anchoredPosition = bottomOpen;

        _shapeCanvasGroup.alpha = 0f;
        _shapeVisual.localScale = _shapeBaseScale * _shapeHiddenScale;
        _shapeVisual.localRotation = _shapeBaseRotation;
    }

    private void ApplyClosedState()
    {
        _topFar.anchoredPosition = Vector2.zero;
        _bottomFar.anchoredPosition = Vector2.zero;
        _topMid.anchoredPosition = Vector2.zero;
        _bottomMid.anchoredPosition = Vector2.zero;
        _topForeground.anchoredPosition = Vector2.zero;
        _bottomForeground.anchoredPosition = Vector2.zero;
    }

    private void ShowShapeImmediate()
    {
        _shapeCanvasGroup.alpha = 1f;
        _shapeVisual.localScale = _shapeBaseScale;
        _shapeVisual.localRotation = _shapeBaseRotation;
    }

    private float GetOpenTravel()
    {
        return _cloudAspectRoot.rect.height * _travelHeightMultiplier + _openMargin;
    }

    private float GetCoverDuration()
    {
        float farEnd = _farCoverStart + _bottomGroupDelay + _farCoverDuration;
        float midEnd = _midCoverStart + _bottomGroupDelay + _midCoverDuration;
        float foregroundEnd = _foregroundCoverStart + _bottomGroupDelay + _foregroundCoverDuration;
        return Mathf.Max(farEnd, midEnd, foregroundEnd);
    }

    private float GetRevealDuration()
    {
        float farEnd = _farRevealStart + _bottomGroupDelay + _farRevealDuration;
        float midEnd = _midRevealStart + _bottomGroupDelay + _midRevealDuration;
        float foregroundEnd = _foregroundRevealStart + _bottomGroupDelay + _foregroundRevealDuration;
        return Mathf.Max(farEnd, midEnd, foregroundEnd);
    }

    private void CacheCanonicalShapeState()
    {
        if (_hasCanonicalShapeState || _shapeVisual == null)
        {
            return;
        }

        _shapeBaseRotation = _shapeVisual.localRotation;
        _shapeBaseScale = _shapeVisual.localScale;
        _hasCanonicalShapeState = true;
    }

    private void KillAnimations()
    {
        _activeSequence?.Kill();
        _activeSequence = null;
        StopShapeSpin();

        if (_shapeVisual != null)
        {
            _shapeVisual.DOKill();
        }

        if (_shapeCanvasGroup != null)
        {
            _shapeCanvasGroup.DOKill();
        }
    }

    private bool HasRequiredReferences()
    {
        return _cloudAspectRoot != null
            && _topFar != null
            && _bottomFar != null
            && _topMid != null
            && _bottomMid != null
            && _topForeground != null
            && _bottomForeground != null
            && _shapeVisual != null
            && _shapeCanvasGroup != null;
    }
}
