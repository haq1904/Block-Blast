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

        bool hasProp = clearAnimation != null && clearAnimation.HasProp;
        ClearPropBase propData = hasProp ? clearAnimation.lineProp : null;

        float leadOffset = 0f;
        float firstBlockDelay = 0f;

        if (hasProp)
        {
            float entryDuration = propData.entryDuration;

            // Time for prop to reach the center of the first cut cell (N - 1):
            // entryDuration (swoop down to outer edge) + 0.5f * stepInterval (travel 0.5 cell from edge to center)
            float timeToFirstCenter = entryDuration + 0.5f * stepInterval;

            // In order for cell (N - 1) to have enough time (tweenDuration) to prepare its squash/tremor before prop hits:
            leadOffset = Mathf.Max(0f, tweenDuration - timeToFirstCenter);
            firstBlockDelay = (timeToFirstCenter + leadOffset) - tweenDuration;
        }

        // If the theme animation has a prop (e.g. SawBlade for Wood), orchestrate the reverse prop swoop and line sweep
        if (hasProp && poolService != null && lineCells.Count >= 2)
        {
            PlayPropSequence(lineCells, propData, poolService, sweepDuration, leadOffset);
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

    private void PlayPropSequence(
        List<ClearCellItem> lineCells,
        ClearPropBase propData,
        IPoolService poolService,
        float sweepDuration,
        float leadOffset)
    {
        if (propData == null || propData.propPrefab == null) return;

        // Reverse sweep: from last cell (N-1) down to first cell (0)
        Vector3 firstPos = lineCells[lineCells.Count - 1].canonicalPos;
        Vector3 lastPos = lineCells[0].canonicalPos;
        Vector3 dir = (lastPos - firstPos).normalized;

        float height = propData.heightOffset;
        float dropHeight = propData.dropHeight;
        float entryDuration = propData.entryDuration;
        float exitDuration = propData.exitDuration;

        Vector3 spawnPos = firstPos - dir * 1.0f + Vector3.up * (height + dropHeight);
        Vector3 cutStartPos = firstPos - dir * 0.5f + Vector3.up * height;
        Vector3 cutEndPos = lastPos + dir * 0.5f + Vector3.up * height;
        Vector3 exitPos = cutEndPos + dir * 1.0f;

        // Prop orientation: blade disc stands upright along cutting direction
        Quaternion propRot = Mathf.Abs(dir.z) > Mathf.Abs(dir.x)
            ? Quaternion.Euler(90f, 90f, 0f)
            : Quaternion.Euler(90f, 0f, 0f);

        GameObject prop = poolService.SpawnObject(
            propData.propPrefab,
            spawnPos,
            propRot,
            PoolType.GameObject);

        if (prop == null) return;

        SawBladeSpinner spinner = prop.GetComponent<SawBladeSpinner>();
        spinner?.SetAlpha(0f);

        Sequence seq = DOTween.Sequence();
        seq.SetTarget(prop);
        seq.SetLink(prop, LinkBehaviour.KillOnDisable);

        if (leadOffset > 0f)
        {
            seq.AppendInterval(leadOffset);
        }

        // Phase 1: Slide in from Y + dropHeight & Fade in onto cell N-1
        seq.AppendCallback(() => propData.PlayEntrySound());
        seq.Append(prop.transform.DOMove(cutStartPos, entryDuration).SetEase(Ease.OutQuad));
        if (spinner != null)
        {
            seq.Join(DOTween.To(() => 0f, a => spinner.SetAlpha(a), 1f, entryDuration));
        }

        // Phase 2: Linear sweep backwards across the line with sparks, mechanical micro-shake, and sweep sound
        seq.AppendCallback(() =>
        {
            spinner?.StartCuttingFeedback(dir);
            propData.PlaySweepSound();
        });
        seq.Append(prop.transform.DOMove(cutEndPos, sweepDuration).SetEase(Ease.Linear));

        // Phase 3: Exit & Fade out past cell 0 (terminate feedback, play exit sound, particles finish naturally)
        seq.AppendCallback(() =>
        {
            spinner?.StopCuttingFeedback();
            propData.PlayExitSound();
        });
        seq.Append(prop.transform.DOMove(exitPos, exitDuration).SetEase(Ease.InQuad));
        if (spinner != null)
        {
            seq.Join(DOTween.To(() => 1f, a => spinner.SetAlpha(a), 0f, exitDuration));
        }

        seq.OnComplete(() =>
        {
            spinner?.SetAlpha(1f);
            poolService.ReturnObjectToPool(prop);
        });
    }
}
