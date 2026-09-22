using UnityEngine;

/// <summary>
/// Reverse domino cascade starting from highest coordinate index (N-1) down to 0.
/// </summary>
[CreateAssetMenu(fileName = "SequentialBackwardStagger", menuName = "Block Blast/Effects/Clear Stagger/Sequential Backward")]
public class SequentialBackwardStaggerSO : ClearStaggerSO
{
    public override float CalculateDelay(
        int indexInLine,
        int totalInLine,
        Vector3 cellWorldPos,
        Vector3 placementOrigin)
    {
        if (totalInLine <= 1 || stepDelay <= 0f) return 0f;
        return (totalInLine - 1 - indexInLine) * stepDelay;
    }
}
