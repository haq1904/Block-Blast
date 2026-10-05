using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "PetSideWobble_PlacementAnimation", menuName = "Block Blast/Effects/Grid/Placement/Pet/Side Wobble Animation")]
public class PetSideWobblePlacementAnimationSO : PlacementAnimationSO
{
    [Header("Drop")]
    [Min(0f)]
    [Tooltip("Height above the canonical placement position where the block starts.")]
    public float dropHeight = 0.10f;

    [Tooltip("Soft compression applied during the first lean.")]
    public Vector3 impactScale = new Vector3(1.035f, 0.93f, 1.035f);

    [Header("Wobble")]
    [Range(0f, 10f)]
    [Tooltip("Maximum local tilt angle used for the first side lean.")]
    public float wobbleAngle = 2.8f;

    [Range(0f, 1f)]
    [Tooltip("Multiplier applied to each successive wobble angle.")]
    public float damping = 0.45f;

    [Tooltip("If true, randomly chooses local X or local Z as the tilt axis.")]
    public bool randomizeAxis = true;

    [Tooltip("If true, randomly flips the initial wobble direction.")]
    public bool randomizeDirection = true;

    [Header("Timing")]
    [Min(0.01f)]
    [Tooltip("Total duration of the drop, damped wobble, and settle sequence.")]
    public float duration = 0.20f;

    public override void Apply(Transform target)
    {
        if (target == null) return;

        target.DOKill();

        Vector3 canonicalPosition = target.localPosition;
        Quaternion canonicalRotation = target.localRotation;
        float safeDuration = Mathf.Max(0.01f, duration);
        float firstLeanDuration = safeDuration * 0.30f;
        float counterLeanDuration = safeDuration * 0.30f;
        float microReturnDuration = safeDuration * 0.22f;
        float settleDuration = safeDuration * 0.18f;
        bool useXAxis = !randomizeAxis || Random.value < 0.5f;
        float direction = randomizeDirection && Random.value < 0.5f ? -1f : 1f;
        float firstAngle = Mathf.Max(0f, wobbleAngle) * direction;
        float safeDamping = Mathf.Clamp01(damping);
        Vector3 firstEuler = useXAxis ? new Vector3(firstAngle, 0f, 0f) : new Vector3(0f, 0f, firstAngle);
        Vector3 counterEuler = -firstEuler * safeDamping;
        Vector3 microEuler = firstEuler * safeDamping * safeDamping;

        target.localPosition = canonicalPosition + Vector3.up * Mathf.Max(0f, dropHeight);
        target.localRotation = canonicalRotation;
        target.localScale = Vector3.one;

        Sequence sequence = DOTween.Sequence();
        sequence.SetTarget(target);
        sequence.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);

        sequence.Append(target.DOLocalMove(canonicalPosition, firstLeanDuration).SetEase(Ease.InQuad));
        sequence.Join(target.DOLocalRotateQuaternion(canonicalRotation * Quaternion.Euler(firstEuler), firstLeanDuration).SetEase(Ease.OutQuad));
        sequence.Join(target.DOScale(impactScale, firstLeanDuration).SetEase(Ease.OutQuad));
        sequence.Append(target.DOLocalRotateQuaternion(canonicalRotation * Quaternion.Euler(counterEuler), counterLeanDuration).SetEase(Ease.InOutSine));
        sequence.Join(target.DOScale(Vector3.Lerp(impactScale, Vector3.one, 0.65f), counterLeanDuration).SetEase(Ease.OutSine));
        sequence.Append(target.DOLocalRotateQuaternion(canonicalRotation * Quaternion.Euler(microEuler), microReturnDuration).SetEase(Ease.InOutSine));
        sequence.Join(target.DOScale(Vector3.Lerp(impactScale, Vector3.one, 0.9f), microReturnDuration).SetEase(Ease.OutSine));
        sequence.Append(target.DOLocalRotateQuaternion(canonicalRotation, settleDuration).SetEase(Ease.OutSine));
        sequence.Join(target.DOScale(Vector3.one, settleDuration).SetEase(Ease.OutSine));
    }
}
