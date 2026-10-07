using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Abstract base ScriptableObject for Pet clear animations.
/// Resolves and coordinates the AnimationPivot rig separating root world motion from pivot presentation poses.
/// Strictly stateless in compliance with dotween.md.
/// </summary>
public abstract class PetClearAnimationSOBase : ClearAnimationSO
{
    protected const string PivotChildName = "AnimationPivot";

    /// <summary>
    /// Immutable rig descriptor resolved at the start of an animation.
    /// Never stored in ScriptableObject instance fields.
    /// </summary>
    protected readonly struct PetAnimationRig
    {
        public readonly Transform root;
        public readonly Transform pivot;
        public readonly Vector3 pivotRestPosition;
        public readonly Quaternion pivotRestRotation;
        public readonly Vector3 pivotRestScale;

        public PetAnimationRig(Transform root, Transform pivot, Vector3 restPosition, Quaternion restRotation, Vector3 restScale)
        {
            this.root = root;
            this.pivot = pivot;
            this.pivotRestPosition = restPosition;
            this.pivotRestRotation = restRotation;
            this.pivotRestScale = restScale;
        }

        public bool HasSeparatePivot => pivot != null && pivot != root;
    }

    /// <summary>
    /// Resolves the AnimationPivot transform on the target Pet root.
    /// In Editor / development, logs an assertion if the pivot child is missing.
    /// </summary>
    protected PetAnimationRig ResolveRig(Transform root)
    {
        if (root == null)
        {
            return new PetAnimationRig(null, null, Vector3.zero, Quaternion.identity, Vector3.one);
        }

        Transform pivot = root.Find(PivotChildName);
        if (pivot == null)
        {
            Debug.Assert(pivot != null, $"[PetClearAnimationSOBase] Missing direct '{PivotChildName}' child on '{root.name}'! Falling back to root.", root);
            return new PetAnimationRig(root, root, Vector3.zero, Quaternion.identity, Vector3.one);
        }

        return new PetAnimationRig(root, pivot, pivot.localPosition, pivot.localRotation, pivot.localScale);
    }

    /// <summary>
    /// Prepares the rig before launching new tweens: kills active tweens and initializes transforms.
    /// </summary>
    protected void PrepareRig(PetAnimationRig rig, Vector3 rootPos, Quaternion rootRot)
    {
        if (rig.root != null)
        {
            rig.root.DOKill();
            rig.root.position = rootPos;
            rig.root.rotation = rootRot;
            rig.root.localScale = Vector3.one;
        }

        if (rig.HasSeparatePivot)
        {
            rig.pivot.DOKill();
            rig.pivot.localPosition = rig.pivotRestPosition;
            rig.pivot.localRotation = rig.pivotRestRotation;
            rig.pivot.localScale = rig.pivotRestScale;
        }
    }

    /// <summary>
    /// Resets the rig to its canonical root transform and recorded pivot rest transform.
    /// </summary>
    protected void ResetRig(PetAnimationRig rig, Vector3 rootPos, Quaternion rootRot)
    {
        if (rig.root != null)
        {
            rig.root.position = rootPos;
            rig.root.rotation = rootRot;
            rig.root.localScale = Vector3.one;
        }

        if (rig.HasSeparatePivot)
        {
            rig.pivot.localPosition = rig.pivotRestPosition;
            rig.pivot.localRotation = rig.pivotRestRotation;
            rig.pivot.localScale = rig.pivotRestScale;
        }
    }

    /// <summary>
    /// Safety cancellation resetting both root and pivot to their canonical states.
    /// </summary>
    public override void Cancel(Transform target, Vector3 canonicalPos, Quaternion canonicalRot)
    {
        if (target == null) return;

        PetAnimationRig rig = ResolveRig(target);
        if (rig.root != null) rig.root.DOKill();
        if (rig.HasSeparatePivot) rig.pivot.DOKill();

        ResetRig(rig, canonicalPos, canonicalRot);
        base.Cancel(target, canonicalPos, canonicalRot);
    }

    /// <summary>
    /// Normalizes and projects the sweep direction onto the XZ horizontal plane.
    /// </summary>
    protected static Vector3 ResolveDirection(Vector3 direction)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
    }

    /// <summary>
    /// Computes the lean rotation for the pivot in root-local space around the perpendicular axis.
    /// </summary>
    protected static Quaternion CalculatePivotLeanRotation(Transform root, Quaternion pivotRestRotation, Vector3 worldDirection, float angleDegrees)
    {
        if (root == null || Mathf.Abs(angleDegrees) < 0.001f) return pivotRestRotation;
        Vector3 localDir = root.InverseTransformDirection(ResolveDirection(worldDirection));
        Vector3 localLeanAxis = Vector3.Cross(Vector3.up, localDir).normalized;
        if (localLeanAxis.sqrMagnitude < 0.001f) return pivotRestRotation;
        return pivotRestRotation * Quaternion.AngleAxis(angleDegrees, localLeanAxis);
    }
}
