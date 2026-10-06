using System;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "PetBubbleLift_ClearAnimation", menuName = "Block Blast/Effects/Grid/Clear/Pet/Bubble Lift")]
public class PetBubbleLiftClearAnimationSO : ClearAnimationSO
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static MaterialPropertyBlock mpbCache;
    private static MaterialPropertyBlock MPB => mpbCache ??= new MaterialPropertyBlock();

    [Header("Bubble Capture")]
    public GameObject bubbleShellPrefab;
    [Range(0.02f, 0.2f)] public float captureDuration = 0.07f;
    public Vector3 captureScale = new Vector3(1.04f, 0.90f, 1.04f);

    [Header("Buoyant Lift")]
    [Range(0.05f, 0.5f)] public float liftDuration = 0.20f;
    [Range(0.1f, 1.5f)] public float liftHeight = 0.65f;
    [Range(0f, 0.2f)] public float swayDistance = 0.05f;
    [Range(0.5f, 1f)] public float liftedScale = 0.92f;
    [Range(0f, 90f)] public float liftYaw = 20f;

    [Header("Bubble Pop")]
    [Range(0.02f, 0.15f)] public float popDuration = 0.05f;
    [Range(1f, 1.3f)] public float shellPopScale = 1.12f;

    public override float PreExplosionDuration => captureDuration + liftDuration + popDuration;

    public override void Play(Transform target, ClearCellContext context, Action onExplode)
    {
        if (target == null) return;

        target.DOKill();

        Vector3 rootPos = context.canonicalPos;
        Quaternion rootRot = context.canonicalRotation;
        Vector3 direction = ResolveDirection(context.sweepDirection);
        Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
        float swaySign = ((context.indexInLine & 1) == 0) ? 1f : -1f;
        Vector3 peakPos = rootPos + Vector3.up * liftHeight + side * (swayDistance * swaySign);
        Quaternion peakRot = rootRot * Quaternion.Euler(0f, liftYaw * swaySign, 0f);

        target.position = rootPos;
        target.rotation = rootRot;
        target.localScale = Vector3.one;

        GameObject shell = SpawnBubbleShell(rootPos, rootRot, context.delay, peakPos);
        MeshRenderer renderer = useShaderFeedback ? target.GetComponentInChildren<MeshRenderer>() : null;
        Color baseColor = GetBaseColor(renderer);

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(target);
        seq.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);
        if (context.delay > 0f) seq.AppendInterval(context.delay);

        seq.Append(target.DOScale(captureScale, captureDuration).SetEase(Ease.OutQuad));
        seq.Append(target.DOMove(peakPos, liftDuration).SetEase(Ease.OutSine));
        seq.Join(target.DOScale(Vector3.one * liftedScale, liftDuration).SetEase(Ease.OutSine));
        seq.Join(target.DORotateQuaternion(peakRot, liftDuration).SetEase(Ease.InOutSine));
        seq.Append(DOTween.To(() => 0f, value => ApplyFlash(renderer, baseColor, value), 1f, popDuration).SetEase(Ease.OutQuad));
        seq.AppendCallback(() =>
        {
            if (renderer != null) renderer.SetPropertyBlock(null);
            ReturnShell(shell);
            SpawnVFX(target, peakPos, target.rotation);
            onExplode?.Invoke();
        });
    }

    public override void Cancel(Transform target, Vector3 canonicalPos, Quaternion canonicalRot)
    {
        base.Cancel(target, canonicalPos, canonicalRot);
        MeshRenderer renderer = target != null ? target.GetComponentInChildren<MeshRenderer>() : null;
        if (renderer != null) renderer.SetPropertyBlock(null);
    }

    private GameObject SpawnBubbleShell(Vector3 startPos, Quaternion rotation, float delay, Vector3 peakPos)
    {
        if (bubbleShellPrefab == null || !ServiceLocator.TryGet<IPoolService>(out var poolService)) return null;

        GameObject shell = poolService.SpawnObject(bubbleShellPrefab, startPos, rotation, PoolType.GameObject);
        if (shell == null) return null;

        shell.transform.DOKill();
        shell.transform.position = startPos;
        shell.transform.rotation = rotation;
        shell.transform.localScale = Vector3.zero;

        Sequence shellSeq = DOTween.Sequence();
        shellSeq.SetTarget(shell.transform);
        shellSeq.SetLink(shell, LinkBehaviour.KillOnDisable);
        if (delay > 0f) shellSeq.AppendInterval(delay);
        shellSeq.Append(shell.transform.DOScale(Vector3.one, captureDuration).SetEase(Ease.OutBack));
        shellSeq.Append(shell.transform.DOMove(peakPos, liftDuration).SetEase(Ease.OutSine));
        shellSeq.Append(shell.transform.DOScale(Vector3.one * shellPopScale, popDuration).SetEase(Ease.OutQuad));
        return shell;
    }

    private static void ReturnShell(GameObject shell)
    {
        if (shell == null || !shell.activeSelf) return;
        var autoReturn = shell.GetComponent<AutoReturnToPool>();
        if (autoReturn != null) autoReturn.ReturnToPool();
        else if (ServiceLocator.TryGet<IPoolService>(out var poolService)) poolService.ReturnObjectToPool(shell, PoolType.GameObject);
        else shell.SetActive(false);
    }

    private static Color GetBaseColor(MeshRenderer renderer)
    {
        if (renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(BaseColorId))
        {
            return renderer.sharedMaterial.GetColor(BaseColorId);
        }
        return Color.white;
    }

    private void ApplyFlash(MeshRenderer renderer, Color baseColor, float progress)
    {
        if (renderer == null || !useShaderFeedback) return;
        MPB.Clear();
        renderer.GetPropertyBlock(MPB);
        MPB.SetColor(BaseColorId, Color.Lerp(baseColor, flashColor, progress));
        renderer.SetPropertyBlock(MPB);
    }

    private static Vector3 ResolveDirection(Vector3 direction)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
    }
}
