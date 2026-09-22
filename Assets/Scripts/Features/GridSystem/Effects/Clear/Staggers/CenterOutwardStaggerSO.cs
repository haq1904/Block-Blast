using UnityEngine;

/// <summary>
/// Symmetric outward ripple starting simultaneously from line midpoint towards both extremities.
/// </summary>
[CreateAssetMenu(fileName = "CenterOutwardStagger", menuName = "Block Blast/Effects/Clear Stagger/Center Outward")]
public class CenterOutwardStaggerSO : ClearStaggerSO
{
    public override float CalculateDelay(
        int indexInLine,
        int totalInLine,
        Vector3 cellWorldPos,
        Vector3 placementOrigin)
    {
        if (totalInLine <= 1 || stepDelay <= 0f) return 0f;

        float center = (totalInLine - 1) * 0.5f;
        int dist = Mathf.RoundToInt(Mathf.Abs(indexInLine - center) - 0.5f);
        if (dist < 0) dist = 0;
        return dist * stepDelay;
    }
}
