using System;
using UnityEngine;

/// <summary>
/// Pure C# presentation math helper to compute timing stagger delays for line clear waves.
/// Stateless, thread-safe, and zero GC allocation.
/// </summary>
public static class ClearStaggerCalculator
{
    public static float CalculateDelay(
        ClearStaggerPattern pattern,
        int indexInLine,
        int totalInLine,
        float stepDelay,
        Vector3 cellWorldPos = default,
        Vector3 placementOrigin = default)
    {
        if (totalInLine <= 1 || stepDelay <= 0f) return 0f;

        switch (pattern)
        {
            case ClearStaggerPattern.CenterOutward:
                {
                    // Distance from line midpoint: index 3 & 4 have dist 0, 2 & 5 have dist 1, etc.
                    float center = (totalInLine - 1) * 0.5f;
                    int dist = Mathf.RoundToInt(Mathf.Abs(indexInLine - center) - 0.5f);
                    if (dist < 0) dist = 0;
                    return dist * stepDelay;
                }

            case ClearStaggerPattern.EdgesInward:
                {
                    // Opposite of CenterOutward: edges (0 & 7) pop first, center pops last
                    int edgeDist = Mathf.Min(indexInLine, totalInLine - 1 - indexInLine);
                    int maxDist = (totalInLine / 2) - 1;
                    int inwardStep = maxDist - edgeDist;
                    if (inwardStep < 0) inwardStep = 0;
                    return inwardStep * stepDelay;
                }

            case ClearStaggerPattern.SequentialForward:
                return indexInLine * stepDelay;

            case ClearStaggerPattern.SequentialBackward:
                return (totalInLine - 1 - indexInLine) * stepDelay;

            case ClearStaggerPattern.FromPlacementOrigin:
                {
                    float distance = Vector3.Distance(cellWorldPos, placementOrigin);
                    return distance * (stepDelay * 0.8f);
                }

            case ClearStaggerPattern.CheckerboardZipper:
                // Even cells pop immediately (0s), odd cells pop after a delay
                return (indexInLine % 2 == 0) ? 0f : (stepDelay * 1.5f);

            case ClearStaggerPattern.RandomShuffled:
                {
                    // Deterministic pseudo-random shuffle index using bit-mixer to avoid GC alloc
                    int pseudoHash = (indexInLine * 7 + 3) % totalInLine;
                    return pseudoHash * stepDelay;
                }

            case ClearStaggerPattern.InstantAll:
            default:
                return 0f;
        }
    }
}
