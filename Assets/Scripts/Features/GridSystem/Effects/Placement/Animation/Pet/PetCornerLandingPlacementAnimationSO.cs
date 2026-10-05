using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "PetCornerLanding_PlacementAnimation", menuName = "Block Blast/Effects/Grid/Placement/Pet/Corner Landing Animation")]
public class PetCornerLandingPlacementAnimationSO : PlacementAnimationSO
{
    [Header("Landing")]
    [Min(0f)]
    [Tooltip("Height above the canonical placement position where the tilted block starts.")]
    public float dropHeight = 0.15f;

    [Range(0f, 10f)]
    [Tooltip("Maximum local tilt angle used for the corner-first landing.")]
    public float landingAngle = 4f;

    [Tooltip("Subtle compression applied after the tilted block contacts the board.")]
    public Vector3 impactScale = new Vector3(1.025f, 0.94f, 1.025f);

    [Tooltip("If true, randomly chooses local X or local Z as the landing axis.")]
    public bool randomizeAxis = true;

    [Tooltip("If true, randomly flips the corner landing direction.")]
    public bool randomizeDirection = true;

    [Header("Timing")]
    [Min(0.01f)]
    [Tooltip("Total duration of the tilted drop, contact, counter rock, and settle sequence.")]
    public float duration = 0.19f;

    public override void Apply(Transform target)
    {
        if (target == null) return;

        target.DOKill();

        Vector3 canonicalPosition = target.localPosition;
        Quaternion canonicalRotation = target.localRotation;
        float safeDuration = Mathf.Max(0.01f, duration);
        float dropDuration = safeDuration * 0.32f;
        float contactDuration = safeDuration * 0.24f;
        float counterDuration = safeDuration * 0.26f;
        float settleDuration = safeDuration * 0.18f;
        bool useXAxis = !randomizeAxis || Random.value < 0.5f;
        float direction = randomizeDirection && Random.value < 0.5f ? -1f : 1f;
        float actualAngle = Mathf.Max(0f, landingAngle) * direction;
        Vector3 landingEuler = useXAxis ? new Vector3(actualAngle, 0f, 0f) : new Vector3(0f, 0f, actualAngle);
        Vector3 contactEuler = landingEuler * 0.55f;
        Vector3 counterEuler = -landingEuler * 0.25f;

        target.localPosition = canonicalPosition + Vector3.up * Mathf.Max(0f, dropHeight);
        target.localRotation = canonicalRotation * Quaternion.Euler(landingEuler);
        target.localScale = Vector3.one;

        Sequence sequence = DOTween.Sequence();
        sequence.SetTarget(target);
        sequence.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);

        sequence.Append(target.DOLocalMove(canonicalPosition, dropDuration).SetEase(Ease.InQuad));
        sequence.Append(target.DOLocalRotateQuaternion(canonicalRotation * Quaternion.Euler(contactEuler), contactDuration).SetEase(Ease.OutQuad));
        sequence.Join(target.DOScale(impactScale, contactDuration).SetEase(Ease.OutQuad));
        sequence.Append(target.DOLocalRotateQuaternion(canonicalRotation * Quaternion.Euler(counterEuler), counterDuration).SetEase(Ease.InOutSine));
        sequence.Join(target.DOScale(Vector3.Lerp(impactScale, Vector3.one, 0.75f), counterDuration).SetEase(Ease.OutSine));
        sequence.Append(target.DOLocalRotateQuaternion(canonicalRotation, settleDuration).SetEase(Ease.OutSine));
        sequence.Join(target.DOScale(Vector3.one, settleDuration).SetEase(Ease.OutSine));
    }
}
