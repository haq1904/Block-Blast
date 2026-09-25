using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Abstract base ScriptableObject for block destruction animations.
/// Encapsulates cell mesh morphing tweens and 3-tier VFX particle layers with shader feedback.
/// Adheres strictly to dotween.md: completely stateless with zero runtime Tween/Transform fields.
/// </summary>
public abstract class ClearAnimationSO : ScriptableObject
{
    [Header("Timing / Duration")]
    [Tooltip("Total duration of pre-explosion animation in seconds (before onExplode climax is invoked).")]
    public virtual float PreExplosionDuration => 0.15f;

    [Header("Props (Theme Line Sweepers)")]
    [Tooltip("Theme-specific line sweeper prop configuration asset.")]
    public ClearPropBase lineProp;

    public bool HasProp => lineProp != null && lineProp.propPrefab != null;

    [Header("Layer Activation Toggles")]
    [Tooltip("Enable / disable Layer 1: Primary Burst particle effect.")]
    public bool useLayer1_Burst = true;

    [Tooltip("Enable / disable Layer 2: Shockwave / Dynamics particle effect.")]
    public bool useLayer2_Dynamics = true;

    [Tooltip("Enable / disable Layer 3: Debris / Shards particle effect.")]
    public bool useLayer3_Debris = true;

    [Tooltip("Enable / disable Shader Feedback (surface flash, emission, rim, dissolve).")]
    public bool useShaderFeedback = true;

    [Header("VFX Layer 1: Primary Burst")]
    [Tooltip("Layer 1: Main explosion / burst particle prefab spawned at climax (e.g. Cartoon Poof, Explosion burst).")]
    public GameObject layer1_BurstPrefab;

    [Tooltip("Enable / disable random scale variation for Layer 1.")]
    public bool layer1_RandomizeScale = true;

    [Tooltip("Random scale / footprint range (min, max). Set (1, 1) for fixed size.")]
    public Vector2 layer1_ScaleRange = new Vector2(0.9f, 1.15f);

    [Tooltip("Enable / disable random particle count override for Layer 1.")]
    public bool layer1_RandomizeCount = false;

    [Tooltip("Random particle count range (min, max). Set (0, 0) to use prefab defaults.")]
    public Vector2Int layer1_CountRange = new Vector2Int(5, 10);

    [Header("VFX Layer 2: Shockwave & Dynamics")]
    [Tooltip("Layer 2: Shockwave, dynamic ripples, sparks, or launch dust particle prefab.")]
    public GameObject layer2_DynamicsPrefab;

    [Tooltip("Enable / disable random scale variation for Layer 2.")]
    public bool layer2_RandomizeScale = true;

    [Tooltip("Random scale / footprint range (min, max). Set (1, 1) for fixed size.")]
    public Vector2 layer2_ScaleRange = new Vector2(0.9f, 1.15f);

    [Tooltip("Enable / disable random particle count override for Layer 2.")]
    public bool layer2_RandomizeCount = false;

    [Tooltip("Random particle count range (min, max). Set (0, 0) to use prefab defaults.")]
    public Vector2Int layer2_CountRange = new Vector2Int(3, 6);

    [Header("VFX Layer 3: Debris & Shards")]
    [Tooltip("Layer 3: Material debris, shards, or lingering particle residue.")]
    public GameObject layer3_DebrisPrefab;

    [Tooltip("Enable / disable random scale variation for Layer 3.")]
    public bool layer3_RandomizeScale = true;

    [Tooltip("Random scale / footprint range (min, max). Set (1, 1) for fixed size.")]
    public Vector2 layer3_ScaleRange = new Vector2(0.85f, 1.25f);

    [Tooltip("Enable / disable random particle count override for Layer 3.")]
    public bool layer3_RandomizeCount = true;

    [Tooltip("Random particle count range (min, max). Set (0, 0) to use prefab defaults.")]
    public Vector2Int layer3_CountRange = new Vector2Int(8, 14);

    [Header("VFX Global Random Options")]
    [Tooltip("Randomize rotation around Y axis (360 degrees) for varied spray directions.")]
    public bool randomYawRotation = true;

    [Header("Base Shader Feedback")]
    [ColorUsage(true, true)]
    [Tooltip("Base HDR flash color applied during burst or anticipation.")]
    public Color flashColor = Color.white;

    [Tooltip("If true, automatically synchronizes flashColor to child Flash particles in spawned VFX.")]
    public bool syncVfxFlashColor = true;

    /// <summary>
    /// Executes the clear tween animation on the target block transform.
    /// Subclasses must call onExplode when anticipation reaches its climax so particles can burst.
    /// </summary>
    /// <param name="target">The block cell transform being animated.</param>
    /// <param name="context">Timing and position context.</param>
    /// <param name="onExplode">Callback triggered at the moment of burst to spawn particles and return block to pool.</param>
    public abstract void Play(
        Transform target,
        ClearCellContext context,
        Action onExplode);

    /// <summary>
    /// Safety cancellation resetting the transform back to canonical state if disabled mid-animation.
    /// </summary>
    public virtual void Cancel(Transform target, Vector3 canonicalPos, Quaternion canonicalRot)
    {
        if (target == null) return;
        target.DOKill();
        target.position = canonicalPos;
        target.rotation = canonicalRot;
        target.localScale = Vector3.one;
    }

