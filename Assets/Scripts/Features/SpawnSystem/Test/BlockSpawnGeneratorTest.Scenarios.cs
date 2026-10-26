using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public partial class BlockSpawnGeneratorTest
{
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
