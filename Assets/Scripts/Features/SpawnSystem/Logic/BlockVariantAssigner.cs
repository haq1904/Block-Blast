using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static class BlockVariantAssigner
{
    /// <summary>
    /// Assigns theme variants to all blocks in a spawn batch.
    /// If monochrome is enabled or there is only 1 variant, all cells share a single variant.
    /// If mixed, exactly 2 variants are chosen and partitioned into contiguous connected clusters.
    /// </summary>
    public static void AssignThemeVariants(BlockModel[] batch, BlockTypeSO theme)
    {
        if (batch == null || theme == null || theme.variants == null || theme.variants.Length == 0) return;

        int totalVariants = theme.variants.Length;

        for (int b = 0; b < batch.Length; b++)
        {
            if (batch[b] == null) continue;

            int cellCount = batch[b].ShapeOffsets.Count;
            int[] variants = new int[cellCount];

            if (theme.isMonochromePerShape || totalVariants <= 1 || cellCount <= 1)
            {
                int singleId = theme.variants[Random.Range(0, totalVariants)].variantId;
                for (int i = 0; i < cellCount; i++) variants[i] = singleId;
            }
            else
            {
                int idxA = Random.Range(0, totalVariants);
                int idxB = (idxA + Random.Range(1, totalVariants)) % totalVariants;
                int idA = theme.variants[idxA].variantId;
                int idB = theme.variants[idxB].variantId;

                variants = GenerateClusteredVariants(batch[b].ShapeOffsets, idA, idB);
            }

            int[] rotationsY = new int[cellCount];
            if (theme.randomCellRotationY)
            {
                for (int i = 0; i < cellCount; i++)
                {
                    rotationsY[i] = Random.Range(0, 4) * 90;
                }
            }

            batch[b] = new BlockModel(batch[b].ShapeOffsets, theme.typeId, variants, rotationsY);
        }
    }

    /// <summary>
    /// Partitions shape cells into two contiguous connected clusters for variant idA and idB.
    /// Guarantees that all cells of idA are connected to each other, and all cells of idB are connected to each other.
    /// </summary>
    public static int[] GenerateClusteredVariants(IReadOnlyList<(int x, int y)> offsets, int idA, int idB)
    {
        int n = offsets != null ? offsets.Count : 0;
        if (n == 0) return Array.Empty<int>();
        if (n == 1) return new int[] { idA };

        int minA = Mathf.Max(1, n / 3);
        int maxA = Mathf.Min(n - 1, (2 * n + 2) / 3);
        int targetK = Random.Range(minA, maxA + 1);

        HashSet<int> clusterA = PartitionIntoConnectedClusters(offsets, targetK);
        int[] result = new int[n];
        for (int i = 0; i < n; i++)
        {
            result[i] = clusterA.Contains(i) ? idA : idB;
        }

        return result;
    }

    private static HashSet<int> PartitionIntoConnectedClusters(IReadOnlyList<(int x, int y)> offsets, int targetK)
    {
        int n = offsets.Count;

        // Tier 1: Try Directional Sweeps (Left-Right, Top-Down, Diagonals)
        Func<(int x, int y), float>[] projections = new Func<(int x, int y), float>[]
        {
            p => p.x,
            p => -p.x,
            p => p.y,
            p => -p.y,
            p => p.x + p.y,
            p => -(p.x + p.y),
            p => p.x - p.y,
            p => -(p.x - p.y)
        };

        List<int> directionOrder = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };
        Shuffle(directionOrder);

        for (int d = 0; d < directionOrder.Count; d++)
        {
            var proj = projections[directionOrder[d]];
            List<int> sorted = new List<int>(n);
            for (int i = 0; i < n; i++) sorted.Add(i);
            sorted.Sort((a, b) => proj(offsets[a]).CompareTo(proj(offsets[b])));

            HashSet<int> candidateA = new HashSet<int>();
            for (int i = 0; i < targetK; i++) candidateA.Add(sorted[i]);

            HashSet<int> candidateB = new HashSet<int>();
            for (int i = targetK; i < n; i++) candidateB.Add(sorted[i]);

            if (IsConnected(offsets, candidateA) && IsConnected(offsets, candidateB))
            {
                return candidateA;
            }
        }

        // Tier 2: BFS Seed Expansion from Extreme/Corner Cells
        List<int> seedCandidates = new List<int>(n);
        for (int i = 0; i < n; i++) seedCandidates.Add(i);
        Shuffle(seedCandidates);

        for (int s = 0; s < seedCandidates.Count; s++)
        {
            HashSet<int> candidateA = ExpandBfsCluster(offsets, seedCandidates[s], targetK);
            if (candidateA.Count == targetK)
            {
                HashSet<int> candidateB = new HashSet<int>();
                for (int i = 0; i < n; i++)
                {
                    if (!candidateA.Contains(i)) candidateB.Add(i);
                }

                if (IsConnected(offsets, candidateA) && IsConnected(offsets, candidateB))
                {
                    return candidateA;
                }
            }
        }

        // Tier 3: Guaranteed Fallback (Non-cut vertex: 1 cell for A, n-1 cells for B)
        for (int i = 0; i < n; i++)
        {
            HashSet<int> candidateB = new HashSet<int>();
            for (int j = 0; j < n; j++)
            {
                if (j != i) candidateB.Add(j);
            }

            if (IsConnected(offsets, candidateB))
            {
                return new HashSet<int> { i };
            }
        }

        // Ultimate safety fallback
        return new HashSet<int> { 0 };
    }

    private static HashSet<int> ExpandBfsCluster(IReadOnlyList<(int x, int y)> offsets, int seed, int targetSize)
    {
        var cluster = new HashSet<int>();
        var queue = new Queue<int>();

        cluster.Add(seed);
        queue.Enqueue(seed);

        while (queue.Count > 0 && cluster.Count < targetSize)
        {
            int curr = queue.Dequeue();
            var currPos = offsets[curr];

            for (int i = 0; i < offsets.Count; i++)
            {
                if (cluster.Contains(i)) continue;

                var neighborPos = offsets[i];
                int dist = Mathf.Abs(currPos.x - neighborPos.x) + Mathf.Abs(currPos.y - neighborPos.y);
                if (dist == 1)
                {
                    cluster.Add(i);
                    queue.Enqueue(i);
                    if (cluster.Count >= targetSize) break;
                }
            }
        }

        return cluster;
    }

    /// <summary>
    /// Verifies if a subset of cells forms a contiguous 4-way connected component.
    /// </summary>
    public static bool IsConnected(IReadOnlyList<(int x, int y)> offsets, HashSet<int> subset)
    {
        if (subset == null || subset.Count == 0) return false;
        if (subset.Count == 1) return true;

        int start = -1;
        foreach (int idx in subset)
        {
            start = idx;
            break;
        }

        var visited = new HashSet<int>();
        var queue = new Queue<int>();
        visited.Add(start);
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            int curr = queue.Dequeue();
            var currPos = offsets[curr];

            foreach (int neighbor in subset)
            {
                if (visited.Contains(neighbor)) continue;

                var neighborPos = offsets[neighbor];
                int dist = Mathf.Abs(currPos.x - neighborPos.x) + Mathf.Abs(currPos.y - neighborPos.y);
                if (dist == 1)
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        return visited.Count == subset.Count;
    }

    private static void Shuffle<T>(IList<T> list)
    {
        int count = list.Count;
        for (int i = count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
