using UnityEngine;

/// <summary>
/// Defines the geometric propagation wave pattern when clearing a line of cells.
/// </summary>
public enum ClearStaggerPattern
{
    CenterOutward,          // From line center outward to both edges (symmetric, cinematic)
    EdgesInward,            // From outer edges compressing into center
    SequentialForward,      // Left-to-right or bottom-to-top domino sequence
    SequentialBackward,     // Right-to-left or top-to-bottom sequence
    FromPlacementOrigin,    // Shockwave ripple radiating from placed block position
    CheckerboardZipper,     // Alternating zipper: even indices first, odd indices next
    RandomShuffled,         // Shuffled random pop order across cells (chaotic arcade)
    InstantAll              // All cells pop simultaneously at the exact same frame
}

/// <summary>
/// Determines how the stagger pattern is chosen for each line clear event.
/// </summary>
public enum ClearStaggerSelectionMode
{
    Specific,       // Uses the exact pattern configured in the Inspector
    RandomFromPool  // Randomly picks a pattern from randomPool on each line clear
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
