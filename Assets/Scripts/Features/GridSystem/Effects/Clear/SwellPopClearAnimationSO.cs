using System;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "SwellPop_ClearAnimation", menuName = "Block Blast/Effects/Grid/Clear/Swell Pop Animation")]
public class SwellPopClearAnimationSO : ClearAnimationSO
{
    [Header("Anticipation Squash")]
    [Tooltip("Target scale during energy anticipation/compression.")]
    [Range(0.5f, 1.0f)]
    public float anticipationScale = 0.85f;

    [Tooltip("Duration of compression phase in seconds.")]
    [Range(0.02f, 0.2f)]
    public float anticipationDuration = 0.05f;

    [Header("Pop Burst")]
    [Tooltip("Target scale when swollen to maximum burst volume.")]
    [Range(1.1f, 2.0f)]
    public float burstScale = 1.35f;

    [Tooltip("Duration of sudden swelling burst in seconds.")]
    [Range(0.02f, 0.2f)]
    public float burstDuration = 0.07f;

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

        // 1. Quick anticipation compression
        seq.Append(target.DOScale(anticipationScale, anticipationDuration).SetEase(Ease.InQuad));

        // 2. Sudden explosive swelling
        seq.Append(target.DOScale(burstScale, burstDuration).SetEase(Ease.OutQuad));

        // 3. Climax reached: trigger explosion callback
        seq.AppendCallback(() => onExplode?.Invoke());
    }
}
