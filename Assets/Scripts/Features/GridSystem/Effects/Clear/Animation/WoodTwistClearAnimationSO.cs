using System;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "WoodTwist_ClearAnimation", menuName = "Block Blast/Effects/Grid/Clear/Wood Twist Animation")]
public class WoodTwistClearAnimationSO : ClearAnimationSO
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static MaterialPropertyBlock mpbCache;
    private static MaterialPropertyBlock MPB => mpbCache ??= new MaterialPropertyBlock();

    [Header("Phase 1: Torsion Strain")]
    [Tooltip("Twist angle around Y axis under torsion strain (degrees).")]
    [Range(10f, 35f)]
    public float twistAngle = 22f;

    [Tooltip("Compression scale under torsional shear.")]
    public Vector3 shearScale = new Vector3(0.92f, 0.92f, 0.92f);

    [Tooltip("Duration of torsional winding.")]
    [Range(0.04f, 0.20f)]
    public float twistDuration = 0.08f;

    [Header("Phase 2: Counter Snap & Fracture")]
    [Tooltip("Counter-kick rotation angle (degrees).")]
    [Range(-20f, -5f)]
    public float counterKickAngle = -10f;

    [Tooltip("Outward bursting scale as fibers give way.")]
    public Vector3 burstScale = new Vector3(1.18f, 0.88f, 1.12f);

    [Tooltip("Duration of counter snap fracture.")]
    [Range(0.04f, 0.20f)]
    public float fractureDuration = 0.07f;



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

        // Phase 1: Winding torsion twist
        Sequence twistSeq = DOTween.Sequence();
        twistSeq.SetTarget(target);
        twistSeq.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);
        twistSeq.Join(target.DORotate(new Vector3(0f, twistAngle, 0f), twistDuration).SetEase(Ease.InQuad));
        twistSeq.Join(target.DOScale(shearScale, twistDuration).SetEase(Ease.InQuad));

        seq.Append(twistSeq);

        // Phase 2: Violent counter snap & bursting fiber deformation
        Sequence snapSeq = DOTween.Sequence();
        snapSeq.SetTarget(target);
        snapSeq.SetLink(target.gameObject, LinkBehaviour.KillOnDisable);
        snapSeq.Join(target.DORotate(new Vector3(0f, counterKickAngle, 0f), fractureDuration).SetEase(Ease.OutBack, 2.5f));
        snapSeq.Join(target.DOScale(burstScale, fractureDuration).SetEase(Ease.OutQuad));

        if (renderer != null && useShaderFeedback)
        {
            snapSeq.Join(DOTween.To(() => 0f, p =>
            {
                if (target == null || renderer == null) return;
                Color currentEmission = flashColor * (p * 4.0f);
                Color currentBase = Color.Lerp(defaultBaseColor, flashColor * 1.5f, p);
                MPB.SetColor(EmissionColorId, currentEmission);
                MPB.SetColor(BaseColorId, currentBase);
                renderer.SetPropertyBlock(MPB);
            }, 1f, fractureDuration));
        }

        seq.Append(snapSeq);

        // Phase 3: Climax reached
        seq.AppendCallback(() =>
        {
            SpawnVFX(target, rootPos, target.rotation);

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
