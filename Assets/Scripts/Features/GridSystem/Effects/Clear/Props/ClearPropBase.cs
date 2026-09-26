using DG.Tweening;
using UnityEngine;

/// <summary>
/// Abstract base ScriptableObject for line sweeper props (SawBlade, Axe, Hammer, Drill, etc.).
/// Encapsulates prop prefab reference, trajectory offsets, and prop-specific audio feedback.
/// Adheres strictly to dotween.md (Stateless SO) and visual_effects.md.
/// </summary>
public abstract class ClearPropBase : ScriptableObject
{
    [Header("Prop Prefab")]
    [Tooltip("The visual prefab spawned from pool in the scene (e.g. SawBlade.prefab, Axe.prefab).")]
    public GameObject propPrefab;

    [Header("Trajectory & Placement Settings")]
    [Tooltip("Distance along the line direction behind the start cut position where the prop swoops in from.")]
    public float spawnDistance = 1.0f;

    [Tooltip("Offset along the cutting line direction (positive = forward along line, negative = backward). Automatically adapts for horizontal rows and vertical columns.")]
    public float forwardOffset = 0f;

    [Tooltip("Cutting/contact height offset along the Y axis where the prop touches the blocks.")]
    public float heightOffset = 0.5f;

    [Tooltip("Vertical drop height above the cutting position before dropping down (e.g. +2.0 units).")]
    public float dropHeight = 2.0f;

    [Tooltip("Duration in seconds for the prop to drop down and fade in before cutting starts.")]
    public float entryDuration = 0.15f;

    [Tooltip("Duration in seconds for the prop to exit and fade out after cutting completes.")]
    public float exitDuration = 0.1f;

    [Header("Prop Fade Transitions")]
    [Tooltip("Controls whether alpha transparency fade is applied during entry/exit.")]
    public bool enableAlphaFade = true;

    [Tooltip("Controls whether scale transition is applied during entry/exit (vital for opaque materials or juicy pop-in/pop-out).")]
    public bool enableScaleFade = false;

    [Tooltip("Easing curve for fade-in transition.")]
    public Ease fadeInEase = Ease.Linear;

    [Tooltip("Easing curve for fade-out transition.")]
    public Ease fadeOutEase = Ease.InQuad;

    [Header("Prop Audio Feedback")]
    [Tooltip("Audio clip played when prop drops down/enters the board.")]
    public AudioClip entrySound;

    [Tooltip("Audio clip played when prop begins actively sweeping/cutting across the line.")]
    public AudioClip sweepSound;

    [Tooltip("Audio clip played when prop exits the board.")]
    public AudioClip exitSound;

    [Range(0f, 1f)]
    [Tooltip("Volume multiplier for prop audio clips.")]
    public float soundVolume = 1.0f;

    /// <summary>
    /// Total duration in seconds spent before the sweep movement begins.
    /// Used by staggers to synchronize the anticipation of the first exploding block.
    /// Defaults to entryDuration (for immediate slide-in props like SawBlade).
    /// </summary>
    public virtual float TotalPreSweepDuration => entryDuration;

    public virtual void PlayEntrySound() => PlaySound(entrySound);
    public virtual void PlaySweepSound() => PlaySound(sweepSound);
    public virtual void PlayExitSound() => PlaySound(exitSound);

    protected void PlaySound(AudioClip clip)
    {
        if (clip != null && ServiceLocator.TryGet<ISoundFXService>(out var soundService))
        {
            soundService.PlaySound(clip, soundVolume);
        }
    }

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static MaterialPropertyBlock mpbCache;
    private static MaterialPropertyBlock MPB => mpbCache ??= new MaterialPropertyBlock();

    /// <summary>
    /// Adjusts transparency across all child renderers and material slots without cloning materials (shaders.md).
    /// Restores full opacity and clears the property block when alpha reaches 1f to maintain SRP batching.
    /// </summary>
    public static void SetPropAlpha(GameObject prop, float alpha)
    {
        if (prop == null) return;
        var renderers = prop.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0) return;

        if (alpha >= 0.999f)
        {
            for (int r = 0; r < renderers.Length; r++)
            {
                var rend = renderers[r];
                if (rend == null) continue;
                int matCount = rend.sharedMaterials != null ? rend.sharedMaterials.Length : 1;
                for (int s = 0; s < matCount; s++)
                {
                    rend.SetPropertyBlock(null, s);
                }
            }
            return;
        }

