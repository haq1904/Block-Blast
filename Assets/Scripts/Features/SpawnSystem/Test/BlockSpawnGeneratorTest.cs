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

    private class MockThreeGapRowGrid : IGridService
    {
        public int GridWidth => 8;
        public int GridHeight => 8;
        public int OccupiedCellCount => 5;
        public float OccupancyRatio => 5f / 64f;

#pragma warning disable CS0067
        public event Action<bool, List<CellPlacementData>> OnPreviewStateChanged;
        public event Action<List<CellPlacementData>> OnBlockPlaced;
        public event Action<List<int>, List<int>> OnLinesCleared;
        public event Action<List<int>, List<int>> OnPreviewLinesToClear;
        public event Action<int, int, bool> OnPlacementResolved;
#pragma warning restore CS0067

        public Vector2Int GetGridPositionFromWorld(Vector3 worldPos) => Vector2Int.zero;
        public Vector3 GetWorldPositionFromGrid(Vector2Int gridPos) => Vector3.zero;
        public void RequestPreview(List<CellPlacementData> cells) { }
        public void RequestPreview(List<Vector2Int> gridPositions) { }
        public void PlaceBlocks(List<CellPlacementData> cells) { }
        public void PlaceBlocks(List<Vector2Int> gridPositions) { }

        public bool CanPlaceBlocks(List<CellPlacementData> cells) => true;

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
        public event Action<bool, List<CellPlacementData>> OnPreviewStateChanged;
        public event Action<List<CellPlacementData>> OnBlockPlaced;
        public event Action<List<int>, List<int>> OnLinesCleared;
        public event Action<List<int>, List<int>> OnPreviewLinesToClear;
        public event Action<int, int, bool> OnPlacementResolved;
#pragma warning restore CS0067

        public Vector2Int GetGridPositionFromWorld(Vector3 worldPos) => Vector2Int.zero;
        public Vector3 GetWorldPositionFromGrid(Vector2Int gridPos) => Vector3.zero;
        public void RequestPreview(List<CellPlacementData> cells) { }
        public void RequestPreview(List<Vector2Int> gridPositions) { }
        public void PlaceBlocks(List<CellPlacementData> cells) { }
        public void PlaceBlocks(List<Vector2Int> gridPositions) { }

        public bool CanPlaceBlocks(List<CellPlacementData> cells) => true;

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

    [Test]
    public void ScenarioChain_WhenPlayerDeviates_DropsScenarioAndRunsAdaptiveSolver()
    {
        ShapeDatabase db = ScriptableObject.CreateInstance<ShapeDatabase>();
        ShapeData line2 = ScriptableObject.CreateInstance<ShapeData>();
        line2.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0) };
        db.shapes.Add(line2);

        ScenarioDatabase scenarioDb = ScriptableObject.CreateInstance<ScenarioDatabase>();
        ScenarioData sc0 = ScriptableObject.CreateInstance<ScenarioData>();
        sc0.scenarioName = "Test Scenario";
        sc0.matchTolerance = 0;
        sc0.targetBoard = new bool[64];
        for (int c = 0; c < 8; c++) sc0.targetBoard[c + 0 * 8] = true;
        scenarioDb.scenarios.Add(sc0);
        testConfig.scenarioDatabase = scenarioDb;

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
        UnityEngine.Object.DestroyImmediate(scenarioDb);
        UnityEngine.Object.DestroyImmediate(sc0);
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
    public void AllScenarios_CanBeGenerated_WithCompleteShapesDatabase()
    {
        ScenarioDatabase scenarioDb = ScriptableObject.CreateInstance<ScenarioDatabase>();
        ShapeData sq2x2 = ScriptableObject.CreateInstance<ShapeData>();
        sq2x2.name = "2_Square_2x2";
        sq2x2.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1) };

        for (int i = 0; i < BlockSpawnGenerator.TotalScenarios; i++)
        {
            ScenarioData sc = ScriptableObject.CreateInstance<ScenarioData>();
            sc.setupBatch = new ScenarioBatch { slot0 = sq2x2, slot1 = sq2x2, slot2 = sq2x2 };
            sc.finisherBatch = new ScenarioBatch { slot0 = sq2x2, slot1 = sq2x2, slot2 = sq2x2 };
            scenarioDb.scenarios.Add(sc);
        }

        testConfig.scenarioDatabase = scenarioDb;

        Assert.AreEqual(33, BlockSpawnGenerator.TotalScenarios, "TotalScenarios should be 33.");

        for (int i = 0; i < BlockSpawnGenerator.TotalScenarios; i++)
        {
            BlockModel[] setup = BlockSpawnGenerator.GenerateScenarioSetup(scenarioDb.scenarios[i]);
            Assert.IsNotNull(setup, $"Scenario {i} setup should not be null.");
            Assert.AreEqual(3, setup.Length, $"Scenario {i} setup must yield exactly 3 blocks.");
            Assert.IsNotNull(setup[0], $"Scenario {i} block 0 should not be null.");
            Assert.IsNotNull(setup[1], $"Scenario {i} block 1 should not be null.");
            Assert.IsNotNull(setup[2], $"Scenario {i} block 2 should not be null.");
        }

        for (int i = 0; i < scenarioDb.scenarios.Count; i++) UnityEngine.Object.DestroyImmediate(scenarioDb.scenarios[i]);
        UnityEngine.Object.DestroyImmediate(scenarioDb);
        UnityEngine.Object.DestroyImmediate(sq2x2);
    }

    [Test]
    public void Scenario_RotationAndMirror_TransformsOffsetsCorrectly()
    {
        ShapeData line2 = ScriptableObject.CreateInstance<ShapeData>();
        line2.baseOffsets = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0) };

        // 90 degree clockwise rotation of horizontal line2 -> should become vertical
        BlockModel rotated = BlockSpawnGenerator.CreateTransformedBlockModel(line2, 90, false);
        Assert.IsNotNull(rotated);
        Assert.AreEqual(2, rotated.ShapeOffsets.Count);
        Assert.IsTrue(rotated.ShapeOffsets.Contains((0, 0)));
        Assert.IsTrue(rotated.ShapeOffsets.Contains((0, 1)));

        UnityEngine.Object.DestroyImmediate(line2);
    }

    [Test]
    public void Scenario_IsBoardMatchingScenario_SupportsRotatedTargetBoard()
    {
        ScenarioData scenario = ScriptableObject.CreateInstance<ScenarioData>();
        scenario.allowRotation = true;
        scenario.matchTolerance = 0;
        scenario.targetBoard = new bool[64];
        scenario.targetBoard[0] = true; // (0, 0) in base

        // When rotated 90 degrees clockwise in 8x8, (x=0, y=0) -> (newX=y=0, newY=7-x=7) -> (0, 7)
        var rotatedGrid = new MockDeviatedGrid(); // has cell at (0, 7)

        bool match90 = BlockSpawnGenerator.IsBoardMatchingScenario(scenario, rotatedGrid, 90, false);
        // Note: MockDeviatedGrid has (0,7) and (1,7), so tolerance=0 will fail on (1,7), but tolerance=1 matches!
        scenario.matchTolerance = 1;
        bool matchWithTolerance = BlockSpawnGenerator.IsBoardMatchingScenario(scenario, rotatedGrid, 90, false);
        Assert.IsTrue(matchWithTolerance, "Board should match scenario rotated by 90 degrees with tolerance 1.");

        UnityEngine.Object.DestroyImmediate(scenario);
    }

    private class MockCrossNearFullGrid : IGridService
    {
        public int GridWidth => 8;
        public int GridHeight => 8;
        public int OccupiedCellCount => 12;
        public float OccupancyRatio => 12f / 64f;

#pragma warning disable CS0067
        public event Action<bool, List<CellPlacementData>> OnPreviewStateChanged;
        public event Action<List<CellPlacementData>> OnBlockPlaced;
        public event Action<List<int>, List<int>> OnLinesCleared;
        public event Action<List<int>, List<int>> OnPreviewLinesToClear;
        public event Action<int, int, bool> OnPlacementResolved;
#pragma warning restore CS0067

        public Vector2Int GetGridPositionFromWorld(Vector3 worldPos) => Vector2Int.zero;
        public Vector3 GetWorldPositionFromGrid(Vector2Int gridPos) => Vector3.zero;
        public void RequestPreview(List<CellPlacementData> cells) { }
        public void RequestPreview(List<Vector2Int> gridPositions) { }
        public void PlaceBlocks(List<CellPlacementData> cells) { }
        public void PlaceBlocks(List<Vector2Int> gridPositions) { }

        public bool CanPlaceBlocks(List<CellPlacementData> cells) => true;

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
        public event Action<bool, List<CellPlacementData>> OnPreviewStateChanged;
        public event Action<List<CellPlacementData>> OnBlockPlaced;
        public event Action<List<int>, List<int>> OnLinesCleared;
        public event Action<List<int>, List<int>> OnPreviewLinesToClear;
        public event Action<int, int, bool> OnPlacementResolved;
#pragma warning restore CS0067

        public Vector2Int GetGridPositionFromWorld(Vector3 worldPos) => Vector2Int.zero;
        public Vector3 GetWorldPositionFromGrid(Vector2Int gridPos) => Vector3.zero;
        public void RequestPreview(List<CellPlacementData> cells) { }
        public void RequestPreview(List<Vector2Int> gridPositions) { }
        public void PlaceBlocks(List<CellPlacementData> cells) { }
        public void PlaceBlocks(List<Vector2Int> gridPositions) { }

        public bool CanPlaceBlocks(List<CellPlacementData> cells) => true;

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
        public event Action<bool, List<CellPlacementData>> OnPreviewStateChanged;
        public event Action<List<CellPlacementData>> OnBlockPlaced;
        public event Action<List<int>, List<int>> OnLinesCleared;
        public event Action<List<int>, List<int>> OnPreviewLinesToClear;
        public event Action<int, int, bool> OnPlacementResolved;
#pragma warning restore CS0067

        public Vector2Int GetGridPositionFromWorld(Vector3 worldPos) => Vector2Int.zero;
        public Vector3 GetWorldPositionFromGrid(Vector2Int gridPos) => Vector3.zero;
        public void RequestPreview(List<CellPlacementData> cells) { }
        public void RequestPreview(List<Vector2Int> gridPositions) { }
        public void PlaceBlocks(List<CellPlacementData> cells) { }
        public void PlaceBlocks(List<Vector2Int> gridPositions) { }

        public bool CanPlaceBlocks(List<CellPlacementData> cells) => true;

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
        public event Action<bool, List<CellPlacementData>> OnPreviewStateChanged;
        public event Action<List<CellPlacementData>> OnBlockPlaced;
        public event Action<List<int>, List<int>> OnLinesCleared;
        public event Action<List<int>, List<int>> OnPreviewLinesToClear;
        public event Action<int, int, bool> OnPlacementResolved;
#pragma warning restore CS0067

        public Vector2Int GetGridPositionFromWorld(Vector3 worldPos) => Vector2Int.zero;
        public Vector3 GetWorldPositionFromGrid(Vector2Int gridPos) => Vector3.zero;
        public void RequestPreview(List<CellPlacementData> cells) { }
        public void RequestPreview(List<Vector2Int> gridPositions) { }
        public void PlaceBlocks(List<CellPlacementData> cells) { }
        public void PlaceBlocks(List<Vector2Int> gridPositions) { }

        public bool CanPlaceBlocks(List<CellPlacementData> cells) => true;

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
        public event Action<bool, List<CellPlacementData>> OnPreviewStateChanged;
        public event Action<List<CellPlacementData>> OnBlockPlaced;
        public event Action<List<int>, List<int>> OnLinesCleared;
        public event Action<List<int>, List<int>> OnPreviewLinesToClear;
        public event Action<int, int, bool> OnPlacementResolved;
#pragma warning restore CS0067

        public Vector2Int GetGridPositionFromWorld(Vector3 worldPos) => Vector2Int.zero;
        public Vector3 GetWorldPositionFromGrid(Vector2Int gridPos) => Vector3.zero;
        public void RequestPreview(List<CellPlacementData> cells) { }
        public void RequestPreview(List<Vector2Int> gridPositions) { }
        public void PlaceBlocks(List<CellPlacementData> cells) { }
        public void PlaceBlocks(List<Vector2Int> gridPositions) { }

        public bool CanPlaceBlocks(List<CellPlacementData> cells) => true;

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

    // ==========================================
    // SCORE SYSTEM & INTEGRATION TESTS
    // ==========================================

    [Test]
    public void Score_Placement_AddsTileCountScore_WithoutAffectingCombo()
    {
        ScoreConfiguration config = ScriptableObject.CreateInstance<ScoreConfiguration>();
        config.pointsPerTile = 1;
        config.highScoreSaveKey = "TEST_SCORE_" + Guid.NewGuid().ToString("N");

        GameObject go = new GameObject("ScoreControllerTest");
        ScoreController controller = go.AddComponent<ScoreController>();
        MockScoreTestGrid grid = new MockScoreTestGrid();
        controller.Initialize(config, grid);

        int scoreEventCurrent = -1, scoreEventGained = -1;
        controller.OnScoreChanged += (curr, gained) =>
        {
            scoreEventCurrent = curr;
            scoreEventGained = gained;
        };

        // Place 4 tiles, 0 lines cleared, not all clear
        controller.HandlePlacementResolved(4, 0, false);

        Assert.AreEqual(4, controller.CurrentScore);
        Assert.AreEqual(0, controller.CurrentCombo);
        Assert.AreEqual(4, scoreEventCurrent);
        Assert.AreEqual(4, scoreEventGained);

        PlayerPrefs.DeleteKey(config.highScoreSaveKey);
        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(config);
    }

    [Test]
    public void Score_Prewarm_FirstClear_TriggersPrewarm_WithoutCombo()
    {
        ScoreConfiguration config = ScriptableObject.CreateInstance<ScoreConfiguration>();
        config.pointsPerTile = 1;
        config.points1Line = 10;
        config.comboBonusStep = 10;
        config.prewarmWindowTurns = 3;
        config.highScoreSaveKey = "TEST_SCORE_" + Guid.NewGuid().ToString("N");

        GameObject go = new GameObject("ScoreControllerTest");
        ScoreController controller = go.AddComponent<ScoreController>();
        MockScoreTestGrid grid = new MockScoreTestGrid();
        controller.Initialize(config, grid);

        int comboEventFired = -1;
        controller.OnComboChanged += c => comboEventFired = c;

        // Place 2 tiles, clear 1 line (First clear = Prewarm/Nổ mồi)
        // Expected: 2 (placement) + 10 (1 line) = 12 (NO combo bonus yet!)
        controller.HandlePlacementResolved(2, 1, false);

        Assert.AreEqual(12, controller.CurrentScore);
        Assert.AreEqual(0, controller.CurrentCombo, "Prewarm does not grant a combo yet (Option A).");
        Assert.IsTrue(controller.IsPrewarmed, "Board should enter prewarmed state.");
        Assert.AreEqual(3, controller.TurnsRemaining);
        Assert.AreEqual(-1, comboEventFired, "Combo event should not fire when entering prewarm (still combo 0).");

        PlayerPrefs.DeleteKey(config.highScoreSaveKey);
        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(config);
    }

    [Test]
    public void Score_Prewarm_ClearWithinWindow_IgnitesComboAndAwardsBonus()
    {
        ScoreConfiguration config = ScriptableObject.CreateInstance<ScoreConfiguration>();
        config.pointsPerTile = 1;
        config.points1Line = 10;
        config.points2Lines = 30;
        config.comboBonusStep = 10;
        config.prewarmWindowTurns = 3;
        config.comboGraceTurns = 3;
        config.highScoreSaveKey = "TEST_SCORE_" + Guid.NewGuid().ToString("N");

        GameObject go = new GameObject("ScoreControllerTest");
        ScoreController controller = go.AddComponent<ScoreController>();
        MockScoreTestGrid grid = new MockScoreTestGrid();
        controller.Initialize(config, grid);

        // Turn 1: 1 line cleared -> Prewarm (score: 1 tile + 10 = 11)
        controller.HandlePlacementResolved(1, 1, false);
        Assert.IsTrue(controller.IsPrewarmed);
        Assert.AreEqual(0, controller.CurrentCombo);
        Assert.AreEqual(11, controller.CurrentScore);

        // Turn 2: Non-clearing turn -> consumes 1 turn of prewarm window
        controller.HandlePlacementResolved(2, 0, false);
        Assert.IsTrue(controller.IsPrewarmed);
        Assert.AreEqual(2, controller.TurnsRemaining);

        // Turn 3: 2 lines cleared simultaneously -> Ignites combo!
        // CurrentCombo = 2 lines
        // Points: 3 (tiles) + 30 (2 lines) + 20 (combo 2 * 10) = 53
        // Total score: 11 + 2 + 53 = 66
        controller.HandlePlacementResolved(3, 2, false);
        Assert.IsFalse(controller.IsPrewarmed);
        Assert.AreEqual(2, controller.CurrentCombo);
        Assert.AreEqual(3, controller.TurnsRemaining);
        Assert.AreEqual(66, controller.CurrentScore);

        PlayerPrefs.DeleteKey(config.highScoreSaveKey);
        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(config);
    }

    [Test]
    public void Score_Prewarm_ExpiresAfter3NonClearingTurns()
    {
        ScoreConfiguration config = ScriptableObject.CreateInstance<ScoreConfiguration>();
        config.pointsPerTile = 1;
        config.points1Line = 10;
        config.prewarmWindowTurns = 3;
        config.highScoreSaveKey = "TEST_SCORE_" + Guid.NewGuid().ToString("N");

        GameObject go = new GameObject("ScoreControllerTest");
        ScoreController controller = go.AddComponent<ScoreController>();
        MockScoreTestGrid grid = new MockScoreTestGrid();
        controller.Initialize(config, grid);

        // Turn 1: Prewarm (1 line)
        controller.HandlePlacementResolved(1, 1, false);
        Assert.IsTrue(controller.IsPrewarmed);
        Assert.AreEqual(3, controller.TurnsRemaining);

        // Turn 2 (Miss 1)
        controller.HandlePlacementResolved(1, 0, false);
        Assert.IsTrue(controller.IsPrewarmed);
        Assert.AreEqual(2, controller.TurnsRemaining);

        // Turn 3 (Miss 2)
        controller.HandlePlacementResolved(1, 0, false);
        Assert.IsTrue(controller.IsPrewarmed);
        Assert.AreEqual(1, controller.TurnsRemaining);

        // Turn 4 (Miss 3) -> Prewarm lost!
        controller.HandlePlacementResolved(1, 0, false);
        Assert.IsFalse(controller.IsPrewarmed, "Prewarm must expire after 3 non-clearing placements.");
        Assert.AreEqual(0, controller.TurnsRemaining);
        Assert.AreEqual(0, controller.CurrentCombo);

        // Turn 5: Clears 1 line again -> Must be a NEW Prewarm, not a combo!
        controller.HandlePlacementResolved(1, 1, false);
        Assert.IsTrue(controller.IsPrewarmed, "Must restart prewarm cycle.");
        Assert.AreEqual(0, controller.CurrentCombo);

        PlayerPrefs.DeleteKey(config.highScoreSaveKey);
        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(config);
    }

    [Test]
    public void Score_ActiveCombo_AccumulatesLinesAndRefreshesCounter()
    {
        ScoreConfiguration config = ScriptableObject.CreateInstance<ScoreConfiguration>();
        config.pointsPerTile = 1;
        config.points1Line = 10;
        config.points2Lines = 30;
        config.points3Lines = 60;
        config.comboBonusStep = 10;
        config.prewarmWindowTurns = 3;
        config.comboGraceTurns = 3;
        config.highScoreSaveKey = "TEST_SCORE_" + Guid.NewGuid().ToString("N");

        GameObject go = new GameObject("ScoreControllerTest");
        ScoreController controller = go.AddComponent<ScoreController>();
        MockScoreTestGrid grid = new MockScoreTestGrid();
        controller.Initialize(config, grid);

        // Turn 1: Prewarm
        controller.HandlePlacementResolved(1, 1, false);
        Assert.AreEqual(0, controller.CurrentCombo);

        // Turn 2: 3 lines cleared -> Combo is 3
        controller.HandlePlacementResolved(3, 3, false);
        Assert.AreEqual(3, controller.CurrentCombo);
        Assert.AreEqual(3, controller.TurnsRemaining);

        // Turn 3: 2 lines cleared -> Combo accumulates to 3 + 2 = 5
        controller.HandlePlacementResolved(2, 2, false);
        Assert.AreEqual(5, controller.CurrentCombo);
        Assert.AreEqual(5, controller.MaxCombo);
        Assert.AreEqual(3, controller.TurnsRemaining);

        PlayerPrefs.DeleteKey(config.highScoreSaveKey);
        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(config);
    }

    [Test]
    public void Score_ActiveCombo_GraceTurns_MaintainsComboUntilThirdMiss()
    {
        ScoreConfiguration config = ScriptableObject.CreateInstance<ScoreConfiguration>();
        config.pointsPerTile = 1;
        config.points1Line = 10;
        config.comboBonusStep = 10;
        config.prewarmWindowTurns = 3;
        config.comboGraceTurns = 3;
        config.highScoreSaveKey = "TEST_SCORE_" + Guid.NewGuid().ToString("N");

        GameObject go = new GameObject("ScoreControllerTest");
        ScoreController controller = go.AddComponent<ScoreController>();
        MockScoreTestGrid grid = new MockScoreTestGrid();
        controller.Initialize(config, grid);

        // Turn 1: Prewarm
        controller.HandlePlacementResolved(1, 1, false);

        // Turn 2: 1 line clear -> Combo = 1
        controller.HandlePlacementResolved(1, 1, false);
        Assert.AreEqual(1, controller.CurrentCombo);
        Assert.AreEqual(3, controller.TurnsRemaining);

        int comboResetCount = 0;
        controller.OnComboChanged += c =>
        {
            if (c == 0) comboResetCount++;
        };

        // Turn 3 (Miss 1) -> Combo stays 1
        controller.HandlePlacementResolved(1, 0, false);
        Assert.AreEqual(1, controller.CurrentCombo);
        Assert.AreEqual(2, controller.TurnsRemaining);
        Assert.AreEqual(0, comboResetCount);

        // Turn 4 (Miss 2) -> Combo stays 1
        controller.HandlePlacementResolved(1, 0, false);
        Assert.AreEqual(1, controller.CurrentCombo);
        Assert.AreEqual(1, controller.TurnsRemaining);
        Assert.AreEqual(0, comboResetCount);

        // Turn 5 (Miss 3) -> Combo drops to 0!
        controller.HandlePlacementResolved(1, 0, false);
        Assert.AreEqual(0, controller.CurrentCombo);
        Assert.AreEqual(0, controller.TurnsRemaining);
        Assert.AreEqual(1, comboResetCount);
        Assert.AreEqual(1, controller.MaxCombo);

        PlayerPrefs.DeleteKey(config.highScoreSaveKey);
        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(config);
    }

    [Test]
    public void Score_ActiveCombo_ClearWithinGracePeriod_ResetsCounter()
    {
        ScoreConfiguration config = ScriptableObject.CreateInstance<ScoreConfiguration>();
        config.pointsPerTile = 1;
        config.points1Line = 10;
        config.points2Lines = 30;
        config.comboBonusStep = 10;
        config.prewarmWindowTurns = 3;
        config.comboGraceTurns = 3;
        config.highScoreSaveKey = "TEST_SCORE_" + Guid.NewGuid().ToString("N");

        GameObject go = new GameObject("ScoreControllerTest");
        ScoreController controller = go.AddComponent<ScoreController>();
        MockScoreTestGrid grid = new MockScoreTestGrid();
        controller.Initialize(config, grid);

        // Turn 1: Prewarm
        controller.HandlePlacementResolved(1, 1, false);

        // Turn 2: 1 line clear -> Combo = 1
        controller.HandlePlacementResolved(1, 1, false);
        Assert.AreEqual(1, controller.CurrentCombo);

        // Turn 3 (Miss 1)
        controller.HandlePlacementResolved(1, 0, false);
        Assert.AreEqual(2, controller.TurnsRemaining);

        // Turn 4 (Miss 2)
        controller.HandlePlacementResolved(1, 0, false);
        Assert.AreEqual(1, controller.TurnsRemaining);

        // Turn 5: Clear 2 lines before reaching miss 3!
        // Combo should accumulate: 1 + 2 = 3, turnsRemaining reset to 3!
        controller.HandlePlacementResolved(2, 2, false);
        Assert.AreEqual(3, controller.CurrentCombo);
        Assert.AreEqual(3, controller.TurnsRemaining);

        PlayerPrefs.DeleteKey(config.highScoreSaveKey);
        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(config);
    }

    [Test]
    public void Score_AllClear_GrantsLargeBonus()
    {
        ScoreConfiguration config = ScriptableObject.CreateInstance<ScoreConfiguration>();
        config.pointsPerTile = 1;
        config.points1Line = 10;
        config.comboBonusStep = 10;
        config.allClearBonus = 300;
        config.prewarmWindowTurns = 3;
        config.comboGraceTurns = 3;
        config.highScoreSaveKey = "TEST_SCORE_" + Guid.NewGuid().ToString("N");

        GameObject go = new GameObject("ScoreControllerTest");
        ScoreController controller = go.AddComponent<ScoreController>();
        MockScoreTestGrid grid = new MockScoreTestGrid();
        controller.Initialize(config, grid);

        // Turn 1: Prewarm (1 line) -> Score: 1 + 10 = 11
        controller.HandlePlacementResolved(1, 1, false);

        // Turn 2: Place 2 tiles, clear 1 line, and board is completely emptied (All Clear)
        // Expected: 2 (placement) + 10 (1 line) + 10 (combo 1) + 300 (All Clear) = 322
        // Total score: 11 + 322 = 333
        controller.HandlePlacementResolved(2, 1, true);

        Assert.AreEqual(333, controller.CurrentScore);
        Assert.AreEqual(1, controller.CurrentCombo);

        PlayerPrefs.DeleteKey(config.highScoreSaveKey);
        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(config);
    }

    [Test]
    public void Score_AllClear_MultipliesBonusByCombo()
    {
        ScoreConfiguration config = ScriptableObject.CreateInstance<ScoreConfiguration>();
        config.pointsPerTile = 1;
        config.points2Lines = 30;
        config.comboBonusStep = 10;
        config.allClearBonus = 300;
        config.prewarmWindowTurns = 3;
        config.comboGraceTurns = 3;
        config.highScoreSaveKey = "TEST_SCORE_" + Guid.NewGuid().ToString("N");

        GameObject go = new GameObject("ScoreControllerTest");
        ScoreController controller = go.AddComponent<ScoreController>();
        MockScoreTestGrid grid = new MockScoreTestGrid();
        controller.Initialize(config, grid);

        // Turn 1: Prewarm (1 line) -> Score: 1 + 10 = 11
        controller.HandlePlacementResolved(1, 1, false);

        // Turn 2: Clear 2 lines simultaneously with All Clear -> Combo becomes 2!
        // Expected: 2 (placement) + 30 (2 lines) + 20 (combo 2 * 10) + (300 * 2) (All Clear * combo 2) = 652
        // Total score: 11 + 652 = 663
        controller.HandlePlacementResolved(2, 2, true);

        Assert.AreEqual(2, controller.CurrentCombo);
        Assert.AreEqual(663, controller.CurrentScore);

        PlayerPrefs.DeleteKey(config.highScoreSaveKey);
        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(config);
    }

    [Test]
    public void Score_HighScore_UpdatesAndPersists_WhenExceeded()
    {
        ScoreConfiguration config = ScriptableObject.CreateInstance<ScoreConfiguration>();
        config.pointsPerTile = 1;
        config.highScoreSaveKey = "TEST_SCORE_" + Guid.NewGuid().ToString("N");

        GameObject go = new GameObject("ScoreControllerTest");
        ScoreController controller = go.AddComponent<ScoreController>();
        MockScoreTestGrid grid = new MockScoreTestGrid();
        controller.Initialize(config, grid);

        int newHighFired = -1;
        controller.OnHighScoreChanged += high => newHighFired = high;

        controller.HandlePlacementResolved(50, 0, false);

        Assert.AreEqual(50, controller.CurrentScore);
        Assert.AreEqual(50, controller.HighScore);
        Assert.AreEqual(50, newHighFired);
        Assert.AreEqual(50, PlayerPrefs.GetInt(config.highScoreSaveKey));

        PlayerPrefs.DeleteKey(config.highScoreSaveKey);
        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(config);
    }

    [Test]
    public void Score_ResetScore_ResetsCurrentScoreAndCombo_RetainsHighScore()
    {
        ScoreConfiguration config = ScriptableObject.CreateInstance<ScoreConfiguration>();
        config.pointsPerTile = 1;
        config.highScoreSaveKey = "TEST_SCORE_" + Guid.NewGuid().ToString("N");

        GameObject go = new GameObject("ScoreControllerTest");
        ScoreController controller = go.AddComponent<ScoreController>();
        MockScoreTestGrid grid = new MockScoreTestGrid();
        controller.Initialize(config, grid);

        controller.HandlePlacementResolved(100, 0, false);
        Assert.AreEqual(100, controller.CurrentScore);
        Assert.AreEqual(100, controller.HighScore);

        controller.ResetScore();
        Assert.AreEqual(0, controller.CurrentScore);
        Assert.AreEqual(0, controller.CurrentCombo);
        Assert.AreEqual(100, controller.HighScore, "ResetScore must retain the existing HighScore.");

        PlayerPrefs.DeleteKey(config.highScoreSaveKey);
        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(config);
    }

    [Test]
    public void Score_GridEvent_OnPlacementResolved_InvokesScoreCalculation()
    {
        ScoreConfiguration config = ScriptableObject.CreateInstance<ScoreConfiguration>();
        config.pointsPerTile = 1;
        config.points1Line = 10;
        config.comboBonusStep = 10;
        config.highScoreSaveKey = "TEST_SCORE_" + Guid.NewGuid().ToString("N");

        GameObject go = new GameObject("ScoreControllerTest");
        ScoreController controller = go.AddComponent<ScoreController>();
        MockScoreTestGrid grid = new MockScoreTestGrid();
        controller.Initialize(config, grid);

        // Turn 1: Prewarm (3 tiles, 1 line) -> 3 + 10 = 13 (combo 0, isPrewarmed = true)
        grid.TriggerPlacementResolved(3, 1, false);
        Assert.AreEqual(13, controller.CurrentScore);
        Assert.AreEqual(0, controller.CurrentCombo);
        Assert.IsTrue(controller.IsPrewarmed);

        // Turn 2: Combo ignition (2 tiles, 1 line) -> 2 + 10 + 10 (combo 1) = 22
        // Total score: 13 + 22 = 35
        grid.TriggerPlacementResolved(2, 1, false);
        Assert.AreEqual(35, controller.CurrentScore);
        Assert.AreEqual(1, controller.CurrentCombo);

        PlayerPrefs.DeleteKey(config.highScoreSaveKey);
        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(config);
    }

    [Test]
    public void ScoreView_ComponentCanBeInstantiated()
    {
        GameObject go = new GameObject("ScoreViewTest");
        ScoreView view = go.AddComponent<ScoreView>();
        Assert.IsNotNull(view);
        UnityEngine.Object.DestroyImmediate(go);
    }

    private class MockScoreTestGrid : IGridService
    {
        public int GridWidth => 8;
        public int GridHeight => 8;
        public int OccupiedCellCount => 0;
        public float OccupancyRatio => 0f;

#pragma warning disable CS0067
        public event Action<bool, List<CellPlacementData>> OnPreviewStateChanged;
        public event Action<List<CellPlacementData>> OnBlockPlaced;
        public event Action<List<int>, List<int>> OnLinesCleared;
        public event Action<List<int>, List<int>> OnPreviewLinesToClear;
#pragma warning restore CS0067
        public event Action<int, int, bool> OnPlacementResolved;

        public void TriggerPlacementResolved(int tiles, int lines, bool allClear)
        {
            OnPlacementResolved?.Invoke(tiles, lines, allClear);
        }

        public Vector2Int GetGridPositionFromWorld(Vector3 worldPos) => Vector2Int.zero;
        public Vector3 GetWorldPositionFromGrid(Vector2Int gridPos) => Vector3.zero;
        public bool CanPlaceBlocks(List<CellPlacementData> cells) => true;
        public bool CanPlaceBlocks(List<Vector2Int> gridPositions) => true;
        public bool IsCellOccupied(int col, int row) => false;
        public void RequestPreview(List<CellPlacementData> cells) { }
        public void RequestPreview(List<Vector2Int> gridPositions) { }
        public void PlaceBlocks(List<CellPlacementData> cells) { }
        public void PlaceBlocks(List<Vector2Int> gridPositions) { }
    }
}


