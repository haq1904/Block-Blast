using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "PopSwell_PlacementAnimation", menuName = "Block Blast/Effects/Grid/Placement/Pop Swell Animation")]
public class PopSwellPlacementAnimationSO : PlacementAnimationSO
{
    [Tooltip("Initial reduced scale when placed.")]
    public float startScaleMultiplier = 0.8f;

    [Tooltip("Maximum scale overshoot during the pop animation.")]
    public float overshootScaleMultiplier = 1.15f;

    [Tooltip("Duration of the pop animation.")]
    public float duration = 0.18f;

    public override void Apply(Transform target)
    {
        if (target == null) return;
        target.DOKill();
        target.localScale = Vector3.one * startScaleMultiplier;
        target.localRotation = Quaternion.identity;

        Sequence popSeq = DOTween.Sequence();
        popSeq.SetTarget(target);
        popSeq.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);

        popSeq.Append(target.DOScale(Vector3.one * overshootScaleMultiplier, duration * 0.6f).SetEase(Ease.OutBack));
        popSeq.Append(target.DOScale(Vector3.one, duration * 0.4f).SetEase(Ease.InOutSine));
    }
}
