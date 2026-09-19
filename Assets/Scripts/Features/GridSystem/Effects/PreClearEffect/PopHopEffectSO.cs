using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "PopHop_Effect", menuName = "Block Blast/Effects/Pop Hop")]
public class PopHopEffectSO : PreClearEffectSO
{
    [Tooltip("Peak height of each vertical hop.")]
    public float jumpPower = 0.25f;

    [Tooltip("Duration of a single hop cycle.")]
    public float duration = 0.32f;

    [Tooltip("Maximum random initial offset delay to stagger hopping among neighboring blocks.")]
    public float randomDelay = 0.1f;

    public override void Apply(Transform target)
    {
        if (target == null) return;
        target.DOKill();

        Vector3 basePos = target.position;
        float delay = Random.Range(0f, randomDelay);

        target.DOJump(basePos, jumpPower, 1, duration)
            .SetDelay(delay)
            .SetLoops(-1)
            .SetLink(target.gameObject, LinkBehaviour.KillOnDisable);
    }

    public override void Cancel(Transform target, Vector3 originalPos, Quaternion originalRot)
    {
        base.Cancel(target, originalPos, originalRot);
    }
}
