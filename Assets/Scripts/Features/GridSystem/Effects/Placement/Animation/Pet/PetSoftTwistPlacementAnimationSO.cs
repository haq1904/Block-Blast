using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "PetSoftTwist_PlacementAnimation", menuName = "Block Blast/Effects/Grid/Placement/Pet/Soft Twist Animation")]
public class PetSoftTwistPlacementAnimationSO : PlacementAnimationSO
{
    [Header("Twisted Drop")]
    [Min(0f)]
    [Tooltip("Height above the canonical placement position where the block starts.")]
    public float dropHeight = 0.11f;

    [Range(0f, 30f)]
    [Tooltip("Initial local yaw offset in degrees.")]
    public float twistAngle = 8f;

    [Range(0f, 1f)]
    [Tooltip("Multiplier used for the small counter twist after landing.")]
    public float counterRatio = 0.28f;

    [Tooltip("Soft compression applied during the counter twist.")]
    public Vector3 impactScale = new Vector3(1.035f, 0.91f, 1.035f);

    [Tooltip("If true, randomly flips the initial twist direction.")]
    public bool randomizeDirection = true;

    [Header("Timing")]
    [Min(0.01f)]
    [Tooltip("Total duration of the twisted drop, counter twist, return, and settle sequence.")]
    public float duration = 0.19f;

    public override void Apply(Transform target)
    {
        if (target == null) return;

        target.DOKill();

        Vector3 canonicalPosition = target.localPosition;
        Quaternion canonicalRotation = target.localRotation;
        float safeDuration = Mathf.Max(0.01f, duration);
        float dropDuration = safeDuration * 0.30f;
        float counterDuration = safeDuration * 0.28f;
        float returnDuration = safeDuration * 0.24f;
        float settleDuration = safeDuration * 0.18f;
        float direction = randomizeDirection && Random.value < 0.5f ? -1f : 1f;
        float signedAngle = Mathf.Max(0f, twistAngle) * direction;
        float safeCounterRatio = Mathf.Clamp01(counterRatio);
        Quaternion initialRotation = canonicalRotation * Quaternion.Euler(0f, signedAngle, 0f);
        Quaternion counterRotation = canonicalRotation * Quaternion.Euler(0f, -signedAngle * safeCounterRatio, 0f);
        Quaternion returnRotation = canonicalRotation * Quaternion.Euler(0f, signedAngle * 0.12f, 0f);

        target.localPosition = canonicalPosition + Vector3.up * Mathf.Max(0f, dropHeight);
        target.localRotation = initialRotation;
        target.localScale = Vector3.one;

        Sequence sequence = DOTween.Sequence();
        sequence.SetTarget(target);
        sequence.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);

        sequence.Append(target.DOLocalMove(canonicalPosition, dropDuration).SetEase(Ease.InQuad));
        sequence.Append(target.DOLocalRotateQuaternion(counterRotation, counterDuration).SetEase(Ease.OutSine));
        sequence.Join(target.DOScale(impactScale, counterDuration).SetEase(Ease.OutSine));
        sequence.Append(target.DOLocalRotateQuaternion(returnRotation, returnDuration).SetEase(Ease.InOutSine));
        sequence.Join(target.DOScale(Vector3.Lerp(impactScale, Vector3.one, 0.85f), returnDuration).SetEase(Ease.OutSine));
        sequence.Append(target.DOLocalRotateQuaternion(canonicalRotation, settleDuration).SetEase(Ease.OutSine));
        sequence.Join(target.DOScale(Vector3.one, settleDuration).SetEase(Ease.OutSine));
    }
}
