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
/// Context payload delivered to each individual cell when triggering its clear animation.
/// </summary>
public struct ClearCellContext
{
    public Vector2Int gridPos;
    public Vector3 canonicalPos;
    public int indexInLine;           // 0 to 7 along the cleared row or column
    public int totalInLine;           // Total cells in this line (typically 8)
    public float delay;               // Calculated stagger delay in seconds
    public Vector3 placementOrigin;   // World position of the trigger block that caused the clear
}
