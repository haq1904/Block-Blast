using UnityEngine;

/// <summary>
/// Radial shockwave ripple propagating outward based on world distance from placed trigger block.
/// </summary>
[CreateAssetMenu(fileName = "FromPlacementOriginStagger", menuName = "Block Blast/Effects/Clear Stagger/From Placement Origin")]
public class FromPlacementOriginStaggerSO : ClearStaggerSO
{
    public override float CalculateDelay(
        int indexInLine,
        int totalInLine,
        Vector3 cellWorldPos,
        Vector3 placementOrigin)
    {
        if (stepDelay <= 0f) return 0f;

        float distance = Vector3.Distance(cellWorldPos, placementOrigin);
        return distance * (stepDelay * 0.8f);
    }
}
