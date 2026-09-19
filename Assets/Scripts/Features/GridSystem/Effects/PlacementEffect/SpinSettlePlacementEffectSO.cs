using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "SpinSettle_PlacementEffect", menuName = "Block Blast/Effects/Placement/Spin Settle")]
public class SpinSettlePlacementEffectSO : PlacementEffectSO
{
    [Tooltip("Base angle in degrees around the Y axis from which the block rotates into aligned position.")]
    public float spinAngle = 15f;

    [Tooltip("Random angle variation offset applied to the spin angle (+/- range).")]
    public float randomAngleOffset = 10f;

    [Tooltip("If true, randomly chooses clockwise or counter-clockwise rotation.")]
    public bool randomizeDirection = true;

    [Tooltip("Duration of the spin snap.")]
    public float duration = 0.18f;

    [Tooltip("Easing curve for the snap alignment.")]
    public Ease easeType = Ease.OutBack;

    public override void Apply(Transform target)
    {
        if (target == null) return;
        target.DOKill();
        target.localScale = Vector3.one;

        float baseMagnitude = Mathf.Abs(spinAngle);
        float actualMagnitude = Mathf.Max(1f, baseMagnitude + Random.Range(-randomAngleOffset, randomAngleOffset));
        float sign = randomizeDirection ? (Random.value < 0.5f ? 1f : -1f) : (spinAngle >= 0f ? 1f : -1f);
        float effectiveSpinAngle = actualMagnitude * sign;

        target.localRotation = Quaternion.Euler(0f, effectiveSpinAngle, 0f);

        target.DOLocalRotate(Vector3.zero, duration)
            .SetEase(easeType)
            .SetTarget(target)
            .SetLink(target.gameObject, LinkBehaviour.KillOnDisable);
    }
}
