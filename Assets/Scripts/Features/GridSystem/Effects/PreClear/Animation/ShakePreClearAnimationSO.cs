using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "Shake_PreClearAnimation", menuName = "Block Blast/Effects/Grid/PreClear/Shake Animation")]
public class ShakePreClearAnimationSO : PreClearAnimationSO
{
    [Tooltip("Strength of positional vibration.")]
    public float strength = 0.08f;

    [Tooltip("Frequency of vibration oscillation per cycle.")]
    public int vibrato = 15;

    [Tooltip("Duration of one shake cycle.")]
    public float duration = 0.3f;

    public override void Apply(Transform target)
    {
        if (target == null) return;
        target.DOKill();
        target.DOShakePosition(duration, strength, vibrato, 90f, false, false)
              .SetLoops(-1, LoopType.Restart)
              .SetLink(target.gameObject, LinkBehaviour.KillOnDisable);
    }

    public override void Cancel(Transform target, Vector3 originalPos, Quaternion originalRot)
    {
        base.Cancel(target, originalPos, originalRot);
    }
}
