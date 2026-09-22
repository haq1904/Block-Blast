using UnityEngine;

/// <summary>
/// Dual inward compression wave: outer border cells trigger first, closing towards center.
/// </summary>
[CreateAssetMenu(fileName = "EdgesInwardStagger", menuName = "Block Blast/Effects/Clear Stagger/Edges Inward")]
public class EdgesInwardStaggerSO : ClearStaggerSO
{
    public override float CalculateDelay(
        int indexInLine,
        int totalInLine,
        Vector3 cellWorldPos,
        Vector3 placementOrigin)
    {
        if (totalInLine <= 1 || stepDelay <= 0f) return 0f;

        int edgeDist = Mathf.Min(indexInLine, totalInLine - 1 - indexInLine);
        return edgeDist * stepDelay;
    }
}
