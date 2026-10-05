using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "PetSoftPlop_PlacementAnimation", menuName = "Block Blast/Effects/Grid/Placement/Pet/Soft Plop Animation")]
public class PetSoftPlopPlacementAnimationSO : PlacementAnimationSO
{
    [Header("Drop")]
    [Min(0f)]
    [Tooltip("Height above the canonical placement position where the block starts.")]
    public float dropHeight = 0.18f;

    [Tooltip("Scale applied while the block begins its short drop.")]
    public Vector3 fallStretchScale = new Vector3(0.97f, 1.05f, 0.97f);

    [Header("Soft Impact")]
    [Tooltip("Wide, compressed scale used at floor impact.")]
    public Vector3 impactScale = new Vector3(1.06f, 0.86f, 1.06f);

    [Tooltip("Small vertical rebound applied after impact.")]
    public Vector3 reboundScale = new Vector3(0.99f, 1.035f, 0.99f);

    [Header("Timing")]
    [Min(0.01f)]
    [Tooltip("Total duration of the drop, squash, rebound, and settle sequence.")]
    public float duration = 0.18f;

    public override void Apply(Transform target)
    {
        if (target == null) return;

        target.DOKill();

        Vector3 canonicalPosition = target.localPosition;
        Quaternion canonicalRotation = target.localRotation;
        float safeDuration = Mathf.Max(0.01f, duration);
        float dropDuration = safeDuration * 0.30f;
        float impactDuration = safeDuration * 0.25f;
        float reboundDuration = safeDuration * 0.25f;
        float settleDuration = safeDuration * 0.20f;

        target.localPosition = canonicalPosition + Vector3.up * Mathf.Max(0f, dropHeight);
        target.localRotation = canonicalRotation;
        target.localScale = fallStretchScale;

        Sequence sequence = DOTween.Sequence();
        sequence.SetTarget(target);
        sequence.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);

        sequence.Append(target.DOLocalMove(canonicalPosition, dropDuration).SetEase(Ease.InQuad));
        sequence.Join(target.DOScale(Vector3.one, dropDuration).SetEase(Ease.InSine));
        sequence.Append(target.DOScale(impactScale, impactDuration).SetEase(Ease.OutQuad));
        sequence.Append(target.DOScale(reboundScale, reboundDuration).SetEase(Ease.OutSine));
        sequence.Append(target.DOScale(Vector3.one, settleDuration).SetEase(Ease.InOutSine));
    }
}
