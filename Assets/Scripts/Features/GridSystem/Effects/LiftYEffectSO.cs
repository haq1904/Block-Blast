using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "LiftY_Effect", menuName = "Block Blast/Effects/Lift Y")]
public class LiftYEffectSO : PreClearEffectSO
{
    [Tooltip("Base height offset along the Y-axis when elevated.")]
    public float liftHeight = 0.25f;

    [Tooltip("Base duration of one elevation cycle.")]
    public float duration = 0.22f;

    [Tooltip("Easing function for smooth breathing motion.")]
    public Ease easeType = Ease.InOutSine;

    [Header("Independent Motion Tuning")]
    [Tooltip("Maximum random start delay in seconds so blocks oscillate independently.")]
    public float maxStartDelay = 0.15f;

    [Tooltip("Random variation added/subtracted to liftHeight for organic movement.")]
    public float heightVariation = 0.04f;

    [Tooltip("Random variation added/subtracted to duration for independent rhythm.")]
    public float durationVariation = 0.03f;

    public override void Apply(Transform target)
    {
        if (target == null) return;
        target.DOKill();

        float finalHeight = liftHeight + Random.Range(-heightVariation, heightVariation);
        float finalDuration = Mathf.Max(0.08f, duration + Random.Range(-durationVariation, durationVariation));
        float delay = Random.Range(0f, maxStartDelay);

        target.DOMoveY(target.position.y + finalHeight, finalDuration)
              .SetDelay(delay)
              .SetLoops(-1, LoopType.Yoyo)
              .SetEase(easeType);
    }

    public override void Cancel(Transform target, Vector3 originalPos, Quaternion originalRot)
    {
        base.Cancel(target, originalPos, originalRot);
    }
}

