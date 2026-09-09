using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static class BlockSpawnGenerator
{
    public const int TotalScenarios = 10;

    public static BlockModel[] GenerateBatch(
        int score,
        IGridService gridService,
        ShapeDatabase shapeDatabase,
        SpawnConfiguration config)
    {
        return GenerateBatch(score, gridService, shapeDatabase, config, null);
    }

    public static BlockModel[] GenerateBatch(
        int score,
        IGridService gridService,
        ShapeDatabase shapeDatabase,
        SpawnConfiguration config,
        SpawnModel spawnModel)
    {
        if (config == null) config = ScriptableObject.CreateInstance<SpawnConfiguration>();

        BlockModel[] batch = new BlockModel[3];
        if (shapeDatabase == null)
        {
            for (int i = 0; i < 3; i++) batch[i] = new BlockModel(new List<(int x, int y)> { (0, 0) });
            return batch;
        }

        bool isUnderThreshold = score < config.comboPhaseScoreThreshold;

        // 1. Scripted Scenarios & Early Progression (Only under threshold)
        if (isUnderThreshold)
        {
            if (config.enableScenarioChains && spawnModel != null)
            {
                BlockModel[] progressionBatch = HandleEarlyGameProgression(score, gridService, shapeDatabase, config, spawnModel);
                if (progressionBatch != null && progressionBatch.Length == 3 && (gridService == null || CanPlaceAny(progressionBatch, gridService)))
                    return progressionBatch;
            }
            else if (config.enableSynergisticBatches && (gridService == null || gridService.OccupiedCellCount == 0))
            {
                BlockModel[] synBatch = TryGenerateSynergisticBatch(score, gridService, shapeDatabase, config);
                if (synBatch != null && synBatch.Length == 3 && (gridService == null || CanPlaceAny(synBatch, gridService)))
                    return synBatch;
            }
        }
        else
        {
            if (spawnModel != null && spawnModel.ActiveScenarioId >= 0)
            {
                spawnModel.ResetScenario();
            }
        }

        // 2. Intelligent Angular Grid Scanner & Shutdown Solver
        BlockModel[] smartBatch = GenerateIntelligentBatch(shapeDatabase, gridService, config, score);
        if (smartBatch != null && smartBatch.Length == 3 && (gridService == null || CanPlaceAny(smartBatch, gridService)))
            return smartBatch;

        // Fallback: guaranteed playable shapes from database
        for (int i = 0; i < 3; i++)
        {
            if (batch[i] == null)
            {
                BlockModel playable = FindPlayableShape(shapeDatabase, gridService);
                batch[i] = playable ?? new BlockModel(new List<(int x, int y)> { (0, 0) });
            }
        }

        return batch;
    }

    public static List<(int x, int y)> NormalizeOffsets(IEnumerable<Vector2Int> offsets)
    {
        List<(int x, int y)> result = new List<(int x, int y)>();
        if (offsets == null) return result;

        int minX = int.MaxValue, minY = int.MaxValue;
        foreach (var v in offsets)
        {
            if (v.x < minX) minX = v.x;
            if (v.y < minY) minY = v.y;
        }

        foreach (var v in offsets) result.Add((v.x - minX, v.y - minY));
        result.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));
        return result;
    }

    public static BlockModel CreateBlockModel(ShapeData data)
    {
        if (data == null) return new BlockModel(new List<(int x, int y)> { (0, 0) });

        List<Vector2Int> unityOffsets = data.canRotate
            ? data.GetRotatedOffsets(Random.Range(0, 4) * 90)
            : data.baseOffsets;

        List<(int x, int y)> pureOffsets = new List<(int x, int y)>(unityOffsets != null ? unityOffsets.Count : 1);
        if (unityOffsets != null)
        {
            for (int i = 0; i < unityOffsets.Count; i++) pureOffsets.Add((unityOffsets[i].x, unityOffsets[i].y));
        }
        else pureOffsets.Add((0, 0));

        return new BlockModel(pureOffsets);
    }

    public static bool CanPlaceAny(BlockModel[] batch, IGridService grid)
    {
        if (batch == null || grid == null) return false;
        for (int i = 0; i < batch.Length; i++)
        {
            if (batch[i] != null && CanPlaceBlockAnywhere(batch[i], grid)) return true;
        }
        return false;
    }

    public static bool CanPlaceBlockAnywhere(BlockModel block, IGridService grid)
    {
        return CanPlaceBlockAnywhere(block, grid, out _, out _);
    }

    public static bool CanPlaceBlockAnywhere(BlockModel block, IGridService grid, out int foundX, out int foundY)
    {
        foundX = -1;
        foundY = -1;
        if (block == null || grid == null) return false;

        var offsets = block.ShapeOffsets;
        int count = offsets.Count;
        List<Vector2Int> testPositions = new List<Vector2Int>(count);
        for (int i = 0; i < count; i++) testPositions.Add(Vector2Int.zero);

        for (int x = 0; x < grid.GridWidth; x++)
        {
            for (int y = 0; y < grid.GridHeight; y++)
            {
                bool outOfBounds = false;
                for (int i = 0; i < count; i++)
                {
                    int gx = x + offsets[i].x;
                    int gy = y + offsets[i].y;
                    if (gx < 0 || gx >= grid.GridWidth || gy < 0 || gy >= grid.GridHeight)
                    {
                        outOfBounds = true;
                        break;
                    }
                    testPositions[i] = new Vector2Int(gx, gy);
                }

                if (!outOfBounds && grid.CanPlaceBlocks(testPositions))
                {
                    foundX = x;
                    foundY = y;
                    return true;
                }
            }
        }
        return false;
    }

    public static bool CanPlaceBlockAt(BlockModel block, int originX, int originY, IGridService grid)
    {
        if (block == null || grid == null) return false;
        var offsets = block.ShapeOffsets;
        for (int i = 0; i < offsets.Count; i++)
        {
            int gx = originX + offsets[i].x;
            int gy = originY + offsets[i].y;
            if (gx < 0 || gx >= grid.GridWidth || gy < 0 || gy >= grid.GridHeight) return false;
            if (grid.IsCellOccupied(gx, gy)) return false;
        }
        return true;
    }

    public static int CountLinesClearedIfPlaced(BlockModel block, int originX, int originY, IGridService grid)
    {
        CalculateRemainingCellsAfterPlacement(block, originX, originY, grid, out int linesCleared);
        return linesCleared;
    }

    public static int CalculateRemainingCellsAfterPlacement(
        BlockModel block,
        int originX,
        int originY,
        IGridService grid,
        out int linesCleared)
    {
        linesCleared = 0;
        if (block == null || grid == null) return grid != null ? grid.OccupiedCellCount : 0;

        ulong blockMask = 0;
        byte rowsAffected = 0;
        byte colsAffected = 0;
        var offsets = block.ShapeOffsets;
        int count = offsets.Count;

        for (int i = 0; i < count; i++)
        {
            int gx = originX + offsets[i].x;
            int gy = originY + offsets[i].y;
            if (gx < 0 || gx >= grid.GridWidth || gy < 0 || gy >= grid.GridHeight) return grid.OccupiedCellCount;
            if (grid.IsCellOccupied(gx, gy)) return grid.OccupiedCellCount;

            blockMask |= 1UL << (gx + (gy << 3));
            rowsAffected |= (byte)(1 << gy);
            colsAffected |= (byte)(1 << gx);
        }

        int rowsCleared = 0;
        for (int r = 0; r < grid.GridHeight; r++)
        {
            if ((rowsAffected & (1 << r)) != 0)
            {
                bool full = true;
                for (int c = 0; c < grid.GridWidth; c++)
                {
                    if (!grid.IsCellOccupied(c, r) && (blockMask & (1UL << (c + (r << 3)))) == 0)
                    {
                        full = false;
                        break;
                    }
                }
                if (full) rowsCleared++;
            }
        }

        int colsCleared = 0;
        for (int c = 0; c < grid.GridWidth; c++)
        {
            if ((colsAffected & (1 << c)) != 0)
            {
                bool full = true;
                for (int r = 0; r < grid.GridHeight; r++)
                {
                    if (!grid.IsCellOccupied(c, r) && (blockMask & (1UL << (c + (r << 3)))) == 0)
                    {
                        full = false;
                        break;
                    }
                }
                if (full) colsCleared++;
            }
        }

        linesCleared = rowsCleared + colsCleared;
        if (linesCleared == 0) return grid.OccupiedCellCount + count;

        int clearedCellsCount = (rowsCleared * grid.GridWidth) + (colsCleared * grid.GridHeight) - (rowsCleared * colsCleared);
        int remaining = grid.OccupiedCellCount + count - clearedCellsCount;
        return Mathf.Max(0, remaining);
    }

    public static int PopCount(ulong x)
    {
        x -= (x >> 1) & 0x5555555555555555UL;
        x = (x & 0x3333333333333333UL) + ((x >> 2) & 0x3333333333333333UL);
        x = (x + (x >> 4)) & 0x0F0F0F0F0F0F0F0FUL;
        return (int)((x * 0x0101010101010101UL) >> 56);
    }

    private struct GridBitboard
    {
        public ulong Mask;
        public int Count;

        public GridBitboard(ulong mask)
        {
            Mask = mask;
            Count = PopCount(mask);
        }

        public GridBitboard(IGridService grid)
        {
            Mask = 0;
            if (grid != null)
            {
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        if (grid.IsCellOccupied(x, y)) Mask |= 1UL << (x + (y << 3));
                    }
                }
            }
            Count = PopCount(Mask);
        }

        public bool IsCellOccupied(int x, int y)
        {
            if ((uint)x >= 8 || (uint)y >= 8) return true;
            return (Mask & (1UL << (x + (y << 3)))) != 0;
        }

        public bool CanPlace(ulong blockMask) => (Mask & blockMask) == 0;

        public GridBitboard PlaceAndClear(ulong blockMask, byte rowsAffected, byte colsAffected, out int linesCleared)
        {
            ulong combined = Mask | blockMask;
            ulong clearMask = 0;
            int cleared = 0;

            for (int r = 0; r < 8; r++)
            {
                if ((rowsAffected & (1 << r)) != 0)
                {
                    ulong rMask = 0xFFUL << (r << 3);
                    if ((combined & rMask) == rMask) { clearMask |= rMask; cleared++; }
                }
            }

            for (int c = 0; c < 8; c++)
            {
                if ((colsAffected & (1 << c)) != 0)
                {
                    ulong cMask = 0x0101010101010101UL << c;
                    if ((combined & cMask) == cMask) { clearMask |= cMask; cleared++; }
                }
            }

            linesCleared = cleared;
            return new GridBitboard(combined & ~clearMask);
        }
    }

    private class SimulatedGrid : IGridService
    {
        private GridBitboard _board;

        public int GridWidth => 8;
        public int GridHeight => 8;
        public int OccupiedCellCount => _board.Count;
        public float OccupancyRatio => _board.Count / 64f;

        public SimulatedGrid(IGridService source)
        {
            _board = source is SimulatedGrid sim ? sim._board : new GridBitboard(source);
        }

#pragma warning disable CS0067
        public event Action<bool, List<Vector2Int>> OnPreviewStateChanged;
        public event Action<List<Vector2Int>> OnBlockPlaced;
        public event Action<List<int>, List<int>> OnLinesCleared;
        public event Action<int, int, bool> OnPlacementResolved;
#pragma warning restore CS0067

        public Vector2Int GetGridPositionFromWorld(Vector3 worldPos) => Vector2Int.zero;
        public Vector3 GetWorldPositionFromGrid(Vector2Int gridPos) => Vector3.zero;
        public void RequestPreview(List<Vector2Int> gridPositions) { }
        public void PlaceBlocks(List<Vector2Int> gridPositions) { }

        public bool IsCellOccupied(int col, int row) => _board.IsCellOccupied(col, row);

        public bool CanPlaceBlocks(List<Vector2Int> gridPositions)
        {
            if (gridPositions == null || gridPositions.Count == 0) return false;
            for (int i = 0; i < gridPositions.Count; i++)
            {
                var pos = gridPositions[i];
                if ((uint)pos.x >= 8 || (uint)pos.y >= 8 || _board.IsCellOccupied(pos.x, pos.y)) return false;
            }
            return true;
        }

        public void SimulatePlaceAndClear(BlockModel block, int originX, int originY)
        {
            if (block == null) return;
            ulong blockMask = 0;
            byte rowsAffected = 0;
            byte colsAffected = 0;
            var offsets = block.ShapeOffsets;
            for (int i = 0; i < offsets.Count; i++)
            {
                int gx = originX + offsets[i].x;
                int gy = originY + offsets[i].y;
                if ((uint)gx < 8 && (uint)gy < 8)
                {
                    blockMask |= 1UL << (gx + (gy << 3));
                    rowsAffected |= (byte)(1 << gy);
                    colsAffected |= (byte)(1 << gx);
                }
            }
            _board = _board.PlaceAndClear(blockMask, rowsAffected, colsAffected, out _);
        }
    }

    private static BlockModel[] HandleEarlyGameProgression(
        int score,
        IGridService grid,
        ShapeDatabase db,
        SpawnConfiguration config,
        SpawnModel model)
    {
        if (db == null) return null;

        if (model != null && model.ActiveScenarioId >= 0 && model.ScenarioStepIndex > 0)
        {
            if (IsBoardMatchingScenario(model.ActiveScenarioId, model.ScenarioStepIndex, grid))
            {
                BlockModel[] finisherBatch = GenerateScenarioFinisher(model.ActiveScenarioId, db, grid, config);
                model.ResetScenario();
                if (finisherBatch != null) return finisherBatch;
            }
            else
            {
                model.ResetScenario();
            }
        }

        if (grid != null && grid.OccupiedCellCount > 0) return GenerateGridAssistedBatch(db, grid, config);

        if (config.enableScenarioChains && model != null)
        {
            int scenarioId = Random.Range(0, TotalScenarios);
            BlockModel[] setupBatch = GenerateScenarioSetup(scenarioId, db, config);
            if (setupBatch != null)
            {
                model.ActiveScenarioId = scenarioId;
                model.ScenarioStepIndex = 1;
                return setupBatch;
            }
        }

        return TryGenerateSynergisticBatch(score, grid, db, config);
    }

    public static bool IsBoardMatchingScenario(int scenarioId, int stepIndex, IGridService grid)
    {
        if (grid == null || grid.OccupiedCellCount == 0) return false;

        int nearFullRows = 0, nearFullCols = 0;
        for (int i = 0; i < 8; i++)
        {
            int rOcc = 0, cOcc = 0;
            for (int j = 0; j < 8; j++)
            {
                if (grid.IsCellOccupied(j, i)) rOcc++;
                if (grid.IsCellOccupied(i, j)) cOcc++;
            }
            if (rOcc >= 4) nearFullRows++;
            if (cOcc >= 4) nearFullCols++;
        }

        if (scenarioId == 0) return nearFullRows >= 2;
        if (scenarioId == 1 || scenarioId == 5 || scenarioId == 8) return nearFullRows >= 1 && nearFullCols >= 1;
        if (scenarioId == 2) return nearFullRows >= 2 || nearFullCols >= 2;
        return nearFullRows >= 1 || nearFullCols >= 1;
    }

    private static BlockModel[] MakeBatch(ShapeData s1, ShapeData s2, ShapeData s3)
    {
        if (s1 != null && s2 != null && s3 != null)
            return new BlockModel[] { CreateBlockModel(s1), CreateBlockModel(s2), CreateBlockModel(s3) };
        return null;
    }

    public static BlockModel[] GenerateScenarioSetup(int scenarioId, ShapeDatabase db, SpawnConfiguration config)
    {
        if (db == null) return null;

        switch (scenarioId)
        {
            case 0: return MakeBatch(FindShapeByBounds(db, 4, 2, 2), FindShapeByBounds(db, 4, 2, 2), FindShapeByBounds(db, 4, 4, 1));
            case 1: return MakeBatch(FindShapeByBounds(db, 5, 5, 1), FindShapeByBounds(db, 4, 4, 1), FindShapeByNameOrBounds(db, "Small_V", 3, 2, 2));
            case 2: return MakeBatch(FindShapeByNameOrBounds(db, "Shape_L", 4, 2, 3), FindShapeByNameOrBounds(db, "Shape_J", 4, 2, 3), FindShapeByBounds(db, 4, 4, 1));
            case 3: return MakeBatch(FindShapeByNameOrBounds(db, "Shape_L", 4, 2, 3), FindShapeByNameOrBounds(db, "Shape_J", 4, 2, 3), FindShapeByBounds(db, 3, 3, 1));
            case 4: return MakeBatch(FindShapeByNameOrBounds(db, "Shape_S", 4, 3, 2), FindShapeByNameOrBounds(db, "Shape_Z", 4, 3, 2), FindShapeByNameOrBounds(db, "Small_V", 3, 2, 2));
            case 5: return MakeBatch(FindShapeByNameOrBounds(db, "Shape_T", 4, 3, 2), FindShapeByNameOrBounds(db, "Small_V", 3, 2, 2), FindShapeByBounds(db, 4, 4, 1));
            case 6: return MakeBatch(FindShapeByNameOrBounds(db, "Small_V", 3, 2, 2), FindShapeByNameOrBounds(db, "Small_V", 3, 2, 2), FindShapeByBounds(db, 4, 4, 1));
            case 7: return MakeBatch(FindShapeByNameOrBounds(db, "Shape_L", 4, 2, 3), FindShapeByNameOrBounds(db, "Small_V", 3, 2, 2), FindShapeByNameOrBounds(db, "Shape_T", 4, 3, 2));
            case 8: return MakeBatch(FindShapeByBounds(db, 4, 2, 2), FindShapeByNameOrBounds(db, "Small_V", 3, 2, 2), FindShapeByNameOrBounds(db, "Shape_L", 4, 2, 3));
            case 9: return MakeBatch(FindShapeByBounds(db, 4, 4, 1), FindShapeByBounds(db, 4, 4, 1), FindShapeByNameOrBounds(db, "Small_V", 3, 2, 2));
        }
        return null;
    }

    public static BlockModel[] GenerateScenarioFinisher(int scenarioId, ShapeDatabase db, IGridService grid, SpawnConfiguration config)
    {
        return GenerateGridAssistedBatch(db, grid, config);
    }

    public static BlockModel[] TryGenerateSynergisticBatch(
        int score,
        IGridService grid,
        ShapeDatabase db,
        SpawnConfiguration config)
    {
        if (db == null) return null;

        if (grid != null && grid.OccupiedCellCount > 0)
        {
            BlockModel[] dynamicBatch = GenerateGridAssistedBatch(db, grid, config);
            if (dynamicBatch != null && dynamicBatch.Length == 3) return dynamicBatch;
        }

        int startPattern = Random.Range(0, 9);
        for (int i = 0; i < 9; i++)
        {
            int pattern = (startPattern + i) % 9;
            BlockModel[] result = TryBuildSynergyPattern(pattern, db);
            if (result != null && result.Length == 3) return result;
        }

        return null;
    }

    private static BlockModel[] TryBuildSynergyPattern(int pattern, ShapeDatabase db)
    {
        ShapeData fallback = GetAllPlayableShapes(db).Count > 0 ? GetAllPlayableShapes(db)[0] : null;
        switch (pattern)
        {
            case 0: return MakeBatch(FindShapeByBounds(db, 4, 4, 1), FindShapeByBounds(db, 4, 4, 1), FindShapeByBounds(db, 4, 2, 2) ?? fallback);
            case 1: return MakeBatch(FindShapeByBounds(db, 4, 2, 2), FindShapeByBounds(db, 4, 2, 2), FindShapeByBounds(db, 4, 4, 1));
            case 2: return MakeBatch(FindShapeByBounds(db, 5, 5, 1), FindShapeByBounds(db, 3, 3, 1), FindShapeByBounds(db, 4, 2, 2) ?? FindShapeByBounds(db, 4, 4, 1) ?? fallback);
            case 3: return MakeBatch(FindShapeByBounds(db, 3, 3, 1), FindShapeByBounds(db, 3, 3, 1), FindShapeByBounds(db, 2, 2, 1));
            case 4: return MakeBatch(FindShapeByBounds(db, 5, 5, 1), FindShapeByBounds(db, 4, 4, 1), FindShapeByBounds(db, 4, 4, 1));
            case 5: return MakeBatch(FindShapeByNameOrBounds(db, "Shape_L", 4, 2, 3), FindShapeByNameOrBounds(db, "Shape_J", 4, 2, 3), FindShapeByBounds(db, 4, 4, 1) ?? FindShapeByBounds(db, 4, 2, 2));
            case 6: return MakeBatch(FindShapeByNameOrBounds(db, "Big_V", 5, 3, 3), FindShapeByBounds(db, 5, 5, 1), FindShapeByBounds(db, 5, 5, 1) ?? FindShapeByBounds(db, 3, 3, 1));
            case 7: return MakeBatch(FindShapeByBounds(db, 4, 2, 2), FindShapeByBounds(db, 4, 2, 2), FindShapeByBounds(db, 4, 2, 2));
            case 8: return MakeBatch(FindShapeByBounds(db, 6, 2, 3) ?? FindShapeByBounds(db, 6, 3, 2), FindShapeByBounds(db, 5, 5, 1), FindShapeByBounds(db, 3, 3, 1));
        }
        return null;
    }

    private static ShapeData FindShapeByNameOrBounds(ShapeDatabase db, string nameKeyword, int tileCount, int width, int height)
    {
        if (db == null) return null;
        List<ShapeData> allShapes = db.shapes ?? new List<ShapeData>();

        if (!string.IsNullOrEmpty(nameKeyword))
        {
            for (int i = 0; i < allShapes.Count; i++)
            {
                var s = allShapes[i];
                if (s != null && s.name != null && s.name.IndexOf(nameKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    return s;
            }
        }

        return FindShapeByBounds(db, tileCount, width, height);
    }

    private static ShapeData FindShapeByBounds(ShapeDatabase db, int tileCount, int width, int height)
    {
        if (db == null) return null;
        List<ShapeData> allShapes = db.shapes ?? new List<ShapeData>();

        for (int i = 0; i < allShapes.Count; i++)
        {
            var s = allShapes[i];
            if (s == null || s.baseOffsets == null || s.baseOffsets.Count != tileCount) continue;

            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            for (int j = 0; j < s.baseOffsets.Count; j++)
            {
                var off = s.baseOffsets[j];
                if (off.x < minX) minX = off.x;
                if (off.x > maxX) maxX = off.x;
                if (off.y < minY) minY = off.y;
                if (off.y > maxY) maxY = off.y;
            }
            int w = maxX - minX + 1;
            int h = maxY - minY + 1;

            if ((w == width && h == height) || (w == height && h == width)) return s;
        }
        return null;
    }

    public static BlockModel[] GenerateGridAssistedBatch(
        ShapeDatabase db,
        IGridService grid,
        SpawnConfiguration config)
    {
        return GenerateIntelligentBatch(db, grid, config, 0);
    }

    public static BlockModel[] GenerateIntelligentBatch(
        ShapeDatabase db,
        IGridService grid,
        SpawnConfiguration config)
    {
        return GenerateIntelligentBatch(db, grid, config, 0);
    }

    public static BlockModel[] GenerateIntelligentBatch(
        ShapeDatabase db,
        IGridService grid,
        SpawnConfiguration config,
        int score)
    {
        if (db == null) return null;

        List<ShapeData> allShapes = GetAllPlayableShapes(db);
        if (allShapes.Count == 0) return null;

        bool isUnderThreshold = score < config.comboPhaseScoreThreshold;
        float occupancyRatio = grid != null ? grid.OccupancyRatio : 0f;

        // 1. Board Shutdown Engine
        // Under threshold: 100% chance to run shutdown solver when occupied cells exist.
        // Above threshold: run with probability highScoreAssistanceRate, OR when board is critical (>= 70%) for clutch comeback.
        bool shouldAttemptShutdown = isUnderThreshold
            || (Random.value < config.highScoreAssistanceRate)
            || (occupancyRatio >= 0.70f);

        if (grid != null && grid.OccupiedCellCount > 0 && shouldAttemptShutdown)
        {
            if (TrySolveBoardShutdown(db, grid, config, out BlockModel[] shutdownBatch))
            {
                if (shutdownBatch != null && shutdownBatch.Length == 3 && CanPlaceAny(shutdownBatch, grid))
                    return shutdownBatch;
            }
        }

        SimulatedGrid sim = new SimulatedGrid(grid);
        BlockModel[] batch = new BlockModel[3];
        HashSet<ShapeData> chosenInBatch = new HashSet<ShapeData>();

        bool allowBulkyInBatch = !isUnderThreshold
            && (occupancyRatio <= 0.40f)
            && (Random.value < config.highScoreBulkyPieceChance);
        bool bulkyAlreadySelected = false;

        for (int slot = 0; slot < 3; slot++)
        {
            BlockModel bestModel = null;
            ShapeData bestShapeData = null;
            int bestX = -1, bestY = -1;
            int bestScore = int.MinValue;
            bool allowBulkyCandidate = allowBulkyInBatch && !bulkyAlreadySelected;

            List<ShapeData> candidatePool = new List<ShapeData>(allShapes);
            ShuffleList(candidatePool);

            for (int p = 0; p < candidatePool.Count; p++)
            {
                var shapeData = candidatePool[p];
                if (shapeData == null) continue;
                bool isAlreadyInBatch = chosenInBatch.Contains(shapeData);

                int maxRot = shapeData.canRotate ? 4 : 1;
                ulong seen0 = 0, seen1 = 0, seen2 = 0, seen3 = 0;

                for (int rot = 0; rot < maxRot; rot++)
                {
                    List<Vector2Int> offsets = shapeData.canRotate
                        ? shapeData.GetRotatedOffsets(rot * 90)
                        : shapeData.baseOffsets;

                    if (offsets == null || offsets.Count == 0) continue;

                    int minOx = int.MaxValue, minOy = int.MaxValue;
                    for (int i = 0; i < offsets.Count; i++)
                    {
                        if (offsets[i].x < minOx) minOx = offsets[i].x;
                        if (offsets[i].y < minOy) minOy = offsets[i].y;
                    }
                    ulong normMask = 0;
                    for (int i = 0; i < offsets.Count; i++)
                    {
                        normMask |= 1UL << ((offsets[i].x - minOx) + ((offsets[i].y - minOy) << 3));
                    }
                    if (rot == 0) seen0 = normMask;
                    else if (rot == 1) { if (normMask == seen0) continue; seen1 = normMask; }
                    else if (rot == 2) { if (normMask == seen0 || normMask == seen1) continue; seen2 = normMask; }
                    else if (rot == 3) { if (normMask == seen0 || normMask == seen1 || normMask == seen2) continue; seen3 = normMask; }

                    List<(int x, int y)> pureOffsets = new List<(int x, int y)>(offsets.Count);
                    for (int i = 0; i < offsets.Count; i++) pureOffsets.Add((offsets[i].x, offsets[i].y));

                    BlockModel candidate = new BlockModel(pureOffsets);

                    for (int x = 0; x < 8; x++)
                    {
                        for (int y = 0; y < 8; y++)
                        {
                            if (!CanPlaceBlockAt(candidate, x, y, sim)) continue;

                            int placementScore = EvaluatePlacement(shapeData, candidate, x, y, sim, isAlreadyInBatch, isUnderThreshold, allowBulkyCandidate, out _);
                            int jitterScore = placementScore + Random.Range(0, 20);

                            if (jitterScore > bestScore)
                            {
                                bestScore = jitterScore;
                                bestModel = candidate;
                                bestShapeData = shapeData;
                                bestX = x;
                                bestY = y;
                            }
                        }
                    }
                }
            }

            if (bestModel != null)
            {
                batch[slot] = bestModel;
                if (bestShapeData != null) chosenInBatch.Add(bestShapeData);
                if (bestModel.ShapeOffsets.Count >= 5) bulkyAlreadySelected = true;
                if (bestX >= 0 && bestY >= 0) sim.SimulatePlaceAndClear(bestModel, bestX, bestY);
            }
            else
            {
                BlockModel fallback = FindPlayableShape(db, sim, out bestX, out bestY);
                if (fallback != null)
                {
                    batch[slot] = fallback;
                    if (bestX >= 0 && bestY >= 0) sim.SimulatePlaceAndClear(fallback, bestX, bestY);
                }
                else batch[slot] = new BlockModel(new List<(int x, int y)> { (0, 0) });
            }
        }

        if (config.enableMercyMode && grid != null && !CanPlaceAny(batch, grid))
        {
            BlockModel mercy = FindPlayableShape(db, grid);
            batch[2] = mercy ?? new BlockModel(new List<(int x, int y)> { (0, 0) });
        }

        return batch;
    }

    private struct ShutdownMove
    {
        public ShapeData shapeData;
        public BlockModel model;
        public ulong blockMask;
        public byte rowsAffected;
        public byte colsAffected;
        public int remainingCells;
        public int linesCleared;
        public int score;
    }

    private static bool TrySolveBoardShutdown(
        ShapeDatabase db,
        IGridService grid,
        SpawnConfiguration config,
        out BlockModel[] batch)
    {
        batch = null;
        if (db == null || grid == null || grid.OccupiedCellCount == 0) return false;

        List<ShapeData> pool = GetAllPlayableShapes(db);
        if (pool.Count == 0) return false;

        GridBitboard rootBoard = new GridBitboard(grid);
        List<ShutdownMove> slot0Moves = GetShutdownMoves(pool, rootBoard, prioritizeClearsOnly: false);
        if (slot0Moves.Count == 0) return false;

        slot0Moves.Sort((a, b) => b.score.CompareTo(a.score));

        BlockModel[] bestBatch = null;
        int bestRemaining = int.MaxValue;
        int bestTotalClears = -1;

        int limit0 = Mathf.Min(4, slot0Moves.Count);
        for (int i0 = 0; i0 < limit0; i0++)
        {
            ShutdownMove m0 = slot0Moves[i0];
            if (m0.remainingCells == 0 && m0.linesCleared > 0)
            {
                batch = BuildShutdownBatch(m0.model, null, null, pool);
                return true;
            }

            GridBitboard board1 = rootBoard.PlaceAndClear(m0.blockMask, m0.rowsAffected, m0.colsAffected, out _);
            if (board1.Count == 0)
            {
                batch = BuildShutdownBatch(m0.model, null, null, pool);
                return true;
            }

            List<ShutdownMove> slot1Moves = GetShutdownMoves(pool, board1, prioritizeClearsOnly: board1.Count <= 12);
            int limit1 = Mathf.Min(3, slot1Moves.Count);

            if (limit1 == 0)
            {
                if (board1.Count < bestRemaining)
                {
                    bestRemaining = board1.Count;
                    bestTotalClears = m0.linesCleared;
                    bestBatch = BuildShutdownBatch(m0.model, null, null, pool);
                }
                continue;
            }

            slot1Moves.Sort((a, b) => b.score.CompareTo(a.score));

            for (int i1 = 0; i1 < limit1; i1++)
            {
                ShutdownMove m1 = slot1Moves[i1];
                if (m1.remainingCells == 0 && m1.linesCleared > 0)
                {
                    batch = BuildShutdownBatch(m0.model, m1.model, null, pool);
                    return true;
                }

                GridBitboard board2 = board1.PlaceAndClear(m1.blockMask, m1.rowsAffected, m1.colsAffected, out _);
                if (board2.Count == 0)
                {
                    batch = BuildShutdownBatch(m0.model, m1.model, null, pool);
                    return true;
                }

                List<ShutdownMove> slot2Moves = GetShutdownMoves(pool, board2, prioritizeClearsOnly: true);
                slot2Moves.Sort((a, b) => b.score.CompareTo(a.score));

                int limit2 = Mathf.Min(2, slot2Moves.Count);
                for (int i2 = 0; i2 < limit2; i2++)
                {
                    ShutdownMove m2 = slot2Moves[i2];
                    if (m2.remainingCells == 0 && m2.linesCleared > 0)
                    {
                        batch = new BlockModel[] { m0.model, m1.model, m2.model };
                        return true;
                    }

                    int totalClears = m0.linesCleared + m1.linesCleared + m2.linesCleared;
                    if (m2.remainingCells < bestRemaining || (m2.remainingCells == bestRemaining && totalClears > bestTotalClears))
                    {
                        bestRemaining = m2.remainingCells;
                        bestTotalClears = totalClears;
                        bestBatch = new BlockModel[] { m0.model, m1.model, m2.model };
                    }
                }

                if (limit2 == 0)
                {
                    int totalClears = m0.linesCleared + m1.linesCleared;
                    if (board2.Count < bestRemaining || (board2.Count == bestRemaining && totalClears > bestTotalClears))
                    {
                        bestRemaining = board2.Count;
                        bestTotalClears = totalClears;
                        bestBatch = BuildShutdownBatch(m0.model, m1.model, null, pool);
                    }
                }
            }
        }

        if (bestBatch != null && (bestRemaining <= 2 || (bestRemaining < grid.OccupiedCellCount && bestTotalClears >= 2)))
        {
            batch = bestBatch;
            return true;
        }

        return false;
    }

    private static List<ShutdownMove> GetShutdownMoves(
        List<ShapeData> pool,
        GridBitboard board,
        bool prioritizeClearsOnly)
    {
        List<ShutdownMove> moves = new List<ShutdownMove>();
        if (pool == null || board.Count == 0) return moves;

        int[] rowOcc = new int[8];
        int[] colOcc = new int[8];
        for (int r = 0; r < 8; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                if (board.IsCellOccupied(c, r)) { rowOcc[r]++; colOcc[c]++; }
            }
        }

        for (int p = 0; p < pool.Count; p++)
        {
            var shapeData = pool[p];
            if (shapeData == null) continue;
            int maxRot = shapeData.canRotate ? 4 : 1;
            ulong seen0 = 0, seen1 = 0, seen2 = 0, seen3 = 0;

            for (int rot = 0; rot < maxRot; rot++)
            {
                List<Vector2Int> offsets = shapeData.canRotate
                    ? shapeData.GetRotatedOffsets(rot * 90)
                    : shapeData.baseOffsets;

                if (offsets == null || offsets.Count == 0) continue;

                int minOx = int.MaxValue, minOy = int.MaxValue;
                for (int i = 0; i < offsets.Count; i++)
                {
                    if (offsets[i].x < minOx) minOx = offsets[i].x;
                    if (offsets[i].y < minOy) minOy = offsets[i].y;
                }
                ulong normMask = 0;
                for (int i = 0; i < offsets.Count; i++)
                {
                    normMask |= 1UL << ((offsets[i].x - minOx) + ((offsets[i].y - minOy) << 3));
                }
                if (rot == 0) seen0 = normMask;
                else if (rot == 1) { if (normMask == seen0) continue; seen1 = normMask; }
                else if (rot == 2) { if (normMask == seen0 || normMask == seen1) continue; seen2 = normMask; }
                else if (rot == 3) { if (normMask == seen0 || normMask == seen1 || normMask == seen2) continue; seen3 = normMask; }

                List<(int x, int y)> pureOffsets = new List<(int x, int y)>(offsets.Count);
                for (int i = 0; i < offsets.Count; i++) pureOffsets.Add((offsets[i].x, offsets[i].y));
                BlockModel candidate = new BlockModel(pureOffsets);

                for (int x = 0; x < 8; x++)
                {
                    for (int y = 0; y < 8; y++)
                    {
                        ulong bMask = 0;
                        byte rowsAff = 0;
                        byte colsAff = 0;
                        bool invalid = false;

                        for (int i = 0; i < offsets.Count; i++)
                        {
                            int gx = x + offsets[i].x;
                            int gy = y + offsets[i].y;
                            if ((uint)gx >= 8 || (uint)gy >= 8) { invalid = true; break; }
                            bMask |= 1UL << (gx + (gy << 3));
                            rowsAff |= (byte)(1 << gy);
                            colsAff |= (byte)(1 << gx);
                        }

                        if (invalid || !board.CanPlace(bMask)) continue;

                        GridBitboard nextBoard = board.PlaceAndClear(bMask, rowsAff, colsAff, out int linesCleared);
                        int rem = nextBoard.Count;

                        if (prioritizeClearsOnly && linesCleared == 0) continue;

                        int score = 0;
                        if (rem == 0 && linesCleared > 0) score += 500000;
                        else if (rem <= 2) score += 150000;

                        score += linesCleared * 25000;
                        int cellsPurged = board.Count + offsets.Count - rem;
                        if (cellsPurged > offsets.Count) score += (cellsPurged - offsets.Count) * 4000;

                        int orphanTiles = 0;
                        for (int i = 0; i < offsets.Count; i++)
                        {
                            int gx = x + offsets[i].x;
                            int gy = y + offsets[i].y;
                            if (rowOcc[gy] == 0 && colOcc[gx] == 0) orphanTiles++;
                        }
                        score -= orphanTiles * 800;

                        if (offsets.Count <= 4)
                        {
                            score += 300;
                            if (offsets.Count == 3) score += 200;
                        }
                        else if (offsets.Count >= 6 && linesCleared == 0) score -= 2000;

                        moves.Add(new ShutdownMove
                        {
                            shapeData = shapeData,
                            model = candidate,
                            blockMask = bMask,
                            rowsAffected = rowsAff,
                            colsAffected = colsAff,
                            remainingCells = rem,
                            linesCleared = linesCleared,
                            score = score
                        });
                    }
                }
            }
        }

        return moves;
    }

    private static BlockModel[] BuildShutdownBatch(
        BlockModel m0,
        BlockModel m1,
        BlockModel m2,
        List<ShapeData> pool)
    {
        BlockModel[] result = new BlockModel[] { m0, m1, m2 };
        for (int i = 0; i < 3; i++)
        {
            if (result[i] == null)
            {
                ShapeData fill = pool.Find(s => s != null && s.baseOffsets != null && s.baseOffsets.Count == 3)
                              ?? pool.Find(s => s != null && s.baseOffsets != null && s.baseOffsets.Count == 2)
                              ?? pool.Find(s => s != null && s.baseOffsets != null && s.baseOffsets.Count == 4)
                              ?? (pool.Count > 0 ? pool[0] : null);
                result[i] = fill != null ? CreateBlockModel(fill) : new BlockModel(new List<(int x, int y)> { (0, 0) });
            }
        }
        return result;
    }

    private static int EvaluatePlacement(
        ShapeData shapeData,
        BlockModel model,
        int originX,
        int originY,
        IGridService grid,
        bool isAlreadyInBatch,
        bool isUnderThreshold,
        bool allowBulkyCandidate,
        out int remainingCellsAfter)
    {
        remainingCellsAfter = CalculateRemainingCellsAfterPlacement(model, originX, originY, grid, out int linesCleared);
        int score = 0;

        // 1. Board Shutdown & Tile Purge
        if (grid.OccupiedCellCount > 0)
        {
            if (remainingCellsAfter == 0) score += 350000;
            else if (linesCleared > 0)
            {
                if (remainingCellsAfter <= 2) score += 160000;
                else if (remainingCellsAfter <= 5) score += 80000;

                int cellsPurged = grid.OccupiedCellCount + model.ShapeOffsets.Count - remainingCellsAfter;
                if (cellsPurged > model.ShapeOffsets.Count) score += (cellsPurged - model.ShapeOffsets.Count) * 3500;
            }

            score += (64 - remainingCellsAfter) * 300;
        }

        // 2. Line Clear Explosions
        if (linesCleared >= 3) score += 50000 + linesCleared * 10000;
        else if (linesCleared == 2) score += 25000;
        else if (linesCleared == 1) score += 8000;

        // 3. Intersection & Near-Full Line Contribution
        int tileCount = model.ShapeOffsets.Count;
        for (int i = 0; i < tileCount; i++)
        {
            int gx = originX + model.ShapeOffsets[i].x;
            int gy = originY + model.ShapeOffsets[i].y;

            int rowOcc = 0, colOcc = 0;
            for (int c = 0; c < 8; c++) if (grid.IsCellOccupied(c, gy)) rowOcc++;
            for (int r = 0; r < 8; r++) if (grid.IsCellOccupied(gx, r)) colOcc++;

            if (rowOcc >= 4) score += (rowOcc >= 6) ? 300 : 150;
            if (colOcc >= 4) score += (colOcc >= 6) ? 300 : 150;
            if (rowOcc >= 4 && colOcc >= 4) score += 1000;

            int neighbors = 0;
            if (gx > 0 && grid.IsCellOccupied(gx - 1, gy)) neighbors++;
            if (gx < 7 && grid.IsCellOccupied(gx + 1, gy)) neighbors++;
            if (gy > 0 && grid.IsCellOccupied(gx, gy - 1)) neighbors++;
            if (gy < 7 && grid.IsCellOccupied(gx, gy + 1)) neighbors++;

            if (neighbors >= 3) score += 350;
            else if (neighbors == 2) score += 180;
            else if (neighbors == 1) score += 60;

            if (gx == 0 || gx == 7) score += 30;
            if (gy == 0 || gy == 7) score += 30;
        }

        // 4. Shape Morphology
        int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
        for (int i = 0; i < tileCount; i++)
        {
            var off = model.ShapeOffsets[i];
            if (off.x < minX) minX = off.x;
            if (off.x > maxX) maxX = off.x;
            if (off.y < minY) minY = off.y;
            if (off.y > maxY) maxY = off.y;
        }
        int w = maxX - minX + 1;
        int h = maxY - minY + 1;

        bool isCompactAngular = (w > 1 && h > 1 && tileCount <= 4);
        bool isStraightConnector = (w == 1 || h == 1) && tileCount <= 4;
        bool isLargeShape = (tileCount >= 5);

        if (isCompactAngular)
        {
            score += 700;
            if (tileCount == 3) score += 250;
        }
        else if (isStraightConnector)
        {
            score += 300;
        }
        else if (isLargeShape)
        {
            if (isUnderThreshold)
            {
                if (linesCleared < 2) score -= 2500;
            }
            else
            {
                // Above threshold: allow bulky pieces (Square 3x3, Line 5) when board is clear (<= 40% full)
                if (allowBulkyCandidate)
                {
                    score += 3500; // Boost bulky pieces to challenge player in open space
                }
                else if (linesCleared < 2)
                {
                    score -= 2000; // Suppress bulky pieces if already selected or not allowed
                }
            }
        }

        if (isAlreadyInBatch) score -= 400;
        return score;
    }

    private static BlockModel FindPlayableShape(ShapeDatabase db, IGridService grid)
    {
        return FindPlayableShape(db, grid, out _, out _);
    }

    private static BlockModel FindPlayableShape(ShapeDatabase db, IGridService grid, out int foundX, out int foundY)
    {
        foundX = -1;
        foundY = -1;
        if (db == null || grid == null) return null;

        List<ShapeData> searchPool = GetAllPlayableShapes(db);
        ShuffleList(searchPool);

        for (int p = 0; p < searchPool.Count; p++)
        {
            var shapeData = searchPool[p];
            if (shapeData == null || (shapeData.baseOffsets != null && shapeData.baseOffsets.Count <= 1)) continue;

            int maxRotations = shapeData.canRotate ? 4 : 1;
            for (int i = 0; i < maxRotations; i++)
            {
                List<Vector2Int> offsets = shapeData.canRotate
                    ? shapeData.GetRotatedOffsets(i * 90)
                    : shapeData.baseOffsets;

                if (offsets == null) continue;

                List<(int x, int y)> pureOffsets = new List<(int x, int y)>(offsets.Count);
                for (int o = 0; o < offsets.Count; o++) pureOffsets.Add((offsets[o].x, offsets[o].y));

                BlockModel candidate = new BlockModel(pureOffsets);
                if (CanPlaceBlockAnywhere(candidate, grid, out foundX, out foundY)) return candidate;
            }
        }

        return null;
    }

    public static List<ShapeData> GetAllPlayableShapes(ShapeDatabase db)
    {
        List<ShapeData> list = new List<ShapeData>();
        if (db == null || db.shapes == null) return list;

        for (int i = 0; i < db.shapes.Count; i++)
        {
            var s = db.shapes[i];
            if (s != null && s.baseOffsets != null && s.baseOffsets.Count > 1) list.Add(s);
        }
        return list;
    }

    private static void ShuffleList<T>(List<T> list)
    {
        if (list == null || list.Count <= 1) return;
        for (int i = list.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }
}
