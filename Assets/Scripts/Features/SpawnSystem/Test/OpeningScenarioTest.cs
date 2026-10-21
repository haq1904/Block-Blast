using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class OpeningScenarioTest
{
    private SpawnConfiguration testConfig;
    private OpeningScenarioDatabase testDatabase;
    private ShapeDatabase testShapeDb;

    [SetUp]
    public void SetUp()
    {
        testDatabase = AssetDatabase.LoadAssetAtPath<OpeningScenarioDatabase>(
            "Assets/ScriptableObjects/OpeningScenario/OpeningScenarioDatabase.asset");

        testShapeDb = AssetDatabase.LoadAssetAtPath<ShapeDatabase>(
            "Assets/ScriptableObjects/Shapes/MasterShapeDatabase.asset");

        testConfig = ScriptableObject.CreateInstance<SpawnConfiguration>();
        testConfig.shapeDatabase = testShapeDb;
        testConfig.openingScenarioDatabase = testDatabase;
        testConfig.prefilledOpeningChance = 0.30f;
        testConfig.enableScenarioChains = true;
        testConfig.enableMercyMode = true;
    }

    [TearDown]
    public void TearDown()
    {
        if (testConfig != null)
        {
            Object.DestroyImmediate(testConfig);
        }
    }

    [Test]
    public void Selector_Boundary_30Percent_SelectsIntelligentOnly_WhenRollLessThanChance()
    {
        testConfig.prefilledOpeningChance = 0.30f;

        var mode0 = OpeningScenarioSelector.SelectMode(testConfig, 0.0f);
        var mode1 = OpeningScenarioSelector.SelectMode(testConfig, 0.15f);
        var mode2 = OpeningScenarioSelector.SelectMode(testConfig, 0.299f);

        Assert.AreEqual(OpeningGenerationMode.IntelligentOnly, mode0);
        Assert.AreEqual(OpeningGenerationMode.IntelligentOnly, mode1);
        Assert.AreEqual(OpeningGenerationMode.IntelligentOnly, mode2);
    }

    [Test]
    public void Selector_Boundary_30Percent_SelectsLegacy_WhenRollGreaterOrEqualChance()
    {
        testConfig.prefilledOpeningChance = 0.30f;

        var mode0 = OpeningScenarioSelector.SelectMode(testConfig, 0.30f);
        var mode1 = OpeningScenarioSelector.SelectMode(testConfig, 0.50f);
        var mode2 = OpeningScenarioSelector.SelectMode(testConfig, 0.99f);

        Assert.AreEqual(OpeningGenerationMode.LegacyScenarioChain, mode0);
        Assert.AreEqual(OpeningGenerationMode.LegacyScenarioChain, mode1);
        Assert.AreEqual(OpeningGenerationMode.LegacyScenarioChain, mode2);
    }

    [Test]
    public void Selector_FallbackToLegacy_WhenConfigOrDatabaseNullOrEmpty()
    {
        // Null config
        Assert.AreEqual(OpeningGenerationMode.LegacyScenarioChain,
            OpeningScenarioSelector.SelectMode(null, 0.10f));

        // Null database
        testConfig.openingScenarioDatabase = null;
        Assert.AreEqual(OpeningGenerationMode.LegacyScenarioChain,
            OpeningScenarioSelector.SelectMode(testConfig, 0.10f));

        // Chance <= 0
        testConfig.openingScenarioDatabase = testDatabase;
        testConfig.prefilledOpeningChance = 0f;
        Assert.AreEqual(OpeningGenerationMode.LegacyScenarioChain,
            OpeningScenarioSelector.SelectMode(testConfig, 0.0f));

        // Empty database
        var emptyDb = ScriptableObject.CreateInstance<OpeningScenarioDatabase>();
        testConfig.openingScenarioDatabase = emptyDb;
        testConfig.prefilledOpeningChance = 0.30f;
        Assert.AreEqual(OpeningGenerationMode.LegacyScenarioChain,
            OpeningScenarioSelector.SelectMode(testConfig, 0.10f));
        Object.DestroyImmediate(emptyDb);
    }

    [Test]
    public void Database_Inventory_ContainsExact10ValidScenarios()
    {
        Assert.IsNotNull(testDatabase, "OpeningScenarioDatabase asset must exist.");
        Assert.AreEqual(10, testDatabase.scenarios.Count, "Database must contain exactly 10 scenarios.");

        var seenNames = new HashSet<string>();
        for (int i = 0; i < testDatabase.scenarios.Count; i++)
        {
            var s = testDatabase.scenarios[i];
            Assert.IsNotNull(s, $"Scenario at index {i} cannot be null.");
            Assert.IsTrue(s.IsValidLayout(), $"Scenario '{s.scenarioName}' must be a valid layout.");
            Assert.IsTrue(seenNames.Add(s.scenarioName), $"Scenario name '{s.scenarioName}' must be unique.");
        }
    }

    [Test]
    public void Layout_Occupancy_WithinRequiredRange()
    {
        foreach (var s in testDatabase.scenarios)
        {
            int count = s.OccupiedCellCount;
            Assert.GreaterOrEqual(count, 32, $"Scenario '{s.scenarioName}' must have at least 32 cells (50%).");
            Assert.LessOrEqual(count, 44, $"Scenario '{s.scenarioName}' must have at most 44 cells (68.75%).");
        }
    }

    [Test]
    public void Layout_Invariants_NoFullRowOrColumn()
    {
        foreach (var s in testDatabase.scenarios)
        {
            for (int r = 0; r < 8; r++)
            {
                int rowCount = 0;
                for (int c = 0; c < 8; c++)
                {
                    if (s.GetCell(c, r)) rowCount++;
                }
                Assert.AreNotEqual(8, rowCount, $"Scenario '{s.scenarioName}' has pre-filled full row {r}.");
            }

            for (int c = 0; c < 8; c++)
            {
                int colCount = 0;
                for (int r = 0; r < 8; r++)
                {
                    if (s.GetCell(c, r)) colCount++;
                }
                Assert.AreNotEqual(8, colCount, $"Scenario '{s.scenarioName}' has pre-filled full col {c}.");
            }
        }
    }

    [Test]
    public void Transform_Invariants_PreservesOccupiedCountAndBounds()
    {
        int[] angles = { 0, 90, 180, 270 };
        bool[] mirrors = { false, true };

        foreach (var s in testDatabase.scenarios)
        {
            int expectedCount = s.OccupiedCellCount;

            foreach (int angle in angles)
            {
                foreach (bool mirror in mirrors)
                {
                    List<Vector2Int> cells = s.GetOccupiedCells(angle, mirror);
                    Assert.AreEqual(expectedCount, cells.Count,
                        $"Transform (angle={angle}, mirror={mirror}) altered cell count on '{s.scenarioName}'.");

                    var unique = new HashSet<Vector2Int>();
                    foreach (var pos in cells)
                    {
                        Assert.GreaterOrEqual(pos.x, 0, "Cell x must be >= 0.");
                        Assert.Less(pos.x, 8, "Cell x must be < 8.");
                        Assert.GreaterOrEqual(pos.y, 0, "Cell y must be >= 0.");
                        Assert.Less(pos.y, 8, "Cell y must be < 8.");
                        Assert.IsTrue(unique.Add(pos), $"Duplicate coordinate ({pos.x}, {pos.y}) after transform.");
                    }
                }
            }
        }
    }

    [Test]
    public void Transform_Invariants_Produces8DistinctD4VariantsPerTemplate()
    {
        int[] angles = { 0, 90, 180, 270 };
        bool[] mirrors = { false, true };

        foreach (var s in testDatabase.scenarios)
        {
            var distinctVariants = new HashSet<string>();

            foreach (int angle in angles)
            {
                foreach (bool mirror in mirrors)
                {
                    List<Vector2Int> cells = s.GetOccupiedCells(angle, mirror);
                    var sortedCoords = new List<string>(cells.Count);
                    foreach (var c in cells)
                    {
                        sortedCoords.Add($"{c.x},{c.y}");
                    }
                    sortedCoords.Sort();
                    distinctVariants.Add(string.Join(";", sortedCoords));
                }
            }

            Assert.AreEqual(8, distinctVariants.Count,
                $"Scenario '{s.scenarioName}' should produce exactly 8 distinct D4 variants under rotation & mirroring.");
        }
    }

    [Test]
    public void Tier2_ComplexityBudget_MeetsTopologyRequirements()
    {
        Assert.GreaterOrEqual(testDatabase.scenarios.Count, 10);

        int[] dc = { 1, -1, 0, 0 };
        int[] dr = { 0, 0, 1, -1 };

        for (int i = 5; i < 10; i++)
        {
            var s = testDatabase.scenarios[i];
            bool[,] grid = new bool[8, 8];
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    grid[c, r] = s.GetCell(c, r);
                }
            }

            // 1. Transition edges
            int transitions = 0;
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 7; c++)
                {
                    if (grid[c, r] != grid[c + 1, r]) transitions++;
                }
            }
            for (int c = 0; c < 8; c++)
            {
                for (int r = 0; r < 7; r++)
                {
                    if (grid[c, r] != grid[c, r + 1]) transitions++;
                }
            }

            // 2. Longest straight run of occupied cells
            int longestRun = 0;
            for (int r = 0; r < 8; r++)
            {
                int cur = 0;
                for (int c = 0; c < 8; c++)
                {
                    if (grid[c, r]) { cur++; if (cur > longestRun) longestRun = cur; }
                    else cur = 0;
                }
            }
            for (int c = 0; c < 8; c++)
            {
                int cur = 0;
                for (int r = 0; r < 8; r++)
                {
                    if (grid[c, r]) { cur++; if (cur > longestRun) longestRun = cur; }
                    else cur = 0;
                }
            }

            // 3. 4-connected empty components
            bool[,] visited = new bool[8, 8];
            int emptyComps = 0;
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    if (!grid[c, r] && !visited[c, r])
                    {
                        emptyComps++;
                        var q = new Queue<Vector2Int>();
                        q.Enqueue(new Vector2Int(c, r));
                        visited[c, r] = true;

                        while (q.Count > 0)
                        {
                            var cur = q.Dequeue();
                            for (int d = 0; d < 4; d++)
                            {
                                int nc = cur.x + dc[d];
                                int nr = cur.y + dr[d];
                                if (nc >= 0 && nc < 8 && nr >= 0 && nr < 8 && !visited[nc, nr] && !grid[nc, nr])
                                {
                                    visited[nc, nr] = true;
                                    q.Enqueue(new Vector2Int(nc, nr));
                                }
                            }
                        }
                    }
                }
            }

            Assert.GreaterOrEqual(transitions, 58, $"Scenario '{s.scenarioName}' must have >= 58 transition edges.");
            Assert.LessOrEqual(longestRun, 5, $"Scenario '{s.scenarioName}' longest run must be <= 5.");
            Assert.GreaterOrEqual(emptyComps, 13, $"Scenario '{s.scenarioName}' must have >= 13 empty components.");
        }
    }

    [Test]
    public void SmartBatch_PlayableOnEachLayout()
    {
        var gridGo = new GameObject("TestGrid");
        var gridController = gridGo.AddComponent<GridController>();
        var awakeMethod = typeof(GridController).GetMethod("Awake",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        awakeMethod.Invoke(gridController, null);

        int[] angles = { 0, 90, 180, 270 };
        bool[] mirrors = { false, true };

        try
        {
            foreach (var s in testDatabase.scenarios)
            {
                foreach (int angle in angles)
                {
                    foreach (bool mirror in mirrors)
                    {
                        awakeMethod.Invoke(gridController, null);

                        var placementCells = new List<CellPlacementData>();
                        foreach (var pos in s.GetOccupiedCells(angle, mirror))
                        {
                            placementCells.Add(new CellPlacementData(pos, "test", 0));
                        }

                        bool inited = gridController.TryInitializeBoard(placementCells);
                        Assert.IsTrue(inited, $"Failed to initialize board for '{s.scenarioName}' (angle={angle}, mirror={mirror}).");

                        BlockModel[] batch = BlockSpawnGenerator.GenerateIntelligentBatch(
                            testShapeDb, gridController, testConfig, score: 0);

                        Assert.IsNotNull(batch, $"Batch null for '{s.scenarioName}' (angle={angle}, mirror={mirror}).");
                        Assert.AreEqual(3, batch.Length, $"Batch must have 3 shapes for '{s.scenarioName}'.");

                        bool canPlaceAny = false;
                        for (int i = 0; i < 3; i++)
                        {
                            if (batch[i] != null && BlockSpawnGenerator.CanPlaceBlockAnywhere(batch[i], gridController))
                            {
                                canPlaceAny = true;
                                break;
                            }
                        }

                        Assert.IsTrue(canPlaceAny, $"Opening layout '{s.scenarioName}' (angle={angle}, mirror={mirror}) produced batch with 0 playable moves.");
                    }
                }
            }
        }
        finally
        {
            if (gridGo != null) Object.DestroyImmediate(gridGo);
        }
    }

    [Test]
    public void SpawnModel_GenerationMode_SetsAndTransitionsCorrectly()
    {
        var model = new SpawnModel();
        Assert.AreEqual(OpeningGenerationMode.LegacyScenarioChain, model.GenerationMode);

        model.SetGenerationMode(OpeningGenerationMode.IntelligentOnly);
        Assert.AreEqual(OpeningGenerationMode.IntelligentOnly, model.GenerationMode);

        model.ResetScenario();
        // ResetScenario should NOT alter GenerationMode
        Assert.AreEqual(OpeningGenerationMode.IntelligentOnly, model.GenerationMode);

        model.SetGenerationMode(OpeningGenerationMode.LegacyScenarioChain);
        Assert.AreEqual(OpeningGenerationMode.LegacyScenarioChain, model.GenerationMode);
    }
}
