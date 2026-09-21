using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "TensionShrink_PreClearAnimation", menuName = "Block Blast/Effects/Grid/PreClear/Tension Shrink Animation")]
public class TensionShrinkPreClearAnimationSO : PreClearAnimationSO
{
    [Tooltip("Target scale multiplier when under tension compression.")]
    public float shrinkScale = 0.85f;

    [Tooltip("Duration of a single scale compression/expansion cycle.")]
    public float duration = 0.22f;

    [Tooltip("Micro-vibration shake amplitude.")]
    public float shakeStrength = 0.03f;

    [Tooltip("Vibration frequency rate.")]
    public int vibrato = 20;

    public override void Apply(Transform target)
    {
        if (target == null) return;
        target.DOKill();

        target.DOScale(Vector3.one * shrinkScale, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(target.gameObject, LinkBehaviour.KillOnDisable);

        target.DOShakePosition(duration * 2f, shakeStrength, vibrato, 90f, false, false)
            .SetLoops(-1)
            .SetLink(target.gameObject, LinkBehaviour.KillOnDisable);
    }

    public override void Cancel(Transform target, Vector3 originalPos, Quaternion originalRot)
    {
        base.Cancel(target, originalPos, originalRot);
    }
}
