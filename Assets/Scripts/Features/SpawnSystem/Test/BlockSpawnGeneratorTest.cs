using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public partial class BlockSpawnGeneratorTest
{
    private ShapeDatabase testDatabase;
    private SpawnConfiguration testConfig;

    [SetUp]
    public void SetUp()
    {
        testDatabase = ScriptableObject.CreateInstance<ShapeDatabase>();
        testConfig = ScriptableObject.CreateInstance<SpawnConfiguration>();
        testConfig.shapeDatabase = testDatabase;

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

        ScenarioDatabase scenarioDb = ScriptableObject.CreateInstance<ScenarioDatabase>();
        ScenarioData scenario = ScriptableObject.CreateInstance<ScenarioData>();
        scenario.setupBatch = new ScenarioBatch { slot0 = line4, slot1 = sq2x2, slot2 = line4 }; // 4 + 4 + 4 = 12 tiles
        scenarioDb.scenarios.Add(scenario);
        testConfig.scenarioDatabase = scenarioDb;
        SpawnModel spawnModel = new SpawnModel();

        BlockModel[] batch = BlockSpawnGenerator.GenerateBatch(0, null, synDb, testConfig, spawnModel);

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
        UnityEngine.Object.DestroyImmediate(scenarioDb);
        UnityEngine.Object.DestroyImmediate(scenario);
    }

    [Test]
    public void GenerateBatch_WithTrayCompleterShapes_GeneratesThreePieceFullRowBatch()
    {
        testConfig.enableScenarioChains = true;
        testConfig.comboPhaseScoreThreshold = 200000;

        ShapeDatabase trayDb = ScriptableObject.CreateInstance<ShapeDatabase>();

        ShapeData line3 = ScriptableObject.CreateInstance<ShapeData>();
        line3.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
        trayDb.shapes.Add(line3);

        ShapeData line2 = ScriptableObject.CreateInstance<ShapeData>();
        line2.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0) };
        trayDb.shapes.Add(line2);

        ScenarioDatabase scenarioDb = ScriptableObject.CreateInstance<ScenarioDatabase>();
        ScenarioData scenario = ScriptableObject.CreateInstance<ScenarioData>();
        scenario.setupBatch = new ScenarioBatch { slot0 = line3, slot1 = line3, slot2 = line2 };
        scenarioDb.scenarios.Add(scenario);
        testConfig.scenarioDatabase = scenarioDb;
        SpawnModel spawnModel = new SpawnModel();

        // When scenario exists with Line 3, Line 3, Line 2 (3 + 3 + 2 = 8), full row setup is generated
        BlockModel[] batch = BlockSpawnGenerator.GenerateBatch(0, null, trayDb, testConfig, spawnModel);

        Assert.IsNotNull(batch);
        Assert.AreEqual(3, batch.Length);
        int totalTiles = batch[0].ShapeOffsets.Count + batch[1].ShapeOffsets.Count + batch[2].ShapeOffsets.Count;
        Assert.AreEqual(8, totalTiles, "Tray completer setup (3 + 3 + 2) must sum to exactly 8 tiles to clear an entire row.");

        UnityEngine.Object.DestroyImmediate(trayDb);
        UnityEngine.Object.DestroyImmediate(line3);
        UnityEngine.Object.DestroyImmediate(line2);
        UnityEngine.Object.DestroyImmediate(scenarioDb);
        UnityEngine.Object.DestroyImmediate(scenario);
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

        ScenarioDatabase scenarioDb = ScriptableObject.CreateInstance<ScenarioDatabase>();
        ScenarioData sc0 = ScriptableObject.CreateInstance<ScenarioData>();
        sc0.scenarioName = "Double Blast Rush";
        sc0.matchTolerance = 4;
        sc0.targetBoard = new bool[64];
        for (int c = 0; c < 6; c++) sc0.targetBoard[c + 0 * 8] = true;
        for (int c = 0; c < 5; c++) sc0.targetBoard[c + 1 * 8] = true;
        sc0.finisherBatch = new ScenarioBatch { slot0 = line4, slot1 = sq2x2, slot2 = line4 };
        scenarioDb.scenarios.Add(sc0);
        testConfig.scenarioDatabase = scenarioDb;

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
        UnityEngine.Object.DestroyImmediate(scenarioDb);
        UnityEngine.Object.DestroyImmediate(sc0);
    }
}
