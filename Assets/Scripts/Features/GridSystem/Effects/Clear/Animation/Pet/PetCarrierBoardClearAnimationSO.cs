using System;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "PetCarrierBoard_ClearAnimation", menuName = "Block Blast/Effects/Grid/Clear/Pet/Carrier Board")]
public class PetCarrierBoardClearAnimationSO : ClearAnimationSO
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

        target.DOKill();

        Vector3 rootPos = context.canonicalPos;
        Quaternion rootRot = context.canonicalRotation;
        Vector3 direction = ResolveDirection(context.sweepDirection);
        Vector3 leanAxis = Vector3.Cross(Vector3.up, direction).normalized;
        Quaternion readyRot = Quaternion.AngleAxis(readyLeanAngle, leanAxis) * rootRot;
        Quaternion boardRot = Quaternion.AngleAxis(boardTiltAngle, leanAxis) * rootRot;

        target.position = rootPos;
        target.rotation = rootRot;
        target.localScale = Vector3.one;

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(target);
        seq.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);
        if (context.delay > 0f) seq.AppendInterval(context.delay);

        seq.Append(target.DORotateQuaternion(readyRot, readyDuration).SetEase(Ease.OutSine));

        Vector3 hopPos = rootPos + direction * hopForwardDistance + Vector3.up * hopHeight;
        seq.Append(target.DOMove(hopPos, hopDuration).SetEase(Ease.OutQuad));
        seq.Join(target.DOScale(hopScale, hopDuration).SetEase(Ease.OutQuad));

        Vector3 boardPos = rootPos + direction * boardForwardDistance + Vector3.up * 0.12f;
        seq.Append(target.DOMove(boardPos, boardDuration).SetEase(Ease.InQuad));
        seq.Join(target.DOScale(Vector3.one * endScale, boardDuration).SetEase(Ease.InBack));
        seq.Join(target.DORotateQuaternion(boardRot, boardDuration).SetEase(Ease.InQuad));
        seq.AppendCallback(() =>
        {
            SpawnVFX(target, boardPos, target.rotation);
            onExplode?.Invoke();
        });
    }

    private static Vector3 ResolveDirection(Vector3 direction)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
    }
}