    /// <summary>
    /// Applies random scale variation and optional burst particle count overrides to a spawned particle object.
    /// Preserves canonical safety since AutoReturnToPool automatically restores transform.localScale on pool return.
    /// </summary>
    protected virtual void ApplyParticleModifiers(
        GameObject spawnedObj,
        bool randomizeScale,
        Vector2 scaleRange,
        bool randomizeCount,
        Vector2Int countRange)
    {
        if (spawnedObj == null) return;

        if (randomizeScale && scaleRange.x > 0f && scaleRange.y >= scaleRange.x)
        {
            float factor = UnityEngine.Random.Range(scaleRange.x, scaleRange.y);
            spawnedObj.transform.localScale = Vector3.one * factor;
        }

        if (randomizeCount && countRange.x > 0 && countRange.y >= countRange.x)
        {
            var ps = spawnedObj.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                var em = ps.emission;
                if (em.burstCount > 0)
                {
                    var burst = em.GetBurst(0);
                    burst.minCount = (short)countRange.x;
                    burst.maxCount = (short)countRange.y;
                    em.SetBurst(0, burst);
                }
                ps.Clear();
                ps.Play();
            }
        }
    }

    /// <summary>
    /// Synchronizes the configured flash color to any child Flash particle systems on the spawned VFX object.
    /// </summary>
    protected virtual void ApplyFlashColorToVFX(GameObject vfxObj, Color color)
    {
        if (vfxObj == null) return;
        var allPS = vfxObj.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < allPS.Length; i++)
        {
            if (allPS[i].gameObject.name.IndexOf("Flash", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var main = allPS[i].main;
                main.startColor = color;
            }
        }
    }

    /// <summary>
    /// Spawns the enabled particle layers for this clear animation via IPoolService.
    /// Uses the 3-layer VFX configuration (Burst, Smoke Dynamics, Debris) matching the theme.
    /// </summary>
    public virtual void SpawnVFX(
        Transform target, 
        Vector3 fallbackPosition, 
        Quaternion fallbackRotation, 
        IPoolService poolService = null)
    {
        IPoolService pool = poolService ?? ServiceLocator.Get<IPoolService>();
        if (pool == null) return;

        Vector3 spawnPos = target != null ? target.position : fallbackPosition;
        Quaternion spawnRot = target != null ? target.rotation : fallbackRotation;

        if (randomYawRotation)
        {
            spawnRot *= Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
        }

        // Layer 1: Primary Burst (e.g. Bomb Blast / Shockwave)
        if (useLayer1_Burst && layer1_BurstPrefab != null)
        {
            GameObject burstObj = pool.SpawnObject(layer1_BurstPrefab, spawnPos, spawnRot, PoolType.ParticleSystem);
            ApplyParticleModifiers(burstObj, layer1_RandomizeScale, layer1_ScaleRange, layer1_RandomizeCount, layer1_CountRange);

            if (syncVfxFlashColor && burstObj != null)
            {
                ApplyFlashColorToVFX(burstObj, flashColor);
            }
        }

        // Layer 2: Dynamics / Smoke (e.g. Bomb Smoke Cloud / Puff)
        if (useLayer2_Dynamics && layer2_DynamicsPrefab != null)
        {
            GameObject dynObj = pool.SpawnObject(layer2_DynamicsPrefab, spawnPos, spawnRot, PoolType.ParticleSystem);
            ApplyParticleModifiers(dynObj, layer2_RandomizeScale, layer2_ScaleRange, layer2_RandomizeCount, layer2_CountRange);
        }

        // Layer 3: Debris / Shards (uses the dedicated debris material and mesh configured on the prefab/theme)
        if (useLayer3_Debris && layer3_DebrisPrefab != null)
        {
            GameObject debrisObj = pool.SpawnObject(layer3_DebrisPrefab, spawnPos, spawnRot, PoolType.ParticleSystem);
            ApplyParticleModifiers(debrisObj, layer3_RandomizeScale, layer3_ScaleRange, layer3_RandomizeCount, layer3_CountRange);

            if (!layer3_RandomizeCount && debrisObj != null)
            {
                var ps = debrisObj.GetComponentInChildren<ParticleSystem>();
                if (ps != null)
                {
                    ps.Clear();
                    ps.Play();
                }
            }
        }
    }

    /// <summary>
    /// Overload for backwards compatibility when target Transform is not available.
    /// </summary>
    public virtual void SpawnVFX(Vector3 position, Quaternion rotation, IPoolService poolService = null)
    {
        SpawnVFX(null, position, rotation, poolService);
    }

    /// <summary>
    /// Plays the cell explosion sound configured on the current block type via ISoundFXService.
    /// </summary>
    public virtual void PlayExplosionSound()
    {
        if (ServiceLocator.TryGet<IBlockService>(out var blockService))
        {
            var theme = blockService.CurrentBlockType;
            if (theme != null && ServiceLocator.TryGet<ISoundFXService>(out var soundService))
            {
                if (theme.explosionSound != null)
                {
                    soundService.PlaySound(theme.explosionSound, theme.explosionSoundVolume);
                }
                else
                {
                    soundService.PlaySound(theme.explosionSoundType, theme.explosionSoundVolume);
                }
            }
        }
    }
}
