using System;
using System.Collections.Generic;
using DG.Tweening;
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
    /// Executes the complete clear wave for a line of cells.
    /// Orchestrates props (if supported by theme/stagger), step delays, cell animations, and particle VFX.
    /// Default implementation iterates line cells and triggers clear animation with calculated delays.
    /// </summary>
    public virtual void Play(
        List<ClearCellItem> lineCells,
        ClearAnimationSO clearAnimation,
        IPoolService poolService,
        Action<Vector2Int> onCellExploded)
    {
        if (lineCells == null || lineCells.Count == 0) return;

        for (int i = 0; i < lineCells.Count; i++)
        {
            var cell = lineCells[i];
            if (cell.gameObject == null) continue;

            float delay = CalculateDelay(cell.indexInLine, cell.totalInLine, cell.canonicalPos, Vector3.zero);

            if (clearAnimation != null)
            {
                ClearCellContext context = new ClearCellContext
                {
                    gridPos = cell.gridPos,
                    canonicalPos = cell.canonicalPos,
                    indexInLine = cell.indexInLine,
                    totalInLine = cell.totalInLine,
                    delay = delay,
                    placementOrigin = Vector3.zero
                };

                clearAnimation.Play(cell.transform, context, onExplode: () =>
                {
                    Vector3 burstPos = cell.gameObject != null ? cell.transform.position : cell.canonicalPos;
                    Quaternion burstRot = cell.gameObject != null ? cell.transform.rotation : Quaternion.identity;

                    Play(burstPos, burstRot, poolService);
                    clearAnimation.PlayExplosionSound();

                    if (cell.gameObject != null)
                    {
                        cell.transform.DOKill();
                        cell.transform.position = cell.canonicalPos;
                        cell.transform.rotation = Quaternion.identity;
                        cell.transform.localScale = Vector3.one;

                        poolService?.ReturnObjectToPool(cell.gameObject);
                    }

                    onCellExploded?.Invoke(cell.gridPos);
                });
            }
            else
            {
                if (cell.gameObject != null)
                {
                    cell.transform.DOKill();
                    cell.transform.position = cell.canonicalPos;
                    cell.transform.rotation = Quaternion.identity;
                    cell.transform.localScale = Vector3.one;
                    poolService?.ReturnObjectToPool(cell.gameObject);
                }
                onCellExploded?.Invoke(cell.gridPos);
            }
        }
    }

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
