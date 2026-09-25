using System;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "Jelly_ClearAnimation", menuName = "Block Blast/Effects/Grid/Clear/Jelly Animation")]
public class JellyClearAnimationSO : ClearAnimationSO
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static MaterialPropertyBlock mpbCache;
    private static MaterialPropertyBlock MPB => mpbCache ??= new MaterialPropertyBlock();

    [Header("Phase 1: Extreme Squash")]
    [Tooltip("Scale deformation when squeezed flat against the board.")]
    public Vector3 squashScale = new Vector3(1.35f, 0.25f, 1.35f);

    [Tooltip("Duration of the squash phase in seconds.")]
    [Range(0.04f, 0.4f)]
    public float squashDuration = 0.12f;

    [Tooltip("Easing curve for the squash compression.")]
    public Ease squashEase = Ease.OutQuad;

    [Header("Phase 2: Swell Expansion")]
    [Tooltip("Scale expansion at peak climax before bursting.")]
    public Vector3 swellScale = new Vector3(1.40f, 1.45f, 1.40f);

    [Tooltip("Duration of the explosive swell phase in seconds.")]
    [Range(0.04f, 0.3f)]
    public float swellDuration = 0.10f;

    [Tooltip("Easing curve for the explosive swell.")]
    public Ease swellEase = Ease.InQuad;

    [Header("Random Variation")]
    [Tooltip("Random offset range applied to squash and swell scales (+- range). Set to 0 to disable.")]
    [Range(0f, 0.3f)]
    public float scaleRandomRange = 0.1f;

    [Header("Shader Feedback")]
    [Tooltip("Enable HDR glow flash during swell climax.")]
    public bool enableClimaxFlash = true;

    [ColorUsage(true, true)]
    [Tooltip("HDR flash color applied during swell climax.")]
    public Color glowColor = Color.white;

    [Range(1f, 8f)]
    [Tooltip("Maximum emission intensity multiplier at peak climax.")]
    public float maxGlowIntensity = 4.0f;

    public override float PreExplosionDuration => squashDuration + swellDuration;

    public override void Play(Transform target, ClearCellContext context, Action onExplode)
    {
        if (target == null) return;

        target.DOKill();

        // 1. Enforce start strictly at root canonical transform
        Vector3 rootPos = context.canonicalPos != Vector3.zero ? context.canonicalPos : target.position;
        target.position = rootPos;
        target.rotation = Quaternion.identity;
        target.localScale = Vector3.one;

        // Calculate randomized scale targets for this specific block instance (stateless local variables)
        Vector3 randomizedSquash = squashScale;
        Vector3 randomizedSwell = swellScale;

        if (scaleRandomRange > 0f)
        {
            randomizedSquash = new Vector3(
                squashScale.x + UnityEngine.Random.Range(-scaleRandomRange, scaleRandomRange),
                Mathf.Max(0.05f, squashScale.y + UnityEngine.Random.Range(-scaleRandomRange, scaleRandomRange)),
                squashScale.z + UnityEngine.Random.Range(-scaleRandomRange, scaleRandomRange)
            );

            randomizedSwell = new Vector3(
                swellScale.x + UnityEngine.Random.Range(-scaleRandomRange, scaleRandomRange),
                swellScale.y + UnityEngine.Random.Range(-scaleRandomRange, scaleRandomRange),
                swellScale.z + UnityEngine.Random.Range(-scaleRandomRange, scaleRandomRange)
            );
        }

        // Cache renderer & baseline albedo color via sharedMaterial
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

        // Phase 1: Extreme Squash (Mesh pivot is at the bottom, so scaling Y flattens naturally against the floor)
        seq.Append(target.DOScale(randomizedSquash, squashDuration).SetEase(squashEase));

        // Phase 2: Explosive Swell (Scale expands blown up large, white flash builds up)
        seq.Append(DOTween.To(() => 0f, p =>
        {
            if (target == null) return;

            float t = DOVirtual.EasedValue(0f, 1f, p, swellEase);
            target.localScale = Vector3.LerpUnclamped(randomizedSquash, randomizedSwell, t);

            // Shader Feedback: White HDR flash ramping up towards climax
            if (renderer != null && enableClimaxFlash)
            {
                float intensity = p * maxGlowIntensity;
                Color currentEmission = glowColor * intensity;
                Color currentBase = Color.Lerp(defaultBaseColor, Color.white * 2.0f, p);

                MPB.SetColor(EmissionColorId, currentEmission);
                MPB.SetColor(BaseColorId, currentBase);
                renderer.SetPropertyBlock(MPB);
            }
        }, 1f, swellDuration));

        // Climax reached: spawn 3-tier VFX layers, clean up shader feedback and invoke explosion callback
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
