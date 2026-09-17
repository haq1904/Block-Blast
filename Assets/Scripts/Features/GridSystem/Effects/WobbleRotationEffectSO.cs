using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "Wobble_Effect", menuName = "Block Blast/Effects/Wobble")]
public class WobbleRotationEffectSO : PreClearEffectSO
{
    [Tooltip("Euler angle tilt amplitude (e.g. Z = 8 degrees).")]
    public Vector3 tiltAngle = new Vector3(0f, 0f, 8f);

    [Tooltip("Duration of one swing from left to right.")]
    public float duration = 0.28f;

    [Tooltip("Easing function for smooth synchronized swinging.")]
    public Ease easeType = Ease.InOutSine;

    public override void Apply(Transform target)
    {
        if (target == null) return;
        target.DOKill();

        Quaternion baseRot = target.rotation;
        target.rotation = baseRot * Quaternion.Euler(-tiltAngle);

        target.DORotateQuaternion(baseRot * Quaternion.Euler(tiltAngle), duration)
              .SetLoops(-1, LoopType.Yoyo)
              .SetEase(easeType);
    }

    public override void Cancel(Transform target, Vector3 originalPos, Quaternion originalRot)
    {
        base.Cancel(target, originalPos, originalRot);
    }
}

