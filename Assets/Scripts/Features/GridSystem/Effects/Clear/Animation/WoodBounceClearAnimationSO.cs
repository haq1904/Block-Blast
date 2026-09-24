using System;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "WoodBounce_ClearAnimation", menuName = "Block Blast/Effects/Grid/Clear/Wood Bounce Animation")]
public class WoodBounceClearAnimationSO : ClearAnimationSO
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static MaterialPropertyBlock mpbCache;
    private static MaterialPropertyBlock MPB => mpbCache ??= new MaterialPropertyBlock();

    [Header("Phase 1: Dry Stiff Squat")]
    [Tooltip("Compression scale before snapping upward.")]
    public Vector3 squatScale = new Vector3(1.15f, 0.72f, 1.15f);

    [Tooltip("Duration of squatting compression.")]
    [Range(0.04f, 0.20f)]
    public float squatDuration = 0.07f;

    [Header("Phase 2: High-Tension Pop Up")]
    [Tooltip("Peak vertical pop height before bursting.")]
    [Range(0.05f, 0.40f)]
    public float popHeight = 0.18f;

    [Tooltip("Elongated scale during rapid vertical ascent.")]
    public Vector3 stretchScale = new Vector3(0.82f, 1.28f, 0.82f);

    [Tooltip("Duration of pop ascent.")]
    [Range(0.04f, 0.25f)]
    public float popDuration = 0.08f;



    public override void Play(Transform target, ClearCellContext context, Action onExplode)
    {
        if (target == null) return;

        target.DOKill();

        Vector3 rootPos = context.canonicalPos != Vector3.zero ? context.canonicalPos : target.position;
        target.position = rootPos;
        target.rotation = Quaternion.identity;
        target.localScale = Vector3.one;

        MeshRenderer renderer = useShaderFeedback ? target.GetComponentInChildren<MeshRenderer>() : null;
        Color defaultBaseColor = Color.white;
        if (renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(BaseColorId))
        {
            defaultBaseColor = renderer.sharedMaterial.GetColor(BaseColorId);
        }

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(target);
        seq.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);

        if (context.delay > 0f)
        {
            seq.AppendInterval(context.delay);
        }

        // Phase 1: Stiff downward squash
        seq.Append(target.DOScale(squatScale, squatDuration).SetEase(Ease.OutQuad));

        // Phase 2: Violent snappy pop upwards
        Sequence popSeq = DOTween.Sequence();
        popSeq.SetTarget(target);
        popSeq.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);

        Vector3 apexPos = rootPos + new Vector3(0f, popHeight, 0f);
        popSeq.Join(target.DOMove(apexPos, popDuration).SetEase(Ease.InBack, 1.2f));
        popSeq.Join(target.DOScale(stretchScale, popDuration).SetEase(Ease.OutQuad));

        if (renderer != null && useShaderFeedback)
        {
            popSeq.Join(DOTween.To(() => 0f, p =>
            {
                if (target == null || renderer == null) return;
                Color currentEmission = flashColor * (p * 3.8f);
                Color currentBase = Color.Lerp(defaultBaseColor, flashColor * 1.4f, p);
                MPB.SetColor(EmissionColorId, currentEmission);
                MPB.SetColor(BaseColorId, currentBase);
                renderer.SetPropertyBlock(MPB);
            }, 1f, popDuration));
        }

        seq.Append(popSeq);

        // Phase 3: Climax reached at apex
        seq.AppendCallback(() =>
        {
            SpawnVFX(target, apexPos, Quaternion.identity);

            if (renderer != null)
            {
                renderer.SetPropertyBlock(null);
            }

            onExplode?.Invoke();
        });
    }

    public override void Cancel(Transform target, Vector3 canonicalPos, Quaternion canonicalRot)
    {
        base.Cancel(target, canonicalPos, canonicalRot);

        if (target != null)
        {
            var renderer = target.GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
            {
                renderer.SetPropertyBlock(null);
            }
        }
    }
}
