using UnityEngine;

/// <summary>
/// Abstract base ScriptableObject encapsulating clear wave propagation rhythm, calculation, and multi-VFX execution.
/// Follows the Strategy Pattern to eliminate switch-cases and static calculation helpers.
/// Adheres strictly to dotween.md: completely stateless with zero runtime Tween/Transform fields.
/// </summary>
public abstract class ClearStaggerSO : ScriptableObject
{
    [Header("Stagger Wave Timing")]
    [Tooltip("Time delay interval between successive steps in seconds.")]
    [Range(0.01f, 1.0f)]
    public float stepDelay = 0.8f;

    [Header("VFX & Particle Dispatch")]
    [Tooltip("Particle systems or visual game objects spawned/triggered for this stagger step.")]
    public GameObject[] stepParticlePrefabs;

    /// <summary>
    /// Computes the stagger delay for an individual cell according to this stagger's specific algorithm.
    /// </summary>
    public abstract float CalculateDelay(
        int indexInLine,
        int totalInLine,
        Vector3 cellWorldPos,
        Vector3 placementOrigin);

    /// <summary>
    /// Executes step-specific particle spawning or custom visual trigger for an individual cell.
    /// </summary>
    public virtual void Play(
        Vector3 cellWorldPos,
        Quaternion cellRotation,
        IPoolService poolService)
    {
        if (stepParticlePrefabs == null || poolService == null) return;

        for (int i = 0; i < stepParticlePrefabs.Length; i++)
        {
            if (stepParticlePrefabs[i] != null)
            {
                poolService.SpawnObject(
                    stepParticlePrefabs[i],
                    cellWorldPos,
                    cellRotation,
                    PoolType.ParticleSystem);
            }
        }
    }
}
