using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Linear forward domino cascade from lowest coordinate index (0) to highest (N-1).
/// Orchestrates line sweeping props (e.g. SawBlade) synchronized with sequential cell explosions.
/// </summary>
[CreateAssetMenu(fileName = "SequentialForwardStagger", menuName = "Block Blast/Effects/Clear Stagger/Sequential Forward")]
public class SequentialForwardStaggerSO : ClearStaggerSO
{
    [Header("Prop Orchestration Settings")]
    [Tooltip("Cutting height offset along the Y axis where the prop touches or cuts the blocks.")]
    public float propHeightOffset = 0.5f;

    [Tooltip("Vertical drop height above the cutting position before dropping down (e.g. +2.0 units).")]
    public float propDropHeight = 2.0f;

    [Tooltip("Duration in seconds for the prop to drop down and fade in before cutting starts.")]
    public float propEntryDuration = 0.15f;

    [Tooltip("Duration in seconds for the prop to exit and fade out after cutting completes.")]
    public float propExitDuration = 0.1f;

    [Tooltip("Manual override for block explosion tween duration in seconds. If <= 0, automatically uses clearAnimation.PreExplosionDuration.")]
    public float overrideBlockTweenDuration = 0f;

    public override float CalculateDelay(
        int indexInLine,
        int totalInLine,
        Vector3 cellWorldPos,
        Vector3 placementOrigin)
    {
        if (totalInLine <= 1 || stepDelay <= 0f) return 0f;
        return indexInLine * stepDelay;
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

        bool hasProp = clearAnimation != null && clearAnimation.linePropPrefab != null;

        float leadOffset = 0f;
        float firstBlockDelay = 0f;

        if (hasProp)
        {
            // Time for prop to reach the center of cell 0:
            // propEntryDuration (swoop down to outer edge) + 0.5f * stepInterval (travel 0.5 cell from edge to center)
            float timeToFirstCenter = propEntryDuration + 0.5f * stepInterval;

            // In order for cell 0 to have enough time (tweenDuration) to prepare its squash/tremor before prop hits:
            leadOffset = Mathf.Max(0f, tweenDuration - timeToFirstCenter);
            firstBlockDelay = (timeToFirstCenter + leadOffset) - tweenDuration;
        }

        // If the theme animation has a prop (e.g. SawBlade for Wood), orchestrate the prop swoop and line sweep
        if (hasProp && poolService != null && lineCells.Count >= 2)
        {
            PlayPropSequence(lineCells, clearAnimation.linePropPrefab, poolService, sweepDuration, leadOffset);
        }

        for (int i = 0; i < lineCells.Count; i++)
        {
            var cell = lineCells[i];
            if (cell.gameObject == null) continue;

            float delay = hasProp
                ? (firstBlockDelay + cell.indexInLine * stepInterval)
                : (cell.indexInLine * stepInterval);

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
        GameObject propPrefab,
        IPoolService poolService,
        float sweepDuration,
        float leadOffset)
    {
        Vector3 firstPos = lineCells[0].canonicalPos;
        Vector3 lastPos = lineCells[lineCells.Count - 1].canonicalPos;
        Vector3 dir = (lastPos - firstPos).normalized;

        float height = propHeightOffset;
        float dropHeight = propDropHeight;
        float entryDuration = propEntryDuration;

        Vector3 spawnPos = firstPos - dir * 1.0f + Vector3.up * (height + dropHeight);
        Vector3 cutStartPos = firstPos - dir * 0.5f + Vector3.up * height;
        Vector3 cutEndPos = lastPos + dir * 0.5f + Vector3.up * height;
        Vector3 exitPos = cutEndPos + dir * 1.0f;

        // Prop orientation: blade disc stands upright along cutting direction
        Quaternion propRot = Mathf.Abs(dir.z) > Mathf.Abs(dir.x)
            ? Quaternion.Euler(90f, 90f, 0f)
            : Quaternion.Euler(90f, 0f, 0f);

        GameObject prop = poolService.SpawnObject(
            propPrefab,
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

        // Phase 1: Slide in from Y + dropHeight & Fade in
        seq.Append(prop.transform.DOMove(cutStartPos, entryDuration).SetEase(Ease.OutQuad));
        if (spinner != null)
        {
            seq.Join(DOTween.To(() => 0f, a => spinner.SetAlpha(a), 1f, entryDuration));
        }

        // Phase 2: Linear sweep across the line with sparks and mechanical micro-shake
        seq.AppendCallback(() => spinner?.StartCuttingFeedback(dir));
        seq.Append(prop.transform.DOMove(cutEndPos, sweepDuration).SetEase(Ease.Linear));

        // Phase 3: Exit & Fade out (terminate feedback, particles finish naturally)
        seq.AppendCallback(() => spinner?.StopCuttingFeedback());
        seq.Append(prop.transform.DOMove(exitPos, propExitDuration).SetEase(Ease.InQuad));
        if (spinner != null)
        {
            seq.Join(DOTween.To(() => 1f, a => spinner.SetAlpha(a), 0f, propExitDuration));
        }

        seq.OnComplete(() =>
        {
            spinner?.SetAlpha(1f);
            poolService.ReturnObjectToPool(prop);
        });
    }
}
