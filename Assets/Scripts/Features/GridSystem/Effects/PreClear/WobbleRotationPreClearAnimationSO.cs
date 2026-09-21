using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "Wobble_PreClearAnimation", menuName = "Block Blast/Effects/Grid/PreClear/Wobble Animation")]
public class WobbleRotationPreClearAnimationSO : PreClearAnimationSO
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
        float halfDuration = duration * 0.5f;

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(target);
        seq.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);

        // 1. Swing from base (0 deg) to +tiltAngle
        seq.Append(target.DORotateQuaternion(baseRot * Quaternion.Euler(tiltAngle), halfDuration).SetEase(Ease.OutSine));
        // 2. Full swing from +tiltAngle to -tiltAngle
        seq.Append(target.DORotateQuaternion(baseRot * Quaternion.Euler(-tiltAngle), duration).SetEase(easeType));
        // 3. Return from -tiltAngle to base (0 deg)
        seq.Append(target.DORotateQuaternion(baseRot, halfDuration).SetEase(Ease.InSine));

        seq.SetLoops(-1);
    }

    public override void Cancel(Transform target, Vector3 originalPos, Quaternion originalRot)
    {
        base.Cancel(target, originalPos, originalRot);
    }
}

