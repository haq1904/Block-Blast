using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveLift_Effect", menuName = "Block Blast/Effects/Grid/PreClear/Wave Lift")]
public class WaveLiftEffectSO : PreClearEffectSO
{
    [Tooltip("Peak lift height along the vertical Y-axis.")]
    public float liftHeight = 0.28f;

    [Tooltip("Duration of a single half-cycle (rise or fall).")]
    public float duration = 0.35f;

    [Tooltip("Propagation delay multiplier per unit of distance from epicenter.")]
    public float waveSpeed = 0.08f;

    [Tooltip("Easing curve for sinusoidal oscillation.")]
    public Ease easeType = Ease.InOutSine;

    public override void Apply(Transform target)
    {
        if (target == null) return;
        Apply(target, target.position);
    }

    public override void Apply(Transform target, Vector3 rippleOrigin)
    {
        if (target == null) return;
        target.DOKill();

        Vector2 targetXZ = new Vector2(target.position.x, target.position.z);
        Vector2 originXZ = new Vector2(rippleOrigin.x, rippleOrigin.z);
        float distance = Vector2.Distance(targetXZ, originXZ);
        float delay = distance * waveSpeed;

        float targetY = target.position.y + liftHeight;

        target.DOMoveY(targetY, duration)
            .SetEase(easeType)
            .SetDelay(delay)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(target.gameObject, LinkBehaviour.KillOnDisable);
    }

    public override void Cancel(Transform target, Vector3 originalPos, Quaternion originalRot)
    {
        base.Cancel(target, originalPos, originalRot);
    }
}
