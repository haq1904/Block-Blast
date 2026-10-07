using System;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "PetCarrierBoard_ClearAnimation", menuName = "Block Blast/Effects/Grid/Clear/Pet/Carrier Board")]
public class PetCarrierBoardClearAnimationSO : PetClearAnimationSOBase
{
    [Header("Ready")]
    [Range(0.01f, 0.15f)] public float readyDuration = 0.05f;
    [Range(0f, 15f)] public float readyLeanAngle = 4f;

    [Header("Hop")]
    [Range(0.04f, 0.3f)] public float hopDuration = 0.11f;
    [Range(0f, 0.75f)] public float hopForwardDistance = 0.20f;
    [Range(0.05f, 0.75f)] public float hopHeight = 0.28f;
    public Vector3 hopScale = new Vector3(0.94f, 1.10f, 0.94f);

    [Header("Board")]
    [Range(0.04f, 0.3f)] public float boardDuration = 0.10f;
    [Range(0f, 0.75f)] public float boardForwardDistance = 0.38f;
    [Range(0.05f, 0.5f)] public float endScale = 0.18f;
    [Range(0f, 30f)] public float boardTiltAngle = 12f;

    public override float PreExplosionDuration => readyDuration + hopDuration + boardDuration;

    public override void Play(Transform target, ClearCellContext context, Action onExplode)
    {
        if (target == null) return;

        PetAnimationRig rig = ResolveRig(target);
        Vector3 rootPos = context.canonicalPos;
        Quaternion rootRot = context.canonicalRotation;
        PrepareRig(rig, rootPos, rootRot);

        Vector3 direction = ResolveDirection(context.sweepDirection);
        Quaternion readyPivotRot = CalculatePivotLeanRotation(rig.root, rig.pivotRestRotation, direction, readyLeanAngle);
        Quaternion boardPivotRot = CalculatePivotLeanRotation(rig.root, rig.pivotRestRotation, direction, boardTiltAngle);

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(target);
        seq.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);
        if (context.delay > 0f) seq.AppendInterval(context.delay);

        // Ready: pivot leans anticipatorily
        seq.Append(rig.pivot.DOLocalRotateQuaternion(readyPivotRot, readyDuration).SetEase(Ease.OutSine));

        // Hop: root moves in world arc, pivot stretches
        Vector3 hopPos = rootPos + direction * hopForwardDistance + Vector3.up * hopHeight;
        seq.Append(rig.root.DOMove(hopPos, hopDuration).SetEase(Ease.OutQuad));
        seq.Join(rig.pivot.DOScale(Vector3.Scale(rig.pivotRestScale, hopScale), hopDuration).SetEase(Ease.OutQuad));

        // Board: root moves to board level, pivot tilts and shrinks into carrier
        Vector3 boardPos = rootPos + direction * boardForwardDistance + Vector3.up * 0.12f;
        seq.Append(rig.root.DOMove(boardPos, boardDuration).SetEase(Ease.InQuad));
        seq.Join(rig.pivot.DOScale(rig.pivotRestScale * endScale, boardDuration).SetEase(Ease.InBack));
        seq.Join(rig.pivot.DOLocalRotateQuaternion(boardPivotRot, boardDuration).SetEase(Ease.InQuad));

        seq.AppendCallback(() =>
        {
            ResetRig(rig, boardPos, rootRot);
            SpawnVFX(target, boardPos, rootRot);
            onExplode?.Invoke();
        });
    }
}
