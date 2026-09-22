using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Abstract base ScriptableObject for block destruction animations.
/// Encapsulates cell mesh morphing tweens (squash, wobble, twist, swell) and delegates propagation rhythm to ClearStaggerSO.
/// Adheres strictly to dotween.md: completely stateless with zero runtime Tween/Transform fields.
/// </summary>
public abstract class ClearAnimationSO : ScriptableObject
{
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
