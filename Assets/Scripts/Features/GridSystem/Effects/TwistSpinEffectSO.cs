using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "TwistSpin_Effect", menuName = "Block Blast/Effects/Twist Spin")]
public class TwistSpinEffectSO : PreClearEffectSO
{
    [Tooltip("Maximum twist rotation angle around vertical Y-axis in degrees.")]
    public float twistAngle = 30f;

    [Tooltip("Duration of a full swing from positive to negative twist.")]
    public float duration = 0.32f;

    [Tooltip("Easing curve for sinusoidal back-and-forth twist.")]
    public Ease easeType = Ease.InOutSine;

    public override void Apply(Transform target)
    {
        if (target == null) return;
        target.DOKill();

        Quaternion baseRot = target.rotation;
        float halfDuration = duration * 0.5f;
        Vector3 tilt = new Vector3(0f, twistAngle, 0f);

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(target);
        seq.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);

        // 1. Swing from initial orientation (0 deg) to +twistAngle
        seq.Append(target.DORotateQuaternion(baseRot * Quaternion.Euler(tilt), halfDuration).SetEase(Ease.OutSine));
        // 2. Full swing from +twistAngle to -twistAngle
        seq.Append(target.DORotateQuaternion(baseRot * Quaternion.Euler(-tilt), duration).SetEase(easeType));
        // 3. Return from -twistAngle back to base orientation
        seq.Append(target.DORotateQuaternion(baseRot, halfDuration).SetEase(Ease.InSine));

        seq.SetLoops(-1);
    }

    public override void Cancel(Transform target, Vector3 originalPos, Quaternion originalRot)
    {
        base.Cancel(target, originalPos, originalRot);
    }
}
