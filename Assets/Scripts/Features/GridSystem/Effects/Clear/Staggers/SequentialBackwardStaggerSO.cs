using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Reverse domino cascade starting from highest coordinate index (N-1) down to 0.
/// Orchestrates line sweeping props (e.g. SawBlade) synchronized with reverse sequential cell explosions.
/// </summary>
[CreateAssetMenu(fileName = "SequentialBackwardStagger", menuName = "Block Blast/Effects/Clear Stagger/Sequential Backward")]
public class SequentialBackwardStaggerSO : ClearStaggerSO
{
    [Header("Block Explosion Override")]
    [Tooltip("Manual override for block explosion tween duration in seconds. If <= 0, automatically uses clearAnimation.PreExplosionDuration.")]
    public float overrideBlockTweenDuration = 0f;

    public override float CalculateDelay(
        int indexInLine,
        int totalInLine,
        Vector3 cellWorldPos,
        Vector3 placementOrigin)
    {
        if (totalInLine <= 1 || stepDelay <= 0f) return 0f;
        return (totalInLine - 1 - indexInLine) * stepDelay;
    }

    public override void Play(
        List<ClearCellItem> lineCells,
        ClearAnimationSO clearAnimation,
        IPoolService poolService,
        Action<Vector2Int> onCellExploded)
    {
        if (lineCells == null || lineCells.Count == 0) return;

        // Retrieve pre-explosion tween duration directly from clearAnimation (or manual override)
        float tweenDuration = overrideBlockTweenDuration > 0f
            ? overrideBlockTweenDuration
            : (clearAnimation != null ? clearAnimation.PreExplosionDuration : 0f);

        // Step interval between successive block explosions is purely stepDelay
        float stepInterval = stepDelay;
        // Sweep duration for prop to travel across all N cells (from outer start edge to outer end edge, distance = N)
        float sweepDuration = lineCells.Count * stepInterval;

        ClearPropBase propData = clearAnimation != null ? clearAnimation.GetProp() : null;
        bool hasProp = propData != null && propData.propPrefab != null;

        float leadOffset = 0f;
        float firstBlockDelay = 0f;

        if (hasProp)
        {
            float preSweepDuration = propData.TotalPreSweepDuration;

            // Time for prop to reach the center of the first cut cell (N - 1):
            // preSweepDuration (swoop down / windup / chop to outer edge) + 0.5f * stepInterval (travel 0.5 cell from edge to center)
            float timeToFirstCenter = preSweepDuration + 0.5f * stepInterval;

            // In order for cell (N - 1) to have enough time (tweenDuration) to prepare its squash/tremor before prop hits:
            leadOffset = Mathf.Max(0f, tweenDuration - timeToFirstCenter);
            firstBlockDelay = (timeToFirstCenter + leadOffset) - tweenDuration;
        }

        // If the theme animation has a prop (e.g. SawBlade or Axe for Wood), orchestrate the reverse prop swoop and line sweep
        if (hasProp && poolService != null && lineCells.Count >= 2)
        {
            Vector3 firstPos = lineCells[lineCells.Count - 1].canonicalPos;
            Vector3 lastPos = lineCells[0].canonicalPos;
            Vector3 dir = (lastPos - firstPos).normalized;

            GameObject prop = poolService.SpawnObject(
                propData.propPrefab,
                firstPos,
                Quaternion.identity,
                PoolType.GameObject);

            if (prop != null)
            {
                propData.AnimatePropSequence(
                    prop,
                    firstPos,
                    lastPos,
                    dir,
                    sweepDuration,
                    leadOffset,
                    poolService);
            }
        }

        for (int i = 0; i < lineCells.Count; i++)
        {
            var cell = lineCells[i];
            if (cell.gameObject == null) continue;

            int reverseIndex = cell.totalInLine - 1 - cell.indexInLine;
            float delay = hasProp
                ? (firstBlockDelay + reverseIndex * stepInterval)
                : (reverseIndex * stepInterval);

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
}
