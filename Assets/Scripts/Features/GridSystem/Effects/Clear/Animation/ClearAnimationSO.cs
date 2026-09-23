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

    [Header("VFX Layer 2: Shockwave & Dynamics")]
    [Tooltip("Layer 2: Shockwave, dynamic ripples, sparks, or launch dust particle prefab.")]
    public GameObject layer2_DynamicsPrefab;

    [Header("VFX Layer 3: Debris & Shards")]
    [Tooltip("Layer 3: Material debris, shards, or lingering particle residue.")]
    public GameObject layer3_DebrisPrefab;

    [Header("Base Shader Feedback")]
    [ColorUsage(true, true)]
    [Tooltip("Base HDR flash color applied during burst or anticipation.")]
    public Color flashColor = Color.white;

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

        // Layer 1: Primary Burst (e.g. Bomb Blast / Shockwave)
        if (useLayer1_Burst && layer1_BurstPrefab != null)
        {
            pool.SpawnObject(layer1_BurstPrefab, spawnPos, spawnRot, PoolType.ParticleSystem);
        }

        // Layer 2: Dynamics / Smoke (e.g. Bomb Smoke Cloud / Puff)
        if (useLayer2_Dynamics && layer2_DynamicsPrefab != null)
        {
            pool.SpawnObject(layer2_DynamicsPrefab, spawnPos, spawnRot, PoolType.ParticleSystem);
        }

        // Layer 3: Debris / Shards (uses the dedicated debris material and mesh configured on the prefab/theme)
        if (useLayer3_Debris && layer3_DebrisPrefab != null)
        {
            GameObject debrisObj = pool.SpawnObject(layer3_DebrisPrefab, spawnPos, spawnRot, PoolType.ParticleSystem);
            if (debrisObj != null)
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
}
