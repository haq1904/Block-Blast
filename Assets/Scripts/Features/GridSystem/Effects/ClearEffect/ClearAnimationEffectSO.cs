using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Abstract base ScriptableObject for block destruction animations.
/// Encapsulates rhythm/stagger settings and delegates specific mesh morphing tweens to subclasses.
/// Adheres strictly to dotween.md: completely stateless with zero runtime Tween/Transform fields.
/// </summary>
public abstract class ClearAnimationEffectSO : ScriptableObject
{
    [Header("Rhythm & Stagger Settings")]
    [Tooltip("How clear stagger wave rhythm is determined.")]
    public ClearStaggerSelectionMode staggerSelectionMode = ClearStaggerSelectionMode.Specific;

    [Tooltip("Specific pattern used when SelectionMode is Specific.")]
    public ClearStaggerPattern staggerPattern = ClearStaggerPattern.CenterOutward;

    [Tooltip("Pool of patterns to randomly choose from when SelectionMode is RandomFromPool.")]
    public ClearStaggerPattern[] randomPool = new ClearStaggerPattern[]
    {
        ClearStaggerPattern.CenterOutward,
        ClearStaggerPattern.EdgesInward,
        ClearStaggerPattern.SequentialForward,
        ClearStaggerPattern.SequentialBackward,
        ClearStaggerPattern.FromPlacementOrigin,
        ClearStaggerPattern.CheckerboardZipper,
        ClearStaggerPattern.RandomShuffled
    };

    [Tooltip("Time delay interval between successive steps in seconds.")]
    [Range(0.01f, 0.08f)]
    public float stepDelay = 0.03f;

    /// <summary>
    /// Resolves the active stagger pattern according to configured selection mode.
    /// </summary>
    public virtual ClearStaggerPattern ResolvePattern()
    {
        if (staggerSelectionMode == ClearStaggerSelectionMode.RandomFromPool && randomPool != null && randomPool.Length > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, randomPool.Length);
            return randomPool[randomIndex];
        }
        return staggerPattern;
    }

    /// <summary>
    /// Computes the stagger delay for an individual cell.
    /// </summary>
    public virtual float CalculateDelay(
        ClearStaggerPattern pattern,
        int indexInLine,
        int totalInLine,
        Vector3 cellWorldPos,
        Vector3 placementOrigin)
    {
        return ClearStaggerCalculator.CalculateDelay(
            pattern,
            indexInLine,
            totalInLine,
            stepDelay,
            cellWorldPos,
            placementOrigin);
    }

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
}
