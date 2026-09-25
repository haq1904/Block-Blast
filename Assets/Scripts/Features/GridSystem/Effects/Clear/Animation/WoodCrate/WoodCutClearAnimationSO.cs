using System;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "WoodCut_ClearAnimation", menuName = "Block Blast/Effects/Grid/Clear/Wood Cut Animation")]
public class WoodCutClearAnimationSO : ClearAnimationSO
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static MaterialPropertyBlock mpbCache;
    private static MaterialPropertyBlock MPB => mpbCache ??= new MaterialPropertyBlock();

    [Header("Phase 1: Heavy Compression")]
    [Tooltip("Scale when under heavy structural load compressed against the board.")]
    public Vector3 compressionScale = new Vector3(1.08f, 0.85f, 1.08f);

    [Tooltip("Duration of heavy compression phase in seconds.")]
    [Range(0.04f, 0.25f)]
    public float compressionDuration = 0.08f;

    [Tooltip("Easing curve for compression.")]
    public Ease compressionEase = Ease.OutQuad;

    [Header("Phase 2: Stress Tremor & Flash")]
    [Tooltip("Duration of high-frequency tremor phase before snapping.")]
    [Range(0.04f, 1f)]
    public float tremorDuration = 0.10f;

    [Tooltip("Maximum tilt angle in degrees during stress creaking tremor.")]
    [Range(1f, 10f)]
    public float maxTiltAngle = 3.5f;

    [Tooltip("Enable micro-position rattle / displacement during tremor.")]
    public bool enablePositionRattle = true;

    [Tooltip("Maximum position rattle displacement in world units.")]
    [Range(0.01f, 0.12f)]
    public float maxPositionRattle = 0.04f;

    [Range(1f, 100f)]
    [Tooltip("Peak emission intensity during flash.")]
    public float maxGlowIntensity = 6.0f;

    [Tooltip("If true, temporarily overrides the texture with pure white during flash to create a 100% uniform glowing silhouette on all 6 faces.")]
    public bool enablePureWhiteSilhouette = true;

    [Tooltip("Progress threshold (0-1) in tremor phase when texture swaps to pure white silhouette.")]
    [Range(0f, 1f)]
    public float whiteSilhouetteThreshold = 0.75f;

    public override float PreExplosionDuration => compressionDuration + tremorDuration;

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

        // Phase 1: Heavy compression load against floor
        seq.Append(target.DOScale(compressionScale, compressionDuration).SetEase(compressionEase));

        // Phase 2: Accelerating stress tremor with micro-position rattle & escalating HDR flash
        seq.Append(DOTween.To(() => 0f, p =>
        {
            if (target == null) return;

            // Frequency scales dynamically with duration so it ramps up fast regardless of duration length
            float dynamicCycles = Mathf.Max(6f, tremorDuration * 24f);
            float frequencyProgress = Mathf.Pow(p, 2.2f); // Non-linear chirp ramp (starts slow, escalates frantically)
            float amplitudeFactor = Mathf.Lerp(0.45f, 1.35f, p);

            // 1. Angular Creaking Tilt (X & Z)
            float tiltFreq = frequencyProgress * Mathf.PI * 2f * dynamicCycles;
            float tiltZ = Mathf.Sin(tiltFreq) * (maxTiltAngle * amplitudeFactor);
            float tiltX = Mathf.Cos(tiltFreq * 0.78f) * (maxTiltAngle * 0.65f * amplitudeFactor);
            target.rotation = Quaternion.Euler(tiltX, 0f, tiltZ);

            // 2. Micro Position Rattle (Physical vibration against floor)
            if (enablePositionRattle)
            {
                float rattleFreq = frequencyProgress * Mathf.PI * 2f * (dynamicCycles * 1.6f);
                float posX = Mathf.Sin(rattleFreq * 1.37f) * (maxPositionRattle * amplitudeFactor);
                float posZ = Mathf.Cos(rattleFreq * 1.83f) * (maxPositionRattle * amplitudeFactor);
                // Vertical vibration: micro-bounce / thump against grid plane
                float posY = -Mathf.Abs(Mathf.Sin(rattleFreq * 2.11f)) * (maxPositionRattle * 0.75f * amplitudeFactor);

                target.position = rootPos + new Vector3(posX, posY, posZ);
            }

            // 3. Shader feedback: Escalating HDR flash with pure white silhouette override
            if (renderer != null && useShaderFeedback)
            {
                float ramp = Mathf.Pow(p, 2.0f);
                float flicker = Mathf.Abs(Mathf.Sin(tiltFreq * 2f)) * (0.35f * p);
                float intensityFactor = ramp + flicker;

                Color currentEmission = flashColor * (intensityFactor * maxGlowIntensity);
                MPB.SetColor(EmissionColorId, currentEmission);

                // Base color boost: strictly 1.0 (matte LDR) when maxGlowIntensity == 0, scaling up to 2.5 HDR only when glow intensity is raised
                float baseHdrBoost = 1.0f + Mathf.Clamp01(maxGlowIntensity / 4f) * 1.5f;

                if (enablePureWhiteSilhouette && p >= whiteSilhouetteThreshold)
                {
                    // Climax overcharge: 100% pure white on all 6 faces, wiping out wood grain
                    MPB.SetTexture(BaseMapId, Texture2D.whiteTexture);
                    MPB.SetColor(BaseColorId, Color.white * baseHdrBoost);
                }
                else
                {
                    Color currentBase = Color.Lerp(defaultBaseColor, flashColor * baseHdrBoost, ramp);
                    MPB.SetColor(BaseColorId, currentBase);
                }

                renderer.SetPropertyBlock(MPB);
            }
        }, 1f, tremorDuration));

        // Phase 3: Climax reached - clean up property block, spawn VFX, trigger explosion callback
        seq.AppendCallback(() =>
        {
            SpawnVFX(target, rootPos, Quaternion.identity);

            if (renderer != null)
            {
                renderer.SetPropertyBlock(null);
            }

            target.position = rootPos;
            target.rotation = Quaternion.identity;

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
