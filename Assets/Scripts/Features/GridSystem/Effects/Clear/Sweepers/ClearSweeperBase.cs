using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Abstract base ScriptableObject for line sweepers (Chop & Drag, Spin & Cut, Jump & Slam, etc.).
/// Encapsulates sweeper prefab reference, trajectory offsets, audio feedback, and stage director lifecycle.
/// Adheres strictly to dotween.md (Stateless SO) and visual_effects.md.
/// </summary>
public abstract class ClearSweeperBase : ScriptableObject
{
    [Header("Trajectory & Placement Settings")]
    [Tooltip("Distance along the line direction behind the start cut position where the sweeper swoops in from.")]
    public float spawnDistance = 1.0f;

    [Tooltip("Offset along the cutting line direction (positive = forward along line, negative = backward). Automatically adapts for horizontal rows and vertical columns.")]
    public float forwardOffset = 0f;

    [Tooltip("Cutting/contact height offset along the Y axis where the sweeper touches the blocks.")]
    public float heightOffset = 0.5f;

    [Tooltip("Vertical drop height above the cutting position before dropping down (e.g. +2.0 units).")]
    public float dropHeight = 2.0f;

    [Tooltip("Duration in seconds for the sweeper to drop down and fade in before cutting starts.")]
    public float entryDuration = 0.15f;

    [Tooltip("Duration in seconds for the sweeper to exit and fade out after cutting completes.")]
    public float exitDuration = 0.1f;

    [Header("Sweeper Fade Transitions")]
    [Tooltip("Controls whether alpha transparency fade is applied during entry/exit.")]
    public bool enableAlphaFade = true;

    [Tooltip("Controls whether scale transition is applied during entry/exit (vital for opaque materials or juicy pop-in/pop-out).")]
    public bool enableScaleFade = false;

    [Tooltip("Easing curve for fade-in transition.")]
    public Ease fadeInEase = Ease.Linear;

    [Tooltip("Easing curve for fade-out transition.")]
    public Ease fadeOutEase = Ease.InQuad;

    [Header("Sweeper Audio Feedback")]
    [Tooltip("Audio clip played when sweeper drops down/enters the board.")]
    public AudioClip entrySound;

    [Tooltip("Audio clip played when sweeper begins actively sweeping/cutting across the line.")]
    public AudioClip sweepSound;

    [Tooltip("Audio clip played when sweeper exits the board.")]
    public AudioClip exitSound;

    [Range(0f, 1f)]
    [Tooltip("Volume multiplier for sweeper audio clips.")]
    public float soundVolume = 1.0f;

