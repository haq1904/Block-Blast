using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BlockSpawnGeneratorTest
{
    private ShapeDatabase testDatabase;
    private SpawnConfiguration testConfig;

    [SetUp]
    public void SetUp()
    {
        testDatabase = ScriptableObject.CreateInstance<ShapeDatabase>();
        testConfig = ScriptableObject.CreateInstance<SpawnConfiguration>();

        // Create dummy ShapeData for Tier 1 (1x1, 1x2)
        ShapeData t1ShapeA = ScriptableObject.CreateInstance<ShapeData>();
        t1ShapeA.tier = 1;
        t1ShapeA.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0) };

        ShapeData t1ShapeB = ScriptableObject.CreateInstance<ShapeData>();
        t1ShapeB.tier = 1;
        t1ShapeB.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0) };

        testDatabase.tier1Shapes.Add(t1ShapeA);
        testDatabase.tier1Shapes.Add(t1ShapeB);

        // Create dummy ShapeData for Tier 3 (3x3)
        ShapeData t3Shape = ScriptableObject.CreateInstance<ShapeData>();
        t3Shape.tier = 3;
        t3Shape.baseOffsets = new List<Vector2Int>();
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                t3Shape.baseOffsets.Add(new Vector2Int(x, y));
            }
        }
        testDatabase.tier3Shapes.Add(t3Shape);
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(testDatabase);
        UnityEngine.Object.DestroyImmediate(testConfig);
    }

    [Test]
    public void GenerateBatch_AlwaysReturnsThreeNonNullBlocks()
    {
        BlockModel[] batch = BlockSpawnGenerator.GenerateBatch(0, null, testDatabase, testConfig);

        Assert.IsNotNull(batch);
        Assert.AreEqual(3, batch.Length);
        Assert.IsNotNull(batch[0]);
        Assert.IsNotNull(batch[1]);
        Assert.IsNotNull(batch[2]);
    }

    [Test]
    public void GenerateBatch_RespectsMaxTier3Limit()
    {
        // Force Tier 3 weight to 100%
        testConfig.tier1WeightCurve = new AnimationCurve(new Keyframe(0, 0));
        testConfig.tier2WeightCurve = new AnimationCurve(new Keyframe(0, 0));
        testConfig.tier3WeightCurve = new AnimationCurve(new Keyframe(0, 10));
        testConfig.maxTier3Count = 1;

        for (int iteration = 0; iteration < 10; iteration++)
        {
            BlockModel[] batch = BlockSpawnGenerator.GenerateBatch(1000, null, testDatabase, testConfig);

            int tier3Detected = 0;
            foreach (var block in batch)
            {
                // 3x3 block has 9 tiles
                if (block.ShapeOffsets.Count == 9)
                {
                    tier3Detected++;
                }
            }

            Assert.LessOrEqual(tier3Detected, testConfig.maxTier3Count,
                $"Batch should have at most {testConfig.maxTier3Count} Tier 3 blocks.");
        }
    }

    [Test]
    public void BoardDensity_CrowdedBoard_IncreasesTier1Weight()
    {
        testConfig.enableBoardDensityAdjustment = true;
        testConfig.highDensityThreshold = 0.7f;

        // Normal density (0.2)
        testConfig.GetTierWeights(1000, 0.2f, out float normalW1, out _, out float normalW3);

        // High density (0.85)
        testConfig.GetTierWeights(1000, 0.85f, out float highW1, out _, out float highW3);

        Assert.Greater(highW1, normalW1, "Tier 1 weight should increase when board is crowded.");
        Assert.Less(highW3, normalW3, "Tier 3 weight should decrease when board is crowded.");
    }

    [Test]
    public void GenerateBatch_AppliesMercy_WhenGridCannotFitBatch()
    {
        testConfig.enableMercyMode = true;

        // Create a database with 2-tile shapes so normal rolls cannot fit the restricted grid
        ShapeDatabase mercyDb = ScriptableObject.CreateInstance<ShapeDatabase>();
        ShapeData bigShape = ScriptableObject.CreateInstance<ShapeData>();
        bigShape.tier = 1;
        bigShape.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0) };
        mercyDb.tier1Shapes.Add(bigShape);

        var fullGridMock = new MockRestrictedGrid();

        // 1. Without a 1x1 in database, mercy fallback generates a 1x1 (0,0) BlockModel
        BlockModel[] batchFallback = BlockSpawnGenerator.GenerateBatch(0, fullGridMock, mercyDb, testConfig);
        Assert.IsTrue(BlockSpawnGenerator.CanPlaceBlockAnywhere(batchFallback[2], fullGridMock),
            "Mercy fallback block must be placeable on the restricted grid.");

        // 2. When a 1x1 shape exists in Tier 1, mercy finds and uses it
        ShapeData singleTileShape = ScriptableObject.CreateInstance<ShapeData>();
        singleTileShape.tier = 1;
        singleTileShape.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0) };
        mercyDb.tier1Shapes.Add(singleTileShape);

        BlockModel[] batchWithMercy = BlockSpawnGenerator.GenerateBatch(0, fullGridMock, mercyDb, testConfig);
        Assert.IsTrue(BlockSpawnGenerator.CanPlaceBlockAnywhere(batchWithMercy[2], fullGridMock),
            "Mercy block found in database must be placeable on the restricted grid.");

        UnityEngine.Object.DestroyImmediate(mercyDb);
        UnityEngine.Object.DestroyImmediate(bigShape);
        UnityEngine.Object.DestroyImmediate(singleTileShape);
    }

    [Test]
    public void CountLinesClearedIfPlaced_ReturnsCorrectClearedCount()
    {
        var nearFullGrid = new MockNearFullRowGrid();
        BlockModel singleDot = new BlockModel(new List<(int x, int y)> { (0, 0) });

        // Placing at (7, 0) completes row 0 (which already has columns 0-6 filled)
        int clears = BlockSpawnGenerator.CountLinesClearedIfPlaced(singleDot, 7, 0, nearFullGrid);
        Assert.AreEqual(1, clears, "Placing dot in the missing gap of row 0 should clear exactly 1 line.");

        // Placing at (7, 1) does not complete any line
        int noClears = BlockSpawnGenerator.CountLinesClearedIfPlaced(singleDot, 7, 1, nearFullGrid);
        Assert.AreEqual(0, noClears, "Placing in an uncompleted row should clear 0 lines.");
    }

    [Test]
    public void GenerateBatch_UnderScoreThreshold_ProvidesLineClearingShape()
    {
        testConfig.comboPhaseScoreThreshold = 200000;
        testConfig.comboAssistanceRate = 1.0f; // 100% guarantee for test

        var nearFullGrid = new MockNearFullRowGrid();

        BlockModel[] batch = BlockSpawnGenerator.GenerateBatch(50000, nearFullGrid, testDatabase, testConfig);

        // Slot 0 should be the Key Shape that can clear a line on nearFullGrid
        bool canClear = false;
        for (int x = 0; x < nearFullGrid.GridWidth; x++)
        {
            for (int y = 0; y < nearFullGrid.GridHeight; y++)
            {
                if (BlockSpawnGenerator.CountLinesClearedIfPlaced(batch[0], x, y, nearFullGrid) > 0)
                {
                    canClear = true;
                    break;
                }
            }
            if (canClear) break;
        }

        Assert.IsTrue(canClear, "Under 200,000 threshold, batch[0] should provide a line-clearing key shape.");
    }

    private class MockNearFullRowGrid : IGridService
    {
        public int GridWidth => 8;
        public int GridHeight => 8;
        public int OccupiedCellCount => 7;
        public float OccupancyRatio => 7f / 64f;

        public event Action<bool, List<Vector2Int>> OnPreviewStateChanged;
        public event Action<List<Vector2Int>> OnBlockPlaced;
        public event Action<List<int>, List<int>> OnLinesCleared;

        public Vector2Int GetGridPositionFromWorld(Vector3 worldPos) => Vector2Int.zero;
        public Vector3 GetWorldPositionFromGrid(Vector2Int gridPos) => Vector3.zero;
        public void RequestPreview(List<Vector2Int> gridPositions) { }
        public void PlaceBlocks(List<Vector2Int> gridPositions) { }

        public bool CanPlaceBlocks(List<Vector2Int> gridPositions)
        {
            foreach (var pos in gridPositions)
            {
                if (pos.x < 0 || pos.x >= 8 || pos.y < 0 || pos.y >= 8) return false;
                if (IsCellOccupied(pos.x, pos.y)) return false;
            }
            return true;
        }

        public bool IsCellOccupied(int col, int row)
        {
            // Row 0 has columns 0 to 6 filled, column 7 is empty
            return row == 0 && col >= 0 && col < 7;
        }
    }



    private class MockRestrictedGrid : IGridService
    {
        public int GridWidth => 8;
        public int GridHeight => 8;
        public int OccupiedCellCount => 63;
        public float OccupancyRatio => 63f / 64f;

        public event Action<bool, List<Vector2Int>> OnPreviewStateChanged;
        public event Action<List<Vector2Int>> OnBlockPlaced;
        public event Action<List<int>, List<int>> OnLinesCleared;

        public Vector2Int GetGridPositionFromWorld(Vector3 worldPos) => Vector2Int.zero;
        public Vector3 GetWorldPositionFromGrid(Vector2Int gridPos) => Vector3.zero;
        public void RequestPreview(List<Vector2Int> gridPositions) { }
        public void PlaceBlocks(List<Vector2Int> gridPositions) { }

        public bool CanPlaceBlocks(List<Vector2Int> gridPositions)
        {
            // Only allows placement if block has exactly 1 tile at (0, 0)
            if (gridPositions != null && gridPositions.Count == 1 && gridPositions[0] == Vector2Int.zero)
            {
                return true;
            }
            return false;
        }

        public bool IsCellOccupied(int col, int row) => !(col == 0 && row == 0);
    }
}

