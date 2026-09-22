using UnityEngine;

/// <summary>
/// Linear forward domino cascade from lowest coordinate index (0) to highest (N-1).
/// </summary>
[CreateAssetMenu(fileName = "SequentialForwardStagger", menuName = "Block Blast/Effects/Clear Stagger/Sequential Forward")]
public class SequentialForwardStaggerSO : ClearStaggerSO
{
    public override float CalculateDelay(
        int indexInLine,
        int totalInLine,
        Vector3 cellWorldPos,
        Vector3 placementOrigin)
    {
        if (totalInLine <= 1 || stepDelay <= 0f) return 0f;
        return indexInLine * stepDelay;
    }
}
