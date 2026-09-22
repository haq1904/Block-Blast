using UnityEngine;

/// <summary>
/// Alternating zipper rhythm: even indices clear immediately, odd indices follow.
/// </summary>
[CreateAssetMenu(fileName = "CheckerboardZipperStagger", menuName = "Block Blast/Effects/Clear Stagger/Checkerboard Zipper")]
public class CheckerboardZipperStaggerSO : ClearStaggerSO
{
    public override float CalculateDelay(
        int indexInLine,
        int totalInLine,
        Vector3 cellWorldPos,
        Vector3 placementOrigin)
    {
        if (stepDelay <= 0f) return 0f;
        return (indexInLine % 2 == 0) ? 0f : (stepDelay * 1.5f);
    }
}
