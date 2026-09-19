using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "Levitating_Effect", menuName = "Block Blast/Effects/Levitating")]
public class LevitatingEffectSO : PreClearEffectSO
{
    [Tooltip("Peak vertical levitation elevation above the board.")]
    public float floatHeight = 0.35f;

    [Tooltip("Multi-axis tilt oscillation angle.")]
    public Vector3 tiltAngle = new Vector3(5f, 0f, 5f);

    [Tooltip("Cycle duration of the zero-gravity bobbing animation.")]
    public float duration = 0.5f;

    [Tooltip("Easing curve for gentle floating.")]
    public Ease easeType = Ease.InOutSine;

    public override void Apply(Transform target)
    {
        if (target == null) return;
        target.DOKill();

        float targetY = target.position.y + floatHeight;
        Quaternion baseRot = target.rotation;

        target.DOMoveY(targetY, duration)
            .SetEase(easeType)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(target.gameObject, LinkBehaviour.KillOnDisable);

        float rotDuration = duration * 1.25f;
        float halfRotDuration = rotDuration * 0.5f;

        Sequence rotSeq = DOTween.Sequence();
        rotSeq.SetTarget(target);
        rotSeq.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);

        // 1. Swing from base orientation (0 deg) to +tiltAngle
        rotSeq.Append(target.DORotateQuaternion(baseRot * Quaternion.Euler(tiltAngle), halfRotDuration).SetEase(Ease.OutSine));
        // 2. Full swing from +tiltAngle to -tiltAngle
        rotSeq.Append(target.DORotateQuaternion(baseRot * Quaternion.Euler(-tiltAngle), rotDuration).SetEase(easeType));
        // 3. Return from -tiltAngle back to base orientation
        rotSeq.Append(target.DORotateQuaternion(baseRot, halfRotDuration).SetEase(Ease.InSine));

        rotSeq.SetLoops(-1);
    }

    public override void Cancel(Transform target, Vector3 originalPos, Quaternion originalRot)
    {
        base.Cancel(target, originalPos, originalRot);
    }
}
