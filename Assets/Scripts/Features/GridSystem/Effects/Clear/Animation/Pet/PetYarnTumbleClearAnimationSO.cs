using System;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "PetYarnTumble_ClearAnimation", menuName = "Block Blast/Effects/Grid/Clear/Pet/Yarn Tumble")]
public class PetYarnTumbleClearAnimationSO : ClearAnimationSO
{
    [Header("Tug")]
    [Range(0.02f, 0.2f)] public float tugDuration = 0.06f;
    [Range(0f, 0.2f)] public float tugDistance = 0.05f;
    public Vector3 tugScale = new Vector3(1.08f, 0.92f, 1f);

    [Header("Tumble")]
    [Range(0.04f, 0.35f)] public float tumbleDuration = 0.15f;
    [Range(0f, 0.75f)] public float tumbleDistance = 0.30f;
    [Range(0f, 0.6f)] public float tumbleHeight = 0.18f;
    [Range(45f, 360f)] public float tumbleDegrees = 150f;

    [Header("Wrap")]
    [Range(0.03f, 0.25f)] public float wrapDuration = 0.08f;
    [Range(0.05f, 0.5f)] public float endScale = 0.15f;
    [Range(0f, 360f)] public float wrapYaw = 120f;

    public override float PreExplosionDuration => tugDuration + tumbleDuration + wrapDuration;

    public override void Play(Transform target, ClearCellContext context, Action onExplode)
    {
        if (target == null) return;

        target.DOKill();

        Vector3 rootPos = context.canonicalPos;
        Quaternion rootRot = context.canonicalRotation;
        Vector3 direction = ResolveDirection(context.sweepDirection);
        Vector3 rollAxis = Vector3.Cross(Vector3.up, direction).normalized;
        float angleJitter = UnityEngine.Random.Range(-15f, 15f);
        Quaternion tumbleRot = Quaternion.AngleAxis(tumbleDegrees + angleJitter, rollAxis) * rootRot;
        Quaternion wrapRot = Quaternion.AngleAxis(wrapYaw, Vector3.up) * tumbleRot;

        target.position = rootPos;
        target.rotation = rootRot;
        target.localScale = Vector3.one;

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(target);
        seq.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);
        if (context.delay > 0f) seq.AppendInterval(context.delay);

        Vector3 tugPos = rootPos + direction * tugDistance;
        Vector3 directionalTugScale = Mathf.Abs(direction.x) > Mathf.Abs(direction.z)
            ? tugScale
            : new Vector3(tugScale.z, tugScale.y, tugScale.x);
        seq.Append(target.DOMove(tugPos, tugDuration).SetEase(Ease.InQuad));
        seq.Join(target.DOScale(directionalTugScale, tugDuration).SetEase(Ease.InQuad));

        Vector3 tumblePos = rootPos + direction * tumbleDistance + Vector3.up * tumbleHeight;
        seq.Append(target.DOMove(tumblePos, tumbleDuration).SetEase(Ease.OutSine));
        seq.Join(target.DORotateQuaternion(tumbleRot, tumbleDuration).SetEase(Ease.InOutSine));
        seq.Join(target.DOScale(Vector3.one, tumbleDuration).SetEase(Ease.OutSine));

        seq.Append(target.DOScale(Vector3.one * endScale, wrapDuration).SetEase(Ease.InBack));
        seq.Join(target.DORotateQuaternion(wrapRot, wrapDuration).SetEase(Ease.InQuad));
        seq.AppendCallback(() =>
        {
            SpawnVFX(target, tumblePos, target.rotation);
            onExplode?.Invoke();
        });
    }

    private static Vector3 ResolveDirection(Vector3 direction)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
    }
}
