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

        // Create dummy ShapeData (1x1, 1x2)
        ShapeData t1ShapeA = ScriptableObject.CreateInstance<ShapeData>();
        t1ShapeA.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0) };

        ShapeData t1ShapeB = ScriptableObject.CreateInstance<ShapeData>();
        t1ShapeB.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0) };

        testDatabase.shapes.Add(t1ShapeA);
        testDatabase.shapes.Add(t1ShapeB);

        // Create dummy ShapeData (3x3)
        ShapeData t3Shape = ScriptableObject.CreateInstance<ShapeData>();
        t3Shape.baseOffsets = new List<Vector2Int>();
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                t3Shape.baseOffsets.Add(new Vector2Int(x, y));
            }
        }
        testDatabase.shapes.Add(t3Shape);
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
    public void GenerateBatch_IntelligentScanner_SuppressesBulkyPieces()
    {
        for (int iteration = 0; iteration < 10; iteration++)
        {
            BlockModel[] batch = BlockSpawnGenerator.GenerateBatch(1000, null, testDatabase, testConfig);

            int bulkyDetected = 0;
            foreach (var block in batch)
            {
                // 3x3 block has 9 tiles
                if (block.ShapeOffsets.Count == 9)
                {
                    bulkyDetected++;
                }
            }

            Assert.AreEqual(0, bulkyDetected, "Scanner should suppress bulky 3x3 shapes on neutral grid.");
        }
    }

    [Test]
    public void GenerateBatch_AppliesMercy_WhenGridCannotFitBatch()
    {
        testConfig.enableMercyMode = true;
        testConfig.comboAssistanceRate = 0f; // Disable combo assistance to isolate Mercy test
        testConfig.enableSynergisticBatches = false;

        ShapeDatabase mercyDb = ScriptableObject.CreateInstance<ShapeDatabase>();
        ShapeData bigShape = ScriptableObject.CreateInstance<ShapeData>();
        bigShape.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0) };
        mercyDb.shapes.Add(bigShape);

        var fullGridMock = new MockRestrictedGrid();

        // 1. Without a 1x1 in database, mercy fallback generates a 1x1 (0,0) BlockModel
        BlockModel[] batchFallback = BlockSpawnGenerator.GenerateBatch(0, fullGridMock, mercyDb, testConfig);
        Assert.IsTrue(BlockSpawnGenerator.CanPlaceBlockAnywhere(batchFallback[2], fullGridMock),
            "Mercy fallback block must be placeable on the restricted grid.");

        // 2. When a 1x1 shape exists in database, mercy finds and uses it
        ShapeData singleTileShape = ScriptableObject.CreateInstance<ShapeData>();
        singleTileShape.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0) };
        mercyDb.shapes.Add(singleTileShape);

        BlockModel[] batchWithMercy = BlockSpawnGenerator.GenerateBatch(0, fullGridMock, mercyDb, testConfig);
        Assert.IsTrue(BlockSpawnGenerator.CanPlaceBlockAnywhere(batchWithMercy[2], fullGridMock),
            "Mercy block found in database must be placeable on the restricted grid.");

        UnityEngine.Object.DestroyImmediate(mercyDb);
        UnityEngine.Object.DestroyImmediate(bigShape);
        UnityEngine.Object.DestroyImmediate(singleTileShape);
    }

    [Test]
    public void GenerateBatch_UnderThreshold_GeneratesSynergisticBatch_OnEmptyBoard()
    {
        testConfig.enableSynergisticBatches = true;
        testConfig.synergyRateUnderThreshold = 1.0f;
        testConfig.comboPhaseScoreThreshold = 200000;

        ShapeDatabase synDb = ScriptableObject.CreateInstance<ShapeDatabase>();

        ShapeData line4 = ScriptableObject.CreateInstance<ShapeData>();
        line4.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0) };
        synDb.shapes.Add(line4);

        ShapeData sq2x2 = ScriptableObject.CreateInstance<ShapeData>();
        sq2x2.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) };
        synDb.shapes.Add(sq2x2);

        ShapeData line5 = ScriptableObject.CreateInstance<ShapeData>();
        line5.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0) };
        synDb.shapes.Add(line5);

        ShapeData line3 = ScriptableObject.CreateInstance<ShapeData>();
        line3.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
        synDb.shapes.Add(line3);

        BlockModel[] batch = BlockSpawnGenerator.GenerateBatch(0, null, synDb, testConfig);

        Assert.IsNotNull(batch, "Batch should not be null.");
        Assert.AreEqual(3, batch.Length, "Synergistic batch must have 3 blocks.");
        Assert.IsNotNull(batch[0]);
        Assert.IsNotNull(batch[1]);
        Assert.IsNotNull(batch[2]);

        // Synergy patterns with Line 4, 2x2, Line 5, Line 3 sum to 12 (4+4+4, 5+3+4) or 13 (5+4+4)
        int totalTiles = batch[0].ShapeOffsets.Count + batch[1].ShapeOffsets.Count + batch[2].ShapeOffsets.Count;
        Assert.IsTrue(totalTiles == 12 || totalTiles == 13, "Synergistic batch on empty board should sum to 12 or 13 tiles for optimal multi-line setups.");

        UnityEngine.Object.DestroyImmediate(synDb);
        UnityEngine.Object.DestroyImmediate(line4);
        UnityEngine.Object.DestroyImmediate(sq2x2);
        UnityEngine.Object.DestroyImmediate(line5);
        UnityEngine.Object.DestroyImmediate(line3);
    }

    [Test]
    public void GenerateBatch_WithTrayCompleterShapes_GeneratesThreePieceFullRowBatch()
    {
        testConfig.enableSynergisticBatches = true;
        testConfig.synergyRateUnderThreshold = 1.0f;
        testConfig.comboPhaseScoreThreshold = 200000;

        ShapeDatabase trayDb = ScriptableObject.CreateInstance<ShapeDatabase>();

        ShapeData line3 = ScriptableObject.CreateInstance<ShapeData>();
        line3.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
        trayDb.shapes.Add(line3);

        ShapeData line2 = ScriptableObject.CreateInstance<ShapeData>();
        line2.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0) };
        trayDb.shapes.Add(line2);

        // When only Line 3 and Line 2 exist, Pattern 3 (3 + 3 + 2 = 8) is selected
        BlockModel[] batch = BlockSpawnGenerator.GenerateBatch(0, null, trayDb, testConfig);

        Assert.IsNotNull(batch);
        Assert.AreEqual(3, batch.Length);
        int totalTiles = batch[0].ShapeOffsets.Count + batch[1].ShapeOffsets.Count + batch[2].ShapeOffsets.Count;
        Assert.AreEqual(8, totalTiles, "Pattern 3 (3 + 3 + 2) must sum to exactly 8 tiles to clear an entire row.");

        UnityEngine.Object.DestroyImmediate(trayDb);
        UnityEngine.Object.DestroyImmediate(line3);
        UnityEngine.Object.DestroyImmediate(line2);
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

    [Test]
    public void GenerateGridAssistedBatch_WithTwoNearFullLines_ProvidesSequentialClearingShapes()
    {
        testConfig.enableSynergisticBatches = true;
        testConfig.synergyRateUnderThreshold = 1.0f;
        testConfig.comboPhaseScoreThreshold = 200000;

        ShapeDatabase multiClearDb = ScriptableObject.CreateInstance<ShapeDatabase>();

        ShapeData line3 = ScriptableObject.CreateInstance<ShapeData>();
        line3.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
        multiClearDb.shapes.Add(line3);

        ShapeData line2 = ScriptableObject.CreateInstance<ShapeData>();
        line2.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0) };
        multiClearDb.shapes.Add(line2);

        var dualGrid = new MockDualNearFullGrid();

        // dualGrid has Row 0 missing 2 cells (at x=6, x=7), and Row 1 missing 3 cells (at x=5, x=6, x=7)
        BlockModel[] batch = BlockSpawnGenerator.GenerateBatch(50000, dualGrid, multiClearDb, testConfig);

        Assert.IsNotNull(batch);
        Assert.AreEqual(3, batch.Length);

        // Batch slot 0 or 1 will clear row 0, and the other will clear row 1
        int totalClearsFound = 0;
        foreach (var block in batch)
        {
            for (int x = 0; x < dualGrid.GridWidth; x++)
            {
                for (int y = 0; y < dualGrid.GridHeight; y++)
                {
                    if (BlockSpawnGenerator.CountLinesClearedIfPlaced(block, x, y, dualGrid) > 0)
                    {
                        totalClearsFound++;
                        goto NextBlock;
                    }
                }
            }
            NextBlock:;
        }

        Assert.IsTrue(totalClearsFound >= 2, "Grid-Assisted batch should provide shapes that sequentially clear both near-full lines.");

        UnityEngine.Object.DestroyImmediate(multiClearDb);
        UnityEngine.Object.DestroyImmediate(line3);
        UnityEngine.Object.DestroyImmediate(line2);
    }

    [Test]
    public void GenerateGridAssistedBatch_WithExactGap_FeedsExactMatchingShape()
    {
        testConfig.enableSynergisticBatches = true;
        testConfig.synergyRateUnderThreshold = 1.0f;
        testConfig.comboPhaseScoreThreshold = 200000;

        ShapeDatabase gapDb = ScriptableObject.CreateInstance<ShapeDatabase>();

        ShapeData line3 = ScriptableObject.CreateInstance<ShapeData>();
        line3.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
        gapDb.shapes.Add(line3);

        ShapeData square2x2 = ScriptableObject.CreateInstance<ShapeData>();
        square2x2.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) };
        gapDb.shapes.Add(square2x2);

        var gapGrid = new MockThreeGapRowGrid();

        BlockModel[] batch = BlockSpawnGenerator.GenerateBatch(10000, gapGrid, gapDb, testConfig);

        Assert.IsNotNull(batch);
        Assert.AreEqual(3, batch.Length);

        // Slot 0 should be the exact 3-length horizontal line that fits the gap and clears row 0
        Assert.AreEqual(3, batch[0].ShapeOffsets.Count, "Slot 0 should be the 3-tile shape matching the exact 3-tile gap.");
        int linesCleared = BlockSpawnGenerator.CountLinesClearedIfPlaced(batch[0], 5, 0, gapGrid);
        Assert.AreEqual(1, linesCleared, "Placing slot 0 into the gap at (5,0) must clear row 0.");

        UnityEngine.Object.DestroyImmediate(gapDb);
        UnityEngine.Object.DestroyImmediate(line3);
        UnityEngine.Object.DestroyImmediate(square2x2);
    }

    private class MockThreeGapRowGrid : IGridService
    {
        public int GridWidth => 8;
        public int GridHeight => 8;
        public int OccupiedCellCount => 5;
        public float OccupancyRatio => 5f / 64f;

#pragma warning disable CS0067
        public event Action<bool, List<Vector2Int>> OnPreviewStateChanged;
        public event Action<List<Vector2Int>> OnBlockPlaced;
        public event Action<List<int>, List<int>> OnLinesCleared;
#pragma warning restore CS0067

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
            // Row 0 has cols 0..4 filled, cols 5..7 empty (gap of 3)
            return row == 0 && col >= 0 && col < 5;
        }
    }

    private class MockDualNearFullGrid : IGridService
    {
        public int GridWidth => 8;
        public int GridHeight => 8;
        public int OccupiedCellCount => 13;
        public float OccupancyRatio => 13f / 64f;

#pragma warning disable CS0067
        public event Action<bool, List<Vector2Int>> OnPreviewStateChanged;
        public event Action<List<Vector2Int>> OnBlockPlaced;
        public event Action<List<int>, List<int>> OnLinesCleared;
#pragma warning restore CS0067

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
            // Row 0 has cols 0..5 filled, cols 6..7 empty (gap of 2)
            if (row == 0 && col >= 0 && col < 6) return true;
            // Row 1 has cols 0..4 filled, cols 5..7 empty (gap of 3)
            if (row == 1 && col >= 0 && col < 5) return true;
            return false;
        }
    }

    [Test]
    public void GenerateBatch_NormalSpawning_NeverContains1x1Dot()
    {
        // Even if a 1x1 shape exists in the database
        ShapeDatabase dbWith1x1 = ScriptableObject.CreateInstance<ShapeDatabase>();
        ShapeData dot = ScriptableObject.CreateInstance<ShapeData>();
        dot.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0) };
        dbWith1x1.shapes.Add(dot);

        ShapeData line2 = ScriptableObject.CreateInstance<ShapeData>();
        line2.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0) };
        dbWith1x1.shapes.Add(line2);

        ShapeData sq2x2 = ScriptableObject.CreateInstance<ShapeData>();
        sq2x2.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) };
        dbWith1x1.shapes.Add(sq2x2);

        var emptyGrid = new MockNearFullRowGrid(); // Has plenty of playable space

        for (int i = 0; i < 20; i++)
        {
            BlockModel[] batch = BlockSpawnGenerator.GenerateBatch(0, emptyGrid, dbWith1x1, testConfig);
            Assert.IsNotNull(batch);
            foreach (var block in batch)
            {
                Assert.IsNotNull(block);
                Assert.Greater(block.ShapeOffsets.Count, 1, "Normal spawning must NEVER produce a 1x1 dot block.");
            }
        }

        UnityEngine.Object.DestroyImmediate(dbWith1x1);
        UnityEngine.Object.DestroyImmediate(dot);
        UnityEngine.Object.DestroyImmediate(line2);
        UnityEngine.Object.DestroyImmediate(sq2x2);
    }

    [Test]
    public void Rect2x3_CanBeSpawned_AndHasCorrectDimensions()
    {
        ShapeDatabase testDb = ScriptableObject.CreateInstance<ShapeDatabase>();
        ShapeData rect2x3 = ScriptableObject.CreateInstance<ShapeData>();
        rect2x3.canRotate = true;
        rect2x3.baseOffsets = new List<Vector2Int>
        {
            new Vector2Int(0, 1), new Vector2Int(1, 1),
            new Vector2Int(0, 0), new Vector2Int(1, 0),
            new Vector2Int(0, -1), new Vector2Int(1, -1)
        };
        testDb.shapes.Add(rect2x3);

        ShapeData line2 = ScriptableObject.CreateInstance<ShapeData>();
        line2.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0) };
        testDb.shapes.Add(line2);

        BlockModel model = BlockSpawnGenerator.CreateBlockModel(rect2x3);
        Assert.IsNotNull(model);
        Assert.AreEqual(6, model.ShapeOffsets.Count, "2x3 block model must contain exactly 6 tiles.");

        // Check bounding box: either 2x3 or 3x2 depending on rotation
        int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
        foreach (var off in model.ShapeOffsets)
        {
            if (off.x < minX) minX = off.x;
            if (off.x > maxX) maxX = off.x;
            if (off.y < minY) minY = off.y;
            if (off.y > maxY) maxY = off.y;
        }
        int w = maxX - minX + 1;
        int h = maxY - minY + 1;
        bool validDimensions = (w == 2 && h == 3) || (w == 3 && h == 2);
        Assert.IsTrue(validDimensions, $"2x3 block dimensions must be 2x3 or 3x2, but got {w}x{h}.");

        UnityEngine.Object.DestroyImmediate(testDb);
        UnityEngine.Object.DestroyImmediate(rect2x3);
        UnityEngine.Object.DestroyImmediate(line2);
    }

    [Test]
    public void ScenarioChain_WhenPlayerFollowsSetup_SpawnsFinisherAndClearsLines()
    {
        ShapeDatabase db = ScriptableObject.CreateInstance<ShapeDatabase>();
        ShapeData line4 = ScriptableObject.CreateInstance<ShapeData>();
        line4.canRotate = true;
        line4.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0) };
        db.shapes.Add(line4);

        ShapeData sq2x2 = ScriptableObject.CreateInstance<ShapeData>();
        sq2x2.canRotate = false;
        sq2x2.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) };
        db.shapes.Add(sq2x2);

        SpawnModel model = new SpawnModel();
        model.ActiveScenarioId = 0; // Scenario 0: Double Blast Rush
        model.ScenarioStepIndex = 1; // Waiting for finisher

        // Dual grid has 2 near-full rows (Row 0 missing cols 6..7, Row 1 missing cols 5..7)
        var dualGrid = new MockDualNearFullGrid();

        BlockModel[] batch = BlockSpawnGenerator.GenerateBatch(0, dualGrid, db, testConfig, model);

        Assert.IsNotNull(batch);
        Assert.AreEqual(3, batch.Length);
        Assert.AreEqual(-1, model.ActiveScenarioId, "Scenario should complete and reset back to -1.");

        UnityEngine.Object.DestroyImmediate(db);
        UnityEngine.Object.DestroyImmediate(line4);
        UnityEngine.Object.DestroyImmediate(sq2x2);
    }

    [Test]
    public void ScenarioChain_WhenPlayerDeviates_DropsScenarioAndRunsAdaptiveSolver()
    {
        ShapeDatabase db = ScriptableObject.CreateInstance<ShapeDatabase>();
        ShapeData line2 = ScriptableObject.CreateInstance<ShapeData>();
        line2.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0) };
        db.shapes.Add(line2);

        SpawnModel model = new SpawnModel();
        model.ActiveScenarioId = 0; // Scenario 0 expects at least 2 rows with >= 4 cells
        model.ScenarioStepIndex = 1;

        // DevGrid only has 2 cells in total on row 7, violating Scenario 0 expected form
        var devGrid = new MockDeviatedGrid();

        BlockModel[] batch = BlockSpawnGenerator.GenerateBatch(0, devGrid, db, testConfig, model);

        Assert.IsNotNull(batch);
        Assert.AreEqual(3, batch.Length);
        Assert.AreEqual(-1, model.ActiveScenarioId, "Scenario must be dropped when player deviates.");

        UnityEngine.Object.DestroyImmediate(db);
        UnityEngine.Object.DestroyImmediate(line2);
    }

    [Test]
    public void IntelligentScanner_IntersectionAndMultiBlast_FeedsCornerShapeToDetonateBothLines()
    {
        ShapeDatabase db = ScriptableObject.CreateInstance<ShapeDatabase>();

        // Compact Angular Corner Shape: Small V (3 tiles)
        ShapeData smallV = ScriptableObject.CreateInstance<ShapeData>();
        smallV.canRotate = true;
        smallV.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) };
        db.shapes.Add(smallV);

        // Clumsy Large Shape: Square 3x3 (9 tiles)
        ShapeData sq3x3 = ScriptableObject.CreateInstance<ShapeData>();
        sq3x3.canRotate = false;
        sq3x3.baseOffsets = new List<Vector2Int>();
        for (int x = 0; x < 3; x++) for (int y = 0; y < 3; y++) sq3x3.baseOffsets.Add(new Vector2Int(x, y));
        db.shapes.Add(sq3x3);

        var crossGrid = new MockCrossNearFullGrid();

        BlockModel[] batch = BlockSpawnGenerator.GenerateBatch(0, crossGrid, db, testConfig);

        Assert.IsNotNull(batch);
        Assert.AreEqual(3, batch.Length);

        // Slot 0 should be the Small V (3 tiles) which triggers the Cross Blast of 2 lines!
        Assert.AreEqual(3, batch[0].ShapeOffsets.Count, "Slot 0 should pick the agile 3-tile Small V corner piece.");
        int linesCleared = BlockSpawnGenerator.CountLinesClearedIfPlaced(batch[0], 3, 2, crossGrid);
        Assert.AreEqual(2, linesCleared, "Placing Small V into the (3,2) intersection must clear 2 lines simultaneously (Cross Blast)!");

        UnityEngine.Object.DestroyImmediate(db);
        UnityEngine.Object.DestroyImmediate(smallV);
        UnityEngine.Object.DestroyImmediate(sq3x3);
    }

    [Test]
    public void IntelligentScanner_PrefersCompactAngularShapes_OverLargeShapes()
    {
        ShapeDatabase db = ScriptableObject.CreateInstance<ShapeDatabase>();

        // Angular shape: Shape L (4 tiles)
        ShapeData shapeL = ScriptableObject.CreateInstance<ShapeData>();
        shapeL.canRotate = true;
        shapeL.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 0) };
        db.shapes.Add(shapeL);

        // Angular shape: Small V (3 tiles)
        ShapeData smallV = ScriptableObject.CreateInstance<ShapeData>();
        smallV.canRotate = true;
        smallV.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) };
        db.shapes.Add(smallV);

        // Large clumsy shape: Square 3x3 (9 tiles)
        ShapeData sq3x3 = ScriptableObject.CreateInstance<ShapeData>();
        sq3x3.canRotate = false;
        sq3x3.baseOffsets = new List<Vector2Int>();
        for (int x = 0; x < 3; x++) for (int y = 0; y < 3; y++) sq3x3.baseOffsets.Add(new Vector2Int(x, y));
        db.shapes.Add(sq3x3);

        var emptyGrid = new MockNearFullRowGrid();

        BlockModel[] batch = BlockSpawnGenerator.GenerateBatch(0, emptyGrid, db, testConfig);

        Assert.IsNotNull(batch);
        foreach (var block in batch)
        {
            Assert.AreNotEqual(9, block.ShapeOffsets.Count, "Scanner should heavily penalize large clumsy 3x3 shapes in favor of agile angular shapes.");
        }

        UnityEngine.Object.DestroyImmediate(db);
        UnityEngine.Object.DestroyImmediate(shapeL);
        UnityEngine.Object.DestroyImmediate(smallV);
        UnityEngine.Object.DestroyImmediate(sq3x3);
    }

    [Test]
    public void AllTenScenarios_CanBeGenerated_WithCompleteShapesDatabase()
    {
        ShapeDatabase fullDb = ScriptableObject.CreateInstance<ShapeDatabase>();

        // Populate full shapes inventory
        ShapeData sq2x2 = ScriptableObject.CreateInstance<ShapeData>();
        sq2x2.name = "2_Square_2x2";
        sq2x2.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) };
        fullDb.shapes.Add(sq2x2);

        ShapeData line4 = ScriptableObject.CreateInstance<ShapeData>();
        line4.name = "2_Line_4";
        line4.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0) };
        fullDb.shapes.Add(line4);

        ShapeData line5 = ScriptableObject.CreateInstance<ShapeData>();
        line5.name = "3_Line_5";
        line5.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0), new Vector2Int(4, 0) };
        fullDb.shapes.Add(line5);

        ShapeData smallV = ScriptableObject.CreateInstance<ShapeData>();
        smallV.name = "2_Small_V";
        smallV.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) };
        fullDb.shapes.Add(smallV);

        ShapeData shapeL = ScriptableObject.CreateInstance<ShapeData>();
        shapeL.name = "2_Shape_L";
        shapeL.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 0) };
        fullDb.shapes.Add(shapeL);

        ShapeData shapeJ = ScriptableObject.CreateInstance<ShapeData>();
        shapeJ.name = "2_Shape_J";
        shapeJ.baseOffsets = new List<Vector2Int> { new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(0, 0) };
        fullDb.shapes.Add(shapeJ);

        ShapeData line3 = ScriptableObject.CreateInstance<ShapeData>();
        line3.name = "1_Line_3";
        line3.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
        fullDb.shapes.Add(line3);

        ShapeData shapeS = ScriptableObject.CreateInstance<ShapeData>();
        shapeS.name = "2_Shape_S";
        shapeS.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(2, 1) };
        fullDb.shapes.Add(shapeS);

        ShapeData shapeZ = ScriptableObject.CreateInstance<ShapeData>();
        shapeZ.name = "2_Shape_Z";
        shapeZ.baseOffsets = new List<Vector2Int> { new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) };
        fullDb.shapes.Add(shapeZ);

        ShapeData shapeT = ScriptableObject.CreateInstance<ShapeData>();
        shapeT.name = "2_Shape_T";
        shapeT.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(1, 0) };
        fullDb.shapes.Add(shapeT);

        Assert.AreEqual(10, BlockSpawnGenerator.TotalScenarios, "TotalScenarios should be 10.");

        for (int i = 0; i < BlockSpawnGenerator.TotalScenarios; i++)
        {
            BlockModel[] setup = BlockSpawnGenerator.GenerateScenarioSetup(i, fullDb, testConfig);
            Assert.IsNotNull(setup, $"Scenario {i} setup should not be null.");
            Assert.AreEqual(3, setup.Length, $"Scenario {i} setup must yield exactly 3 blocks.");
            Assert.IsNotNull(setup[0], $"Scenario {i} block 0 should not be null.");
            Assert.IsNotNull(setup[1], $"Scenario {i} block 1 should not be null.");
            Assert.IsNotNull(setup[2], $"Scenario {i} block 2 should not be null.");
        }

        UnityEngine.Object.DestroyImmediate(fullDb);
        UnityEngine.Object.DestroyImmediate(sq2x2);
        UnityEngine.Object.DestroyImmediate(line4);
        UnityEngine.Object.DestroyImmediate(line5);
        UnityEngine.Object.DestroyImmediate(smallV);
        UnityEngine.Object.DestroyImmediate(shapeL);
        UnityEngine.Object.DestroyImmediate(shapeJ);
        UnityEngine.Object.DestroyImmediate(line3);
        UnityEngine.Object.DestroyImmediate(shapeS);
        UnityEngine.Object.DestroyImmediate(shapeZ);
        UnityEngine.Object.DestroyImmediate(shapeT);
    }

    private class MockCrossNearFullGrid : IGridService
    {
        public int GridWidth => 8;
        public int GridHeight => 8;
        public int OccupiedCellCount => 12;
        public float OccupancyRatio => 12f / 64f;

#pragma warning disable CS0067
        public event Action<bool, List<Vector2Int>> OnPreviewStateChanged;
        public event Action<List<Vector2Int>> OnBlockPlaced;
        public event Action<List<int>, List<int>> OnLinesCleared;
#pragma warning restore CS0067

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
            // Row 2 has cols 0,1,2, 5,6,7 occupied (cols 3,4 empty)
            if (row == 2 && (col != 3 && col != 4)) return true;
            // Col 3 has rows 0,1, 4,5,6,7 occupied (rows 2,3 empty)
            if (col == 3 && (row != 2 && row != 3)) return true;
            return false;
        }
    }

    private class MockDeviatedGrid : IGridService
    {
        public int GridWidth => 8;
        public int GridHeight => 8;
        public int OccupiedCellCount => 2;
        public float OccupancyRatio => 2f / 64f;

#pragma warning disable CS0067
        public event Action<bool, List<Vector2Int>> OnPreviewStateChanged;
        public event Action<List<Vector2Int>> OnBlockPlaced;
        public event Action<List<int>, List<int>> OnLinesCleared;
#pragma warning restore CS0067

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
            // Only 2 cells placed at (0, 7) and (1, 7) -> Player deviated!
            return row == 7 && (col == 0 || col == 1);
        }
    }

    private class MockNearFullRowGrid : IGridService
    {
        public int GridWidth => 8;
        public int GridHeight => 8;
        public int OccupiedCellCount => 7;
        public float OccupancyRatio => 7f / 64f;

#pragma warning disable CS0067
        public event Action<bool, List<Vector2Int>> OnPreviewStateChanged;
        public event Action<List<Vector2Int>> OnBlockPlaced;
        public event Action<List<int>, List<int>> OnLinesCleared;
#pragma warning restore CS0067

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

#pragma warning disable CS0067
        public event Action<bool, List<Vector2Int>> OnPreviewStateChanged;
        public event Action<List<Vector2Int>> OnBlockPlaced;
        public event Action<List<int>, List<int>> OnLinesCleared;
#pragma warning restore CS0067

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

    [Test]
    public void IntelligentScanner_BoardShutdown_ClearsScatteredBlocksInOneTurn()
    {
        ShapeDatabase db = ScriptableObject.CreateInstance<ShapeDatabase>();

        ShapeData line2 = ScriptableObject.CreateInstance<ShapeData>();
        line2.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0) };
        db.shapes.Add(line2);

        ShapeData line3 = ScriptableObject.CreateInstance<ShapeData>();
        line3.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
        db.shapes.Add(line3);

        ShapeData smallV = ScriptableObject.CreateInstance<ShapeData>();
        smallV.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1) };
        db.shapes.Add(smallV);

        var shutdownGrid = new MockScatteredShutdownGrid();

        BlockModel[] batch = BlockSpawnGenerator.GenerateBatch(0, shutdownGrid, db, testConfig);

        Assert.IsNotNull(batch, "Batch should not be null.");
        Assert.AreEqual(3, batch.Length, "Batch must have 3 blocks.");

        bool hasLine2 = false, hasLine3 = false;
        foreach (var b in batch)
        {
            if (b.ShapeOffsets.Count == 2) hasLine2 = true;
            if (b.ShapeOffsets.Count == 3) hasLine3 = true;
        }

        Assert.IsTrue(hasLine2, "Shutdown batch must include the 2-tile gap-closer for Row 1.");
        Assert.IsTrue(hasLine3, "Shutdown batch must include the 3-tile gap-closer for Row 4.");

        int linesCleared1 = BlockSpawnGenerator.CountLinesClearedIfPlaced(batch[0], 6, 1, shutdownGrid);
        int linesCleared2 = BlockSpawnGenerator.CountLinesClearedIfPlaced(batch[1], 0, 4, shutdownGrid);
        Assert.IsTrue(linesCleared1 > 0 || linesCleared2 > 0, "Batch blocks must directly detonate lines on the board.");

        UnityEngine.Object.DestroyImmediate(db);
        UnityEngine.Object.DestroyImmediate(line2);
        UnityEngine.Object.DestroyImmediate(line3);
        UnityEngine.Object.DestroyImmediate(smallV);
    }

    private class MockScatteredShutdownGrid : IGridService
    {
        public int GridWidth => 8;
        public int GridHeight => 8;
        public int OccupiedCellCount => 11;
        public float OccupancyRatio => 11f / 64f;

#pragma warning disable CS0067
        public event Action<bool, List<Vector2Int>> OnPreviewStateChanged;
        public event Action<List<Vector2Int>> OnBlockPlaced;
        public event Action<List<int>, List<int>> OnLinesCleared;
#pragma warning restore CS0067

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
            if (row == 1 && col >= 0 && col < 6) return true;
            if (row == 4 && col >= 3 && col <= 7) return true;
            return false;
        }
    }

    [Test]
    public void DifficultyScaling_OverScoreThreshold_DisablesScenarioChains()
    {
        ShapeDatabase db = ScriptableObject.CreateInstance<ShapeDatabase>();
        ShapeData line2 = ScriptableObject.CreateInstance<ShapeData>();
        line2.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0) };
        db.shapes.Add(line2);

        SpawnModel model = new SpawnModel();
        model.ActiveScenarioId = 0;
        model.ScenarioStepIndex = 1;

        var dualGrid = new MockDualNearFullGrid();

        // When score >= 200,000 (e.g. 250,000), scenarios must be dropped/disabled
        BlockModel[] batch = BlockSpawnGenerator.GenerateBatch(250000, dualGrid, db, testConfig, model);

        Assert.IsNotNull(batch);
        Assert.AreEqual(3, batch.Length);
        Assert.AreEqual(-1, model.ActiveScenarioId, "Scenario must be cleared when player is above score threshold.");

        UnityEngine.Object.DestroyImmediate(db);
        UnityEngine.Object.DestroyImmediate(line2);
    }

    [Test]
    public void DifficultyScaling_OverScoreThreshold_AllowsBulkyPieces_WhenBoardIsClear()
    {
        ShapeDatabase db = ScriptableObject.CreateInstance<ShapeDatabase>();

        ShapeData sq3x3 = ScriptableObject.CreateInstance<ShapeData>();
        sq3x3.canRotate = false;
        sq3x3.baseOffsets = new List<Vector2Int>();
        for (int x = 0; x < 3; x++) for (int y = 0; y < 3; y++) sq3x3.baseOffsets.Add(new Vector2Int(x, y));
        db.shapes.Add(sq3x3);

        ShapeData line2 = ScriptableObject.CreateInstance<ShapeData>();
        line2.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0) };
        db.shapes.Add(line2);

        testConfig.comboPhaseScoreThreshold = 200000;
        testConfig.highScoreBulkyPieceChance = 1.0f;

        var clearGrid = new MockNearFullRowGrid(); // Clean playable board

        bool foundBulky = false;
        for (int i = 0; i < 15; i++)
        {
            BlockModel[] batch = BlockSpawnGenerator.GenerateBatch(250000, clearGrid, db, testConfig);
            Assert.IsNotNull(batch);
            foreach (var b in batch)
            {
                if (b.ShapeOffsets.Count == 9)
                {
                    foundBulky = true;
                    break;
                }
            }
            if (foundBulky) break;
        }

        Assert.IsTrue(foundBulky, "Over 200,000 score, bulky pieces (3x3) should be permitted when board is clear to create spatial pressure.");

        UnityEngine.Object.DestroyImmediate(db);
        UnityEngine.Object.DestroyImmediate(sq3x3);
        UnityEngine.Object.DestroyImmediate(line2);
    }

    [Test]
    public void DifficultyScaling_OverScoreThreshold_GuaranteesPlayableMoves()
    {
        ShapeDatabase db = ScriptableObject.CreateInstance<ShapeDatabase>();
        ShapeData line2 = ScriptableObject.CreateInstance<ShapeData>();
        line2.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0) };
        db.shapes.Add(line2);

        var restrictedGrid = new MockRestrictedGrid();
        testConfig.enableMercyMode = true;

        // Even with extreme high score (999,999), player fairness/mercy mode ensures at least one block can be placed
        BlockModel[] batch = BlockSpawnGenerator.GenerateBatch(999999, restrictedGrid, db, testConfig);
        Assert.IsNotNull(batch);
        Assert.IsTrue(BlockSpawnGenerator.CanPlaceAny(batch, restrictedGrid), "High score mode must still guarantee at least one valid playable move.");

        UnityEngine.Object.DestroyImmediate(db);
        UnityEngine.Object.DestroyImmediate(line2);
    }
}

