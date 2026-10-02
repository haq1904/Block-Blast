using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BlockVariantAssignerTest
{
    private BlockTypeSO CreateTestTheme(bool isMonochrome, int variantCount)
    {
        var theme = ScriptableObject.CreateInstance<BlockTypeSO>();
        theme.typeId = "test_theme";
        theme.isMonochromePerShape = isMonochrome;
        theme.variants = new BlockVariantData[variantCount];
        for (int i = 0; i < variantCount; i++)
        {
            theme.variants[i] = new BlockVariantData { variantId = 100 + i };
        }
        return theme;
    }

    [Test]
    public void AssignThemeVariants_Monochrome_AllCellsSameVariant()
    {
        var theme = CreateTestTheme(isMonochrome: true, variantCount: 4);
        var offsets = new List<(int x, int y)> { (0, 0), (1, 0), (0, 1), (1, 1) };
        var batch = new BlockModel[] { new BlockModel(offsets, theme.typeId) };

        BlockVariantAssigner.AssignThemeVariants(batch, theme);

        int first = batch[0].VariantIds[0];
        for (int i = 1; i < batch[0].VariantIds.Length; i++)
        {
            Assert.AreEqual(first, batch[0].VariantIds[i], "All cells in a monochrome block must share the identical variantId.");
        }
    }

    [Test]
    public void AssignThemeVariants_SingleVariantTheme_AllCellsSameVariant()
    {
        var theme = CreateTestTheme(isMonochrome: false, variantCount: 1);
        var offsets = new List<(int x, int y)> { (0, 0), (1, 0), (2, 0) };
        var batch = new BlockModel[] { new BlockModel(offsets, theme.typeId) };

        BlockVariantAssigner.AssignThemeVariants(batch, theme);

        for (int i = 0; i < batch[0].VariantIds.Length; i++)
        {
            Assert.AreEqual(100, batch[0].VariantIds[i], "With only 1 theme variant, all cells must receive that variantId.");
        }
    }

    [Test]
    public void AssignThemeVariants_SingleCellBlock_ValidVariant()
    {
        var theme = CreateTestTheme(isMonochrome: false, variantCount: 3);
        var offsets = new List<(int x, int y)> { (0, 0) };
        var batch = new BlockModel[] { new BlockModel(offsets, theme.typeId) };

        BlockVariantAssigner.AssignThemeVariants(batch, theme);

        Assert.AreEqual(1, batch[0].VariantIds.Length);
        Assert.IsTrue(batch[0].VariantIds[0] >= 100 && batch[0].VariantIds[0] <= 102);
    }

    [Test]
    public void GenerateClusteredVariants_Square2x2_BothClustersConnected()
    {
        var offsets = new List<(int x, int y)> { (0, 0), (1, 0), (0, 1), (1, 1) };
        AssertClusteringForAllIterations(offsets, 50);
    }

    [Test]
    public void GenerateClusteredVariants_Square3x3_BothClustersConnected()
    {
        var offsets = new List<(int x, int y)>();
        for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
                offsets.Add((x, y));

        AssertClusteringForAllIterations(offsets, 50);
    }

    [Test]
    public void GenerateClusteredVariants_Line1x4_BothClustersConnected()
    {
        var offsets = new List<(int x, int y)> { (0, 0), (0, 1), (0, 2), (0, 3) };
        AssertClusteringForAllIterations(offsets, 50);
    }

    [Test]
    public void GenerateClusteredVariants_LShape_BothClustersConnected()
    {
        var offsets = new List<(int x, int y)> { (0, 0), (0, 1), (0, 2), (1, 0) };
        AssertClusteringForAllIterations(offsets, 50);
    }

    [Test]
    public void GenerateClusteredVariants_TShape_BothClustersConnected()
    {
        var offsets = new List<(int x, int y)> { (0, 1), (1, 1), (2, 1), (1, 0) };
        AssertClusteringForAllIterations(offsets, 50);
    }

    [Test]
    public void GenerateClusteredVariants_PlusShape_BothClustersConnected()
    {
        var offsets = new List<(int x, int y)> { (1, 1), (0, 1), (2, 1), (1, 0), (1, 2) };
        AssertClusteringForAllIterations(offsets, 50);
    }

    [Test]
    public void GenerateClusteredVariants_UShape_BothClustersConnected()
    {
        var offsets = new List<(int x, int y)> { (0, 1), (0, 0), (1, 0), (2, 0), (2, 1) };
        AssertClusteringForAllIterations(offsets, 50);
    }

    [Test]
    public void GenerateClusteredVariants_ZShape_BothClustersConnected()
    {
        var offsets = new List<(int x, int y)> { (0, 1), (1, 1), (1, 0), (2, 0) };
        AssertClusteringForAllIterations(offsets, 50);
    }

    private void AssertClusteringForAllIterations(List<(int x, int y)> offsets, int iterations)
    {
        int idA = 1;
        int idB = 2;

        for (int iter = 0; iter < iterations; iter++)
        {
            int[] variants = BlockVariantAssigner.GenerateClusteredVariants(offsets, idA, idB);
            Assert.AreEqual(offsets.Count, variants.Length);

            var clusterA = new HashSet<int>();
            var clusterB = new HashSet<int>();

            for (int i = 0; i < variants.Length; i++)
            {
                if (variants[i] == idA) clusterA.Add(i);
                else if (variants[i] == idB) clusterB.Add(i);
                else Assert.Fail($"Unexpected variantId: {variants[i]}");
            }

            Assert.IsTrue(clusterA.Count > 0, "Cluster A must contain at least 1 cell.");
            Assert.IsTrue(clusterB.Count > 0, "Cluster B must contain at least 1 cell.");

            Assert.IsTrue(BlockVariantAssigner.IsConnected(offsets, clusterA),
                $"Cluster A must be a contiguous connected component. Iteration {iter}");
            Assert.IsTrue(BlockVariantAssigner.IsConnected(offsets, clusterB),
                $"Cluster B must be a contiguous connected component. Iteration {iter}");
        }
    }
}
