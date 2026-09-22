using UnityEngine;

/// <summary>
/// Synchronous clear where all cells across the line burst simultaneously with zero delay.
/// </summary>
[CreateAssetMenu(fileName = "InstantAllStagger", menuName = "Block Blast/Effects/Clear Stagger/Instant All")]
public class InstantAllStaggerSO : ClearStaggerSO
{
    public override float CalculateDelay(
        int indexInLine,
        int totalInLine,
        Vector3 cellWorldPos,
        Vector3 placementOrigin)
    {
        return 0f;
    }
}
