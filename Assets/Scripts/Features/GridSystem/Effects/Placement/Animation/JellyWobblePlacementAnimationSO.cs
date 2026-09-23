using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "JellyWobble_PlacementAnimation", menuName = "Block Blast/Effects/Grid/Placement/Jelly Wobble Animation")]
public class JellyWobblePlacementAnimationSO : PlacementAnimationSO
{
    [Header("Scale Squash & Stretch")]
    [Tooltip("Initial squash scale upon floor impact (wider XZ, compressed Y).")]
    public Vector3 squashScale = new Vector3(1.25f, 0.72f, 1.25f);

    [Tooltip("Rebound stretch scale springing upward (narrower XZ, elongated Y).")]
    public Vector3 stretchScale = new Vector3(0.86f, 1.22f, 0.86f);

    [Tooltip("Secondary micro-squash scale before settling.")]
    public Vector3 settleSquashScale = new Vector3(1.08f, 0.94f, 1.08f);

    [Tooltip("Random scale variation offset applied to squash and stretch phases (+/- range).")]
    public float randomScaleOffset = 0.25f;

    [Header("Vertical Hop")]
    [Tooltip("Rebound hop elevation distance when springing upward.")]
    public float reboundHeight = 0.12f;

    [Header("Wobble Rotation")]
    [Tooltip("Base swing tilt angle in degrees (set Y for twist wobble).")]
    public Vector3 tiltAngle = new Vector3(0f, 15f, 0f);

    [Tooltip("Random angle variation offset added to the Y axis (+/- range).")]
    public float randomYOffset = 4f;

    [Tooltip("If true, randomly flips the initial rotation direction (clockwise or counter-clockwise).")]
    public bool randomizeDirection = false;

    [Header("Timing")]
    [Tooltip("Total duration of the jelly settling sequence.")]
    public float duration = 0.32f;

    public override void Apply(Transform target)
    {
        if (target == null) return;
        target.DOKill();
        target.localScale = Vector3.one;
        target.localRotation = Quaternion.identity;

        float baseY = target.position.y;
        float t1 = duration * 0.22f;
        float t2 = duration * 0.32f;
        float t3 = duration * 0.26f;
        float t4 = duration * 0.20f;

        // Calculate randomized scale vectors for the 3 phases
        Vector3 actualSquash = ApplyRandomScaleOffset(squashScale, randomScaleOffset);
        Vector3 actualStretch = ApplyRandomScaleOffset(stretchScale, randomScaleOffset);
        Vector3 actualSettle = ApplyRandomScaleOffset(settleSquashScale, randomScaleOffset);

        // Calculate randomized rotation angle based on inspector settings
        float actualY = tiltAngle.y + Random.Range(-randomYOffset, randomYOffset);
        if (randomizeDirection && Random.value < 0.5f)
        {
            actualY = -actualY;
        }
        Vector3 effectiveTilt = new Vector3(tiltAngle.x, actualY, tiltAngle.z);

        // --- Motion & Scale Sequence ---
        Sequence motionSeq = DOTween.Sequence();
        motionSeq.SetTarget(target);
        motionSeq.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);

        // Phase 1: Heavy impact squash down
        motionSeq.Append(target.DOScale(actualSquash, t1).SetEase(Ease.OutQuad));

        // Phase 2: Rebound stretch up + vertical hop
        motionSeq.Append(target.DOScale(actualStretch, t2).SetEase(Ease.OutQuad));
        if (reboundHeight > 0f)
        {
            motionSeq.Join(target.DOMoveY(baseY + reboundHeight, t2).SetEase(Ease.OutQuad));
        }

        // Phase 3: Fall back down + secondary micro-squash
        motionSeq.Append(target.DOScale(actualSettle, t3).SetEase(Ease.InQuad));
        if (reboundHeight > 0f)
        {
            motionSeq.Join(target.DOMoveY(baseY, t3).SetEase(Ease.InQuad));
        }

        // Phase 4: Settle back to default
        motionSeq.Append(target.DOScale(Vector3.one, t4).SetEase(Ease.OutQuad));

        // --- Wobble Rotation Sequence ---
        Sequence rotSeq = DOTween.Sequence();
        rotSeq.SetTarget(target);
        rotSeq.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);

        // Phase 1: Tilt to effectiveTilt
        rotSeq.Append(target.DOLocalRotate(effectiveTilt, t1).SetEase(Ease.OutQuad));

        // Phase 2: Swing across to opposite side with damped overshoot
        rotSeq.Append(target.DOLocalRotate(-effectiveTilt * 0.65f, t2).SetEase(Ease.InOutQuad));

        // Phase 3: Swing back with decaying oscillation
        rotSeq.Append(target.DOLocalRotate(effectiveTilt * 0.3f, t3).SetEase(Ease.InOutQuad));

        // Phase 4: Settle back upright
        rotSeq.Append(target.DOLocalRotate(Vector3.zero, t4).SetEase(Ease.OutQuad));
    }

    private static Vector3 ApplyRandomScaleOffset(Vector3 baseScale, float offsetRange)
    {
        if (offsetRange <= 0f) return baseScale;
        float rx = Random.Range(-offsetRange, offsetRange);
        float ry = Random.Range(-offsetRange, offsetRange);
        float rz = Random.Range(-offsetRange, offsetRange);
        return new Vector3(
            Mathf.Max(0.05f, baseScale.x + rx),
            Mathf.Max(0.05f, baseScale.y + ry),
            Mathf.Max(0.05f, baseScale.z + rz)
        );
    }
}
