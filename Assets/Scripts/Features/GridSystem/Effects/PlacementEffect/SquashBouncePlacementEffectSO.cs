using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "SquashBounce_PlacementEffect", menuName = "Block Blast/Effects/Placement/Squash Bounce")]
public class SquashBouncePlacementEffectSO : PlacementEffectSO
{
    [Tooltip("Punch scale deformation vector (nudge XZ out, compress Y down).")]
    public Vector3 punchScale = new Vector3(0.12f, -0.15f, 0.12f);

    [Tooltip("Duration of the squash and bounce animation.")]
    public float duration = 0.16f;

    [Tooltip("Number of vibration oscillations.")]
    public int vibrato = 5;

    [Tooltip("Elasticity factor representing how much springiness remains after the initial impact.")]
    [Range(0f, 1f)]
    public float elasticity = 0.5f;

    public override void Apply(Transform target)
    {
        if (target == null) return;
        target.DOKill();
        target.localScale = Vector3.one;
        target.localRotation = Quaternion.identity;

        target.DOPunchScale(punchScale, duration, vibrato, elasticity)
            .SetTarget(target)
            .SetLink(target.gameObject, LinkBehaviour.KillOnDisable);
    }
}
