using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "DropSlam_PlacementEffect", menuName = "Block Blast/Effects/Grid/Placement/Drop Slam")]
public class DropSlamPlacementEffectSO : PlacementEffectSO
{
    [Tooltip("Height above the board from which the block drops.")]
    public float dropHeight = 0.35f;

    [Tooltip("Time taken for the block to slam down to the board.")]
    public float dropDuration = 0.08f;

    [Tooltip("Height of the micro-bounce upon hitting the board.")]
    public float bounceHeight = 0.06f;

    [Tooltip("Duration of the rebound bounce.")]
    public float bounceDuration = 0.08f;

    public override void Apply(Transform target)
    {
        if (target == null) return;
        target.DOKill();
        target.localScale = Vector3.one;
        target.localRotation = Quaternion.identity;

        float groundY = target.position.y;
        target.position = new Vector3(target.position.x, groundY + dropHeight, target.position.z);

        Sequence slamSeq = DOTween.Sequence();
        slamSeq.SetTarget(target);
        slamSeq.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);

        slamSeq.Append(target.DOMoveY(groundY, dropDuration).SetEase(Ease.InQuad));
        slamSeq.Append(target.DOMoveY(groundY + bounceHeight, bounceDuration * 0.5f).SetEase(Ease.OutQuad));
        slamSeq.Append(target.DOMoveY(groundY, bounceDuration * 0.5f).SetEase(Ease.InQuad));
    }
}