        float clampedAlpha = Mathf.Clamp01(alpha);
        for (int r = 0; r < renderers.Length; r++)
        {
            var rend = renderers[r];
            if (rend == null) continue;

            int matCount = rend.sharedMaterials != null ? rend.sharedMaterials.Length : 1;
            for (int s = 0; s < matCount; s++)
            {
                Color baseColor = Color.white;
                if (rend.sharedMaterials != null && s < rend.sharedMaterials.Length && rend.sharedMaterials[s] != null)
                {
                    var mat = rend.sharedMaterials[s];
                    if (mat.HasProperty(BaseColorId))
                    {
                        baseColor = mat.GetColor(BaseColorId);
                    }
                }

                baseColor.a = clampedAlpha;
                rend.GetPropertyBlock(MPB, s);
                MPB.SetColor(BaseColorId, baseColor);
                rend.SetPropertyBlock(MPB, s);
            }
        }
    }

    /// <summary>
    /// Controls particle emission across all child ParticleSystems on the prop.
    /// Automatically orients any ParticlesPivot transform opposite to the cutting direction if provided.
    /// </summary>
    public static void SetPropParticles(GameObject prop, bool play, Vector3? sprayDir = null)
    {
        if (prop == null) return;

        if (sprayDir.HasValue && sprayDir.Value.sqrMagnitude > 0.001f)
        {
            Transform pivot = prop.transform.Find("ParticlesPivot") ?? prop.transform.Find("particlesPivot");
            if (pivot != null)
            {
                Vector3 spray = (-sprayDir.Value + Vector3.up * 0.4f).normalized;
                pivot.rotation = Quaternion.LookRotation(spray, Vector3.up);
            }
        }

        var particleSystems = prop.GetComponentsInChildren<ParticleSystem>(true);
        if (particleSystems == null) return;

        for (int i = 0; i < particleSystems.Length; i++)
        {
            var ps = particleSystems[i];
            if (ps == null) continue;
            if (play)
            {
                ps.Play();
            }
            else
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    /// <summary>
    /// Initializes prop visuals for entry. Hides the prop via alpha = 0 and/or scale = 0 based on configuration.
    /// </summary>
    public virtual void PreparePropForEntry(GameObject prop)
    {
        if (prop == null) return;
        if (enableAlphaFade)
        {
            SetPropAlpha(prop, 0f);
        }
        if (enableScaleFade)
        {
            prop.transform.localScale = Vector3.zero;
        }
    }

    /// <summary>
    /// Joins fade-in tweens into the given sequence. Smoothly transitions alpha to 1f and/or scale to Vector3.one.
    /// </summary>
    public virtual void ApplyFadeIn(Sequence seq, GameObject prop, float duration)
    {
        if (seq == null || prop == null || duration <= 0.001f) return;

        if (enableAlphaFade)
        {
            seq.Join(DOTween.To(() => 0f, a => SetPropAlpha(prop, a), 1f, duration).SetEase(fadeInEase));
        }
        if (enableScaleFade)
        {
            seq.Join(prop.transform.DOScale(Vector3.one, duration).SetEase(fadeInEase));
        }
    }

    /// <summary>
    /// Joins fade-out tweens into the given sequence. Smoothly transitions alpha to 0f and/or scale to Vector3.zero.
    /// </summary>
    public virtual void ApplyFadeOut(Sequence seq, GameObject prop, float duration)
    {
        if (seq == null || prop == null || duration <= 0.001f) return;

        if (enableAlphaFade)
        {
            seq.Join(DOTween.To(() => 1f, a => SetPropAlpha(prop, a), 0f, duration).SetEase(fadeOutEase));
        }
        if (enableScaleFade)
        {
            seq.Join(prop.transform.DOScale(Vector3.zero, duration).SetEase(fadeOutEase));
        }
    }

    /// <summary>
    /// Restores canonical transform and visual properties when returning to Object Pool (dotween.md & shaders.md).
    /// </summary>
    public virtual void ResetPropVisualState(GameObject prop)
    {
        if (prop == null) return;
        SetPropAlpha(prop, 1f);
        prop.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// Builds and executes the complete DOTween sequence for this prop.
    /// Subclasses (e.g. SawBladeClearPropSO, AxeClearPropSO) implement their specialized visual choreographies.
    /// Adheres strictly to dotween.md: stateless SO, binds target and lifecycle link, handles pool return.
    /// </summary>
    public abstract Sequence AnimatePropSequence(
        GameObject prop,
        Vector3 startCellPos,
        Vector3 endCellPos,
        Vector3 dir,
        float sweepDuration,
        float leadOffset,
        IPoolService poolService);
}
