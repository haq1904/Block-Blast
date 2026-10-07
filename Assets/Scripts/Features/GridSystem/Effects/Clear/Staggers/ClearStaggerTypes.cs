using System;
using UnityEngine;

/// <summary>
/// Determines how the clear stagger rhythm is selected for each line clear event.
/// </summary>
public enum ClearStaggerSelectionMode
{
    Single,         // Uses single clearStagger
    RandomFromList  // Randomly picks from clearStaggerPool
}

/// <summary>
/// Payload decoupling gameplay release (making the cell placeable immediately)
/// from visual completion (finishing exit/fade animations before returning the object to pool).
/// </summary>
public readonly struct ClearAnimationLifecycle
{
    public readonly Action onGameplayRelease;
    public readonly Action onVisualComplete;

    public ClearAnimationLifecycle(Action onGameplayRelease, Action onVisualComplete)
    {
        this.onGameplayRelease = onGameplayRelease;
        this.onVisualComplete = onVisualComplete;
    }

    public void ReleaseGameplay() => onGameplayRelease?.Invoke();
    public void CompleteVisual() => onVisualComplete?.Invoke();
    public void ReleaseAndComplete()
    {
        ReleaseGameplay();
        CompleteVisual();
    }
}

/// <summary>
/// Context payload delivered to each individual cell when triggering its clear animation.
/// </summary>
public struct ClearCellContext
{
    public Vector2Int gridPos;
    public Vector3 canonicalPos;
    public Quaternion canonicalRotation;
    public int indexInLine;           // 0 to 7 along the cleared row or column
    public int totalInLine;           // Total cells in this line (typically 8)
    public float delay;               // Calculated stagger delay in seconds
    public Vector3 placementOrigin;   // World position of the trigger block that caused the clear
    public Vector3 sweepDirection;    // Normalized world-space direction followed by the active line sweeper
    public Vector3 visualExitPosition; // Shared world-space destination outside the viewport for exiting props/visuals
    public Transform followTarget;     // Live transform of active sweeper prop (if available)
    public float sweeperCompletionTime; // Timestamp when active sweeper completes its visual cycle
}

