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

    [Header("Block Explosion Override")]
    [Tooltip("Manual override for block explosion tween duration in seconds. If <= 0, automatically uses clearAnimation.PreExplosionDuration.")]
    public float overrideBlockTweenDuration = 0f;

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
    /// Executes the complete clear wave for a line of cells with optional sweeper and prop overrides.
    /// Orchestrates sweepers (via ClearTimelineContext), step delays, cell animations, and particle VFX.
    /// Supports decoupled lifecycle: onCellGameplayReleased makes cells placeable immediately, while
    /// onCellVisualCompleted finishes visual exit/cleanup and returns objects to pool.
    /// </summary>
    public virtual void Play(
        List<ClearCellItem> lineCells,
        ClearAnimationSO clearAnimation,
        ClearSweeperBase sweeperOverride,
        GameObject propPrefabOverride,
        IPoolService poolService,
        Action<Vector2Int> onCellGameplayReleased,
        Action<Vector2Int> onCellVisualCompleted = null,
        Camera viewCamera = null)
    {
        if (lineCells == null || lineCells.Count == 0) return;

        int count = lineCells.Count;
        List<CellWaypoint> waypoints = new List<CellWaypoint>(count);
        float minHitTime = float.MaxValue;
        float maxHitTime = float.MinValue;
        Vector3 startPos = lineCells[0].canonicalPos;
        Vector3 endPos = lineCells[count - 1].canonicalPos;

        for (int i = 0; i < count; i++)
        {
            var cell = lineCells[i];
            float delay = CalculateDelay(cell.indexInLine, cell.totalInLine, cell.canonicalPos, Vector3.zero);
            waypoints.Add(new CellWaypoint(cell.gridPos, cell.canonicalPos, delay, cell.indexInLine, cell.totalInLine));

            if (delay <= minHitTime)
            {
                minHitTime = delay;
                startPos = cell.canonicalPos;
            }
            if (delay >= maxHitTime)
            {
                maxHitTime = delay;
                endPos = cell.canonicalPos;
            }
        }

        Vector3 dir = endPos - startPos;
        if (dir.sqrMagnitude > 0.001f)
        {
            dir = dir.normalized;
        }
        else if (count >= 2)
        {
            dir = (lineCells[count - 1].canonicalPos - lineCells[0].canonicalPos).normalized;
        }
        else
        {
            dir = Vector3.forward;
        }

        float totalSweepDuration = Mathf.Max(0.1f, (maxHitTime - minHitTime) + Mathf.Max(stepDelay, 0.05f));

        ClearSweeperBase sweeperData = sweeperOverride;
        GameObject activePrefab = propPrefabOverride;

        bool hasSweeper = sweeperData != null && activePrefab != null && poolService != null && count >= 2;

        float contactOffset = clearAnimation != null
            ? Mathf.Max(0f, clearAnimation.SweepContactOffset)
            : 0f;

        float postContactDelay = clearAnimation != null
            ? Mathf.Max(0f, clearAnimation.SweepPostContactDelay)
            : 0f;

        float leadOffset = 0f;
        float firstBlockDelay = 0f;
        Vector3 resolvedExit = endPos + dir * 1.5f;
        ClearTimelineContext timeline = null;

        if (hasSweeper)
        {
            float preSweepDuration = sweeperData.TotalPreSweepDuration;
            float timeToFirstCenter = preSweepDuration + 0.5f * stepDelay;
            float firstAnimationStart = timeToFirstCenter - contactOffset + postContactDelay;

            leadOffset = Mathf.Max(0f, -firstAnimationStart);
            firstBlockDelay = Mathf.Max(0f, firstAnimationStart);

            timeline = new ClearTimelineContext
            {
                waypoints = waypoints,
                startPos = startPos,
                endPos = endPos,
                direction = dir,
                totalSweepDuration = totalSweepDuration,
                leadOffset = leadOffset,
                firstBlockDelay = firstBlockDelay,
                overridePrefab = activePrefab,
                viewCamera = viewCamera
            };

            timeline.visualExitPosition = sweeperData.ResolveVisualExitPosition(timeline);
            resolvedExit = timeline.visualExitPosition;

            sweeperData.Play(timeline, poolService);
        }

        for (int i = 0; i < count; i++)
        {
            var cell = lineCells[i];
            if (cell.gameObject == null) continue;

            float calculatedDelay = waypoints[i].hitTime;
            float delay = hasSweeper ? (firstBlockDelay + calculatedDelay) : calculatedDelay;

            bool gameplayReleased = false;
            bool visualCompleted = false;

            Action releaseGameplayOnce = () =>
            {
                if (gameplayReleased) return;
                gameplayReleased = true;

                Vector3 burstPos = cell.gameObject != null ? cell.transform.position : cell.canonicalPos;
                Quaternion burstRot = cell.gameObject != null ? cell.transform.rotation : Quaternion.identity;

                Play(burstPos, burstRot, poolService);
                clearAnimation?.PlayExplosionSound();
                onCellGameplayReleased?.Invoke(cell.gridPos);
            };

            Action completeVisualOnce = () =>
            {
                if (visualCompleted) return;
                visualCompleted = true;

                if (!gameplayReleased)
                {
                    releaseGameplayOnce();
                }

                if (cell.gameObject != null)
                {
                    cell.transform.DOKill();
                    cell.transform.position = cell.canonicalPos;
                    cell.transform.rotation = Quaternion.identity;
                    cell.transform.localScale = Vector3.one;

                    poolService?.ReturnObjectToPool(cell.gameObject);
                }

                onCellVisualCompleted?.Invoke(cell.gridPos);
            };

            if (clearAnimation != null)
            {
                ClearCellContext context = new ClearCellContext
                {
                    gridPos = cell.gridPos,
                    canonicalPos = cell.canonicalPos,
                    canonicalRotation = cell.canonicalRotation,
                    indexInLine = cell.indexInLine,
                    totalInLine = cell.totalInLine,
                    delay = delay,
                    placementOrigin = Vector3.zero,
                    sweepDirection = dir,
                    visualExitPosition = resolvedExit,
                    followTarget = timeline?.activeSweeperTransform,
                    sweeperCompletionTime = timeline != null ? timeline.visualCompletionTime : 0f
                };

                ClearAnimationLifecycle lifecycle = new ClearAnimationLifecycle(
                    releaseGameplayOnce,
                    completeVisualOnce);

                clearAnimation.PlayWithLifecycle(cell.transform, context, lifecycle);
            }
            else
            {
                releaseGameplayOnce();
                completeVisualOnce();
            }
        }
    }

    /// <summary>
    /// Backwards-compatibility overload without split callbacks and viewCamera.
    /// </summary>
    public virtual void Play(
        List<ClearCellItem> lineCells,
        ClearAnimationSO clearAnimation,
        ClearSweeperBase sweeperOverride,
        GameObject propPrefabOverride,
        IPoolService poolService,
        Action<Vector2Int> onCellExploded)
    {
        Play(lineCells, clearAnimation, sweeperOverride, propPrefabOverride, poolService, onCellExploded, null, null);
    }

    /// <summary>
    /// Backwards-compatibility overload without sweeper and prop overrides.
    /// </summary>
    public virtual void Play(
        List<ClearCellItem> lineCells,
        ClearAnimationSO clearAnimation,
        IPoolService poolService,
        Action<Vector2Int> onCellExploded)
    {
        Play(lineCells, clearAnimation, null, null, poolService, onCellExploded);
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
