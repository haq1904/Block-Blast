using System;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "WoodChop_ClearAnimation", menuName = "Block Blast/Effects/Grid/Clear/Wood Chop Animation")]
public class WoodChopClearAnimationSO : ClearAnimationSO
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static MaterialPropertyBlock mpbCache;
    private static MaterialPropertyBlock MPB => mpbCache ??= new MaterialPropertyBlock();

    [Header("Phase 1: Anticipation Lift")]
    [Tooltip("Vertical lift height before slamming down.")]
    [Range(0.05f, 0.30f)]
    public float liftHeight = 0.15f;

    [Tooltip("Duration of anticipation lift in seconds.")]
    [Range(0.03f, 0.20f)]
    public float liftDuration = 0.06f;

    [Header("Phase 2: Heavy Chop Slam & Lateral Split")]
    [Tooltip("Scale deformation when slammed and split laterally across X axis.")]
    public Vector3 splitScale = new Vector3(1.35f, 0.65f, 0.85f);

    [Tooltip("Duration of slam and split in seconds.")]
    [Range(0.04f, 0.25f)]
    public float chopDuration = 0.08f;

    [Tooltip("Easing curve for chopping slam.")]
    public Ease chopEase = Ease.InQuad;



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

        // Phase 1: Quick anticipation lift
        Vector3 liftedPos = rootPos + new Vector3(0f, liftHeight, 0f);
        seq.Append(target.DOMove(liftedPos, liftDuration).SetEase(Ease.OutQuad));

        // Phase 2: Downward slam back to root position + violent lateral split
        Sequence slamSeq = DOTween.Sequence();
        slamSeq.SetTarget(target);
        slamSeq.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);
        slamSeq.Join(target.DOMove(rootPos, chopDuration).SetEase(chopEase));
        slamSeq.Join(target.DOScale(splitScale, chopDuration).SetEase(chopEase));

        if (renderer != null && useShaderFeedback)
        {
            slamSeq.Join(DOTween.To(() => 0f, p =>
            {
                if (target == null || renderer == null) return;
                Color currentEmission = flashColor * (p * 3.5f);
                Color currentBase = Color.Lerp(defaultBaseColor, flashColor * 1.5f, p);
                MPB.SetColor(EmissionColorId, currentEmission);
                MPB.SetColor(BaseColorId, currentBase);
                renderer.SetPropertyBlock(MPB);
            }, 1f, chopDuration));
        }

        seq.Append(slamSeq);

        // Phase 3: Climax reached
        seq.AppendCallback(() =>
        {
            SpawnVFX(target, rootPos, Quaternion.identity);

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
