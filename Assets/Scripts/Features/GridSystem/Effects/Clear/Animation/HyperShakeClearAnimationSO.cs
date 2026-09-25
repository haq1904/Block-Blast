using System;
using DG.Tweening;
using UnityEngine;

[CreateAssetMenu(fileName = "HyperShake_ClearAnimation", menuName = "Block Blast/Effects/Grid/Clear/Hyper Shake Animation")]
public class HyperShakeClearAnimationSO : ClearAnimationSO
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static MaterialPropertyBlock mpbCache;
    private static MaterialPropertyBlock MPB => mpbCache ??= new MaterialPropertyBlock();

    [Header("Hyper Shake")]
    [Tooltip("Total duration of intense violent shake in seconds.")]
    [Range(0.04f, 1f)]
    public float shakeDuration = 0.09f;

    [Tooltip("Shake position displacement amplitude at peak.")]
    [Range(0.01f, 0.5f)]
    public float shakeStrength = 0.08f;

    [Tooltip("Scale swell during shake.")]
    [Range(1.0f, 1.5f)]
    public float anticipationScale = 1.15f;

    [Tooltip("Additional scale vibration amplitude ramping up with size.")]
    [Range(0f, 0.2f)]
    public float scaleShakeStrength = 0.04f;

    [Tooltip("Easing curve for build-up ramp.")]
    public Ease rampEase = Ease.InQuad;

    [Header("Hyper Shake Shader Feedback")]
    [Tooltip("Pulses high-intensity HDR emission during high-frequency vibration (requires useShaderFeedback = true).")]
    public bool pulseEmission = true;

    [ColorUsage(true, true)]
    [Tooltip("HDR color used for overloading electrical emission.")]
    public Color emissionColor = Color.white;

    [Range(1f, 10f)]
    [Tooltip("Maximum emission intensity multiplier at peak oscillation.")]
    public float maxEmissionIntensity = 6.0f;

    [Tooltip("Initial pulse frequency in Hz at start of shake.")]
    [Range(1f, 10f)]
    public float minPulseFrequency = 3.5f;

    [Tooltip("Peak pulse frequency in Hz right before climax detonation.")]
    [Range(10f, 60f)]
    public float maxPulseFrequency = 28.0f;

    [Tooltip("Strobe contrast curve exponent (higher = sharper, punchier electrical flashes).")]
    [Range(1f, 4f)]
    public float pulseSharpness = 2.0f;

    public override float PreExplosionDuration => shakeDuration;

    public override void Play(Transform target, ClearCellContext context, Action onExplode)
    {
        if (target == null) return;

        target.DOKill();

        // 1. Enforce start strictly at root canonical transform
        Vector3 rootPos = context.canonicalPos != Vector3.zero ? context.canonicalPos : target.position;
        target.position = rootPos;
        target.rotation = Quaternion.identity;
        target.localScale = Vector3.one;

        // Cache renderer & baseline albedo color via sharedMaterial (never create runtime material instance)
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

        // 2. Progressive shake ramping up with size from root transform + Shader Feedback
        seq.Append(DOTween.To(() => 0f, x =>
        {
            if (target == null) return;

            // Size swells from 1.0 to anticipationScale
            float baseScale = Mathf.Lerp(1.0f, anticipationScale, x);

            // Shake intensity builds up progressively as size increases (0 at start -> max at climax)
            float currentPosShake = shakeStrength * x;
            float currentScaleShake = scaleShakeStrength * x;

            // Frequency-based vibration anchored around canonical root position
            Vector3 offset = UnityEngine.Random.onUnitSphere * (currentPosShake * UnityEngine.Random.Range(0.6f, 1f));
            offset.y *= 0.4f; // Keep vertical jitter grounded on board plane
            target.position = rootPos + offset;

            // Scale with micro-jitter
            float scaleJitter = (UnityEngine.Random.value * 2f - 1f) * currentScaleShake;
            float finalScale = Mathf.Max(0.1f, baseScale + scaleJitter);
            target.localScale = new Vector3(finalScale, finalScale, finalScale);

            // Shader Feedback: accelerating white electrical pulse emission & HDR glow via MaterialPropertyBlock
            if (renderer != null)
            {
                float strobe = 1f;
                if (pulseEmission)
                {
                    // Chirp phase integration: frequency ramps up smoothly from min to max
                    float freqRamp = Mathf.Lerp(minPulseFrequency, maxPulseFrequency, x * 0.5f);
                    float phase = x * freqRamp * Mathf.PI * 2f;
                    float rawWave = Mathf.Sin(phase) * 0.5f + 0.5f;
                    strobe = Mathf.Pow(rawWave, pulseSharpness);
                }

                // Baseline continuous glow heating up as block approaches climax detonation
                float baseGlow = Mathf.Pow(x, 2.5f) * 1.5f;
                // High-intensity white strobe pulses reaching up to maxEmissionIntensity (overpowers albedo and blooms)
                float pulseGlow = strobe * Mathf.Lerp(1.0f, maxEmissionIntensity, x);
                float totalIntensity = pulseGlow + baseGlow;

                Color currentEmission = emissionColor * totalIntensity;
                Color currentBase = Color.Lerp(defaultBaseColor, Color.white * 2.0f, strobe * x);

                MPB.SetColor(EmissionColorId, currentEmission);
                MPB.SetColor(BaseColorId, currentBase);
                renderer.SetPropertyBlock(MPB);
            }
        }, 1f, shakeDuration).SetEase(rampEase));

        // 3. Climax reached: spawn self VFX layers, clean up MaterialPropertyBlock, and trigger explosion callback
        seq.AppendCallback(() =>
        {
            SpawnVFX(target, rootPos, Quaternion.identity);

            // Mandatory cleanup: reset PropertyBlock before returning object to pool
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
