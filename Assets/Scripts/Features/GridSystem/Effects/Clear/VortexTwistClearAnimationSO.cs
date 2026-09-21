using System;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "VortexTwist_ClearAnimation", menuName = "Block Blast/Effects/Grid/Clear/Vortex Twist Animation")]
public class VortexTwistClearAnimationSO : ClearAnimationSO
{
    [Header("Vortex Twist")]
    [Tooltip("Total duration of vortex inward twist in seconds.")]
    [Range(0.04f, 0.3f)]
    public float twistDuration = 0.10f;

    [Tooltip("Total spin rotation around Y-axis in degrees.")]
    [Range(180f, 720f)]
    public float spinDegrees = 360f;

    [Tooltip("Shrink scale at the center of vortex.")]
    [Range(0.01f, 0.2f)]
    public float endScale = 0.05f;

    public override void Play(Transform target, ClearCellContext context, Action onExplode)
    {
        if (target == null) return;

        target.DOKill();

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(target);
        seq.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);

        if (context.delay > 0f)
        {
            seq.AppendInterval(context.delay);
        }

        // Rapid inward twist and spin
        seq.Append(target.DOScale(endScale, twistDuration).SetEase(Ease.InBack));
        seq.Join(target.DORotate(new Vector3(0f, spinDegrees, 0f), twistDuration, RotateMode.FastBeyond360).SetEase(Ease.InQuad));

        // Climax reached: trigger explosion callback
        seq.AppendCallback(() => onExplode?.Invoke());
    }
}