    /// <summary>
    /// Total duration in seconds spent before the sweep movement begins.
    /// Used by staggers to synchronize the anticipation of the first exploding block.
    /// Defaults to entryDuration (for immediate slide-in sweepers like SpinCutSweeper).
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
    public static void SetSweeperAlpha(GameObject sweeper, float alpha)
    {
        if (sweeper == null) return;
        var renderers = sweeper.GetComponentsInChildren<Renderer>(true);
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
    /// Controls particle emission across all child ParticleSystems on the sweeper.
    /// Automatically orients any ParticlesPivot transform opposite to the cutting direction if provided.
    /// </summary>
    public static void SetSweeperParticles(GameObject sweeper, bool play, Vector3? sprayDir = null)
    {
        if (sweeper == null) return;

        if (sprayDir.HasValue && sprayDir.Value.sqrMagnitude > 0.001f)
        {
            Transform pivot = sweeper.transform.Find("ParticlesPivot") ?? sweeper.transform.Find("particlesPivot");
            if (pivot != null)
            {
                Vector3 spray = (-sprayDir.Value + Vector3.up * 0.4f).normalized;
                pivot.rotation = Quaternion.LookRotation(spray, Vector3.up);
            }
        }

        var particleSystems = sweeper.GetComponentsInChildren<ParticleSystem>(true);
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
    /// Initializes sweeper visuals for entry. Hides via alpha = 0 and/or scale = 0 based on configuration.
    /// </summary>
    public virtual void PrepareSweeperForEntry(GameObject sweeper)
    {
        if (sweeper == null) return;
        if (enableAlphaFade)
        {
            SetSweeperAlpha(sweeper, 0f);
        }
        if (enableScaleFade)
        {
            sweeper.transform.localScale = Vector3.zero;
        }
    }

    /// <summary>
    /// Joins fade-in tweens into the given sequence. Smoothly transitions alpha to 1f and/or scale to Vector3.one.
    /// </summary>
    public virtual void ApplyFadeIn(Sequence seq, GameObject sweeper, float duration)
    {
        if (seq == null || sweeper == null || duration <= 0.001f) return;

        if (enableAlphaFade)
        {
            seq.Join(DOTween.To(() => 0f, a => SetSweeperAlpha(sweeper, a), 1f, duration).SetEase(fadeInEase));
        }
        if (enableScaleFade)
        {
            seq.Join(sweeper.transform.DOScale(Vector3.one, duration).SetEase(fadeInEase));
        }
    }

    /// <summary>
    /// Joins fade-out tweens into the given sequence. Smoothly transitions alpha to 0f and/or scale to Vector3.zero.
    /// </summary>
    public virtual void ApplyFadeOut(Sequence seq, GameObject sweeper, float duration)
    {
        if (seq == null || sweeper == null || duration <= 0.001f) return;

        if (enableAlphaFade)
        {
            seq.Join(DOTween.To(() => 1f, a => SetSweeperAlpha(sweeper, a), 0f, duration).SetEase(fadeOutEase));
        }
        if (enableScaleFade)
        {
            seq.Join(sweeper.transform.DOScale(Vector3.zero, duration).SetEase(fadeOutEase));
        }
    }

    /// <summary>
    /// Restores canonical transform and visual properties when returning to Object Pool (dotween.md & shaders.md).
    /// </summary>
    public virtual void ResetSweeperVisualState(GameObject sweeper)
    {
        if (sweeper == null) return;
        SetSweeperAlpha(sweeper, 1f);
        sweeper.transform.localScale = Vector3.one;
    }

    /// <summary>
    /// Main entry point called by Stagger to orchestrate the sweeper's lifecycle and choreography.
    /// Spawns the sweeper prefab via poolService, executes AnimateSweeperSequence, and returns it to pool on complete.
    /// Adheres to Allocation Symmetry: the sweeper initiates its own spawn and guarantees its own pool return.
    /// </summary>
    public virtual Sequence Play(ClearTimelineContext timeline, IPoolService poolService)
    {
        if (poolService == null || timeline == null) return null;

        GameObject prefabToSpawn = timeline.overridePrefab;
        if (prefabToSpawn == null) return null;

        GameObject sweeper = poolService.SpawnObject(
            prefabToSpawn,
            timeline.startPos,
            Quaternion.identity,
            PoolType.GameObject);

        if (sweeper == null) return null;

        return AnimateSweeperSequence(sweeper, timeline, poolService);
    }

    /// <summary>
    /// Builds and executes the complete DOTween sequence for this sweeper motion profile.
    /// Subclasses (e.g. ChopDragSweeperSO, SpinCutSweeperSO) implement their specialized visual choreographies.
    /// Adheres strictly to dotween.md: stateless SO, binds target and lifecycle link, handles pool return.
    /// </summary>
    public abstract Sequence AnimateSweeperSequence(
        GameObject sweeper,
        ClearTimelineContext timeline,
        IPoolService poolService);

    /// <summary>
    /// Resolves the shared world-space exit target for sweepers and escaping visuals.
    /// By default, projects along the sweep line past the end cell.
    /// </summary>
    public virtual Vector3 ResolveVisualExitPosition(ClearTimelineContext timeline)
    {
        if (timeline == null) return Vector3.zero;
        Vector3 dir = timeline.direction;
        Vector3 forwardVec = dir * forwardOffset;
        Vector3 cutEndPos = timeline.endPos + dir * 0.5f + forwardVec + Vector3.up * heightOffset;
        return cutEndPos + dir * 1.0f;
    }

    /// <summary>
    /// Computes the exit destination outside the camera's viewport frustum, or falls back to distance along line.
    /// Reusable across any linear or projectile sweeper.
    /// </summary>
    public static Vector3 ComputeViewportExitPosition(
        ClearTimelineContext timeline,
        float forwardOffset,
        float heightOffset,
        bool exitOutsideViewport,
        float exitViewportPadding,
        float fallbackExitDistance)
    {
        if (timeline == null) return Vector3.zero;

        Vector3 dir = timeline.direction;
        Vector3 forwardVec = dir * forwardOffset;
        Vector3 cutEndPos = timeline.endPos + dir * 0.5f + forwardVec + Vector3.up * heightOffset;

        Camera cam = timeline.viewCamera;
        if (exitOutsideViewport && cam != null)
        {
            Vector3 p0 = cutEndPos;
            Vector3 p1 = p0 + dir;
            Vector3 vp0 = cam.WorldToViewportPoint(p0);
            Vector3 vp1 = cam.WorldToViewportPoint(p1);

            if (vp0.z > 0.01f && vp1.z > 0.01f)
            {
                Vector2 dir2D = new Vector2(vp1.x - vp0.x, vp1.y - vp0.y);
                if (dir2D.sqrMagnitude > 0.0001f)
                {
                    float minBound = -exitViewportPadding;
                    float maxBound = 1f + exitViewportPadding;

                    float tExit = float.MaxValue;
                    if (dir2D.x > 0.0001f) tExit = Mathf.Min(tExit, (maxBound - vp0.x) / dir2D.x);
                    else if (dir2D.x < -0.0001f) tExit = Mathf.Min(tExit, (minBound - vp0.x) / dir2D.x);

                    if (dir2D.y > 0.0001f) tExit = Mathf.Min(tExit, (maxBound - vp0.y) / dir2D.y);
                    else if (dir2D.y < -0.0001f) tExit = Mathf.Min(tExit, (minBound - vp0.y) / dir2D.y);

                    if (tExit > 0f && tExit < 100f)
                    {
                        Vector3 exitVp = new Vector3(vp0.x + dir2D.x * tExit, vp0.y + dir2D.y * tExit, vp0.z);
                        Vector3 worldExit = cam.ViewportToWorldPoint(exitVp);
                        worldExit.y = cutEndPos.y;
                        return worldExit;
                    }
                }
            }
        }

        return cutEndPos + dir * fallbackExitDistance;
    }
}
