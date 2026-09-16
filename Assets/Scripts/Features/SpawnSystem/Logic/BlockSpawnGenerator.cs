using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static class BlockSpawnGenerator
{
    public const int TotalScenarios = 33;

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

    public static BlockModel CreateTransformedBlockModel(ShapeData data, int angle, bool mirrorX)
    {
        if (data == null || data.baseOffsets == null || data.baseOffsets.Count == 0)
            return new BlockModel(new List<(int x, int y)> { (0, 0) });

        List<Vector2Int> transformed = new List<Vector2Int>(data.baseOffsets.Count);
        int rotations = ((angle % 360) + 360) % 360 / 90;

        for (int i = 0; i < data.baseOffsets.Count; i++)
        {
            int x = data.baseOffsets[i].x;
            int y = data.baseOffsets[i].y;

            if (mirrorX) x = -x;

            for (int r = 0; r < rotations; r++)
            {
                int temp = x;
                x = y;
                y = -temp;
            }
            transformed.Add(new Vector2Int(x, y));
        }

        List<(int x, int y)> normalized = NormalizeOffsets(transformed);
        return new BlockModel(normalized);
    }

    public static BlockModel[] MakeTransformedBatch(ShapeData s1, ShapeData s2, ShapeData s3, int angle, bool mirrorX)
    {
        if (s1 != null && s2 != null && s3 != null)
        {
            return new BlockModel[]
            {
                CreateTransformedBlockModel(s1, angle, mirrorX),
                CreateTransformedBlockModel(s2, angle, mirrorX),
                CreateTransformedBlockModel(s3, angle, mirrorX)
            };
        }
        return null;
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

        GridBitboard board = new GridBitboard(grid);
        ulong blockMask = 0;
        byte rows = 0, cols = 0;
        var offsets = block.ShapeOffsets;

        for (int i = 0; i < offsets.Count; i++)
        {
            int gx = originX + offsets[i].x, gy = originY + offsets[i].y;
            if ((uint)gx >= 8 || (uint)gy >= 8 || board.IsCellOccupied(gx, gy)) return grid.OccupiedCellCount;
            blockMask |= 1UL << (gx + (gy << 3));
            rows |= (byte)(1 << gy);
            cols |= (byte)(1 << gx);
        }

        return board.PlaceAndClear(blockMask, rows, cols, out linesCleared).Count;
    }

    private static BlockModel[] HandleEarlyGameProgression(
        int score,
        IGridService grid,
        ShapeDatabase db,
        SpawnConfiguration config,
        SpawnModel model)
    {
        if (config == null || score >= config.comboPhaseScoreThreshold)
        {
            if (model != null && model.ActiveScenarioId >= 0)
            {
                model.ResetScenario();
            }
            return null;
        }

        ScenarioDatabase scenarioDb = GetScenarioDatabase(config);

        // Phase 2: Check active scenario for completion or deviation
        if (model != null && model.ActiveScenarioId >= 0 && model.ScenarioStepIndex == 1)
        {
            ScenarioData activeScenario = (scenarioDb != null && scenarioDb.scenarios != null && model.ActiveScenarioId < scenarioDb.scenarios.Count)
                ? scenarioDb.scenarios[model.ActiveScenarioId]
                : null;

            if (activeScenario != null && IsBoardMatchingScenario(activeScenario, grid, model.ActiveScenarioAngle, model.ActiveScenarioMirror))
            {
                BlockModel[] finisherBatch = GenerateScenarioFinisher(activeScenario, model.ActiveScenarioAngle, model.ActiveScenarioMirror);
                model.ResetScenario();
                if (finisherBatch != null && finisherBatch.Length == 3) return finisherBatch;
            }
            else
            {
                // Player deviated from scenario -> Reset cleanly and fallback to adaptive solver
                model.ResetScenario();
                if (grid != null && grid.OccupiedCellCount > 0)
                {
                    return GenerateIntelligentBatch(db, grid, config, score);
                }
            }
        }

        // If grid has existing blocks not matching a scenario, assist with normal play
        if (grid != null && grid.OccupiedCellCount > 0)
        {
            return GenerateIntelligentBatch(db, grid, config, score);
        }

        // Phase 1: Setup Trigger (Only when board is clean/empty and scenarios enabled)
        if (config.enableScenarioChains && model != null && scenarioDb != null && scenarioDb.scenarios != null && scenarioDb.scenarios.Count > 0)
        {
            int scenarioIndex = Random.Range(0, scenarioDb.scenarios.Count);
            ScenarioData scenario = scenarioDb.scenarios[scenarioIndex];
            if (scenario != null && scenario.setupBatch != null && scenario.setupBatch.HasAnyShape)
            {
                int angle = scenario.allowRotation ? Random.Range(0, 4) * 90 : 0;
                bool mirror = scenario.allowMirror && Random.value > 0.5f;

                BlockModel[] setupBatch = GenerateScenarioSetup(scenario, angle, mirror);
                if (setupBatch != null && setupBatch.Length == 3)
                {
                    model.ActiveScenarioId = scenarioIndex;
                    model.ScenarioStepIndex = 1;
                    model.ActiveScenarioAngle = angle;
                    model.ActiveScenarioMirror = mirror;
                    return setupBatch;
                }
            }
        }

        return GenerateIntelligentBatch(db, grid, config, score);
    }

    public static bool IsBoardMatchingScenario(ScenarioData scenario, IGridService grid, int angle = 0, bool mirror = false)
    {
        if (scenario == null || grid == null || scenario.targetBoard == null) return false;

        bool[] expected = (angle != 0 || mirror)
            ? ScenarioData.TransformGrid(scenario.targetBoard, angle, mirror)
            : scenario.targetBoard;

        int mismatch = 0;
        int targetOccupied = 0;
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                bool exp = expected[y * 8 + x];
                if (exp) targetOccupied++;
                bool act = grid.IsCellOccupied(x, y);
                if (exp != act)
                {
                    mismatch++;
                    if (mismatch > scenario.matchTolerance) return false;
                }
            }
        }

        // If premature clear occurred, grid occupied count will be significantly lower than target
        if (grid.OccupiedCellCount < targetOccupied - scenario.matchTolerance) return false;

        return mismatch <= scenario.matchTolerance;
    }

    public static BlockModel[] GenerateScenarioSetup(ScenarioData scenario, int angle = 0, bool mirror = false)
    {
        if (scenario == null || scenario.setupBatch == null) return null;
        return MakeTransformedBatch(
            scenario.setupBatch.slot0,
            scenario.setupBatch.slot1,
            scenario.setupBatch.slot2,
            angle,
            mirror);
    }

    public static BlockModel[] GenerateScenarioFinisher(ScenarioData scenario, int angle = 0, bool mirror = false)
    {
        if (scenario == null || scenario.finisherBatch == null) return null;
        return MakeTransformedBatch(
            scenario.finisherBatch.slot0,
            scenario.finisherBatch.slot1,
            scenario.finisherBatch.slot2,
            angle,
            mirror);
    }

    private static ScenarioDatabase GetScenarioDatabase(SpawnConfiguration config)
    {
        if (config != null && config.scenarioDatabase != null)
        {
            return config.scenarioDatabase;
        }
        if (ServiceLocator.TryGet<ScenarioDatabase>(out var db))
        {
            return db;
        }
        return null;
    }

    public static BlockModel[] GenerateGridAssistedBatch(ShapeDatabase db, IGridService grid, SpawnConfiguration config) =>
        GenerateIntelligentBatch(db, grid, config, 0);

    public static BlockModel[] GenerateIntelligentBatch(ShapeDatabase db, IGridService grid, SpawnConfiguration config) =>
        GenerateIntelligentBatch(db, grid, config, 0);

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

        GridBitboard board = new GridBitboard(grid);
        BlockModel[] batch = new BlockModel[3];
        HashSet<ShapeData> chosenInBatch = new HashSet<ShapeData>();

        bool allowBulkyInBatch = !isUnderThreshold
            && (occupancyRatio <= 0.40f)
            && (Random.value < config.highScoreBulkyPieceChance);
        bool bulkyAlreadySelected = false;
        bool tinyAlreadySelected = false;
        bool allowTinyInBatch = (occupancyRatio >= 0.65f) || (Random.value < 0.10f);

        for (int slot = 0; slot < 3; slot++)
        {
            BlockModel bestModel = null;
            ShapeData bestShapeData = null;
            ulong bestMask = 0;
            byte bestRowsAff = 0, bestColsAff = 0;
            int bestScore = int.MinValue;
            bool allowBulkyCandidate = allowBulkyInBatch && !bulkyAlreadySelected;
            bool allowTinyCandidate = allowTinyInBatch && !tinyAlreadySelected;

            List<ShapeData> candidatePool = new List<ShapeData>(allShapes);
            ShuffleList(candidatePool);

            for (int p = 0; p < candidatePool.Count; p++)
            {
                var shapeData = candidatePool[p];
                if (shapeData == null) continue;
                bool isAlreadyInBatch = chosenInBatch.Contains(shapeData);

                List<BlockModel> uniqueRotations = GetUniqueRotations(shapeData);
                for (int r = 0; r < uniqueRotations.Count; r++)
                {
                    BlockModel candidate = uniqueRotations[r];
                    var offsets = candidate.ShapeOffsets;

                    for (int x = 0; x < 8; x++)
                    {
                        for (int y = 0; y < 8; y++)
                        {
                            ulong bMask = 0;
                            byte rowsAff = 0, colsAff = 0;
                            bool outOfBounds = false;

                            for (int i = 0; i < offsets.Count; i++)
                            {
                                int gx = x + offsets[i].x;
                                int gy = y + offsets[i].y;
                                if ((uint)gx >= 8 || (uint)gy >= 8) { outOfBounds = true; break; }
                                bMask |= 1UL << (gx + (gy << 3));
                                rowsAff |= (byte)(1 << gy);
                                colsAff |= (byte)(1 << gx);
                            }

                            if (outOfBounds || !board.CanPlace(bMask)) continue;

                            GridBitboard nextBoard = board.PlaceAndClear(bMask, rowsAff, colsAff, out int linesCleared);
                            int placementScore = EvaluatePlacement(shapeData, candidate, x, y, board, nextBoard, linesCleared, isAlreadyInBatch, isUnderThreshold, allowBulkyCandidate, bulkyAlreadySelected, tinyAlreadySelected, allowTinyCandidate);
                            int jitterScore = placementScore + Random.Range(0, 45);

                            if (jitterScore > bestScore)
                            {
                                bestScore = jitterScore;
                                bestModel = candidate;
                                bestShapeData = shapeData;
                                bestMask = bMask;
                                bestRowsAff = rowsAff;
                                bestColsAff = colsAff;
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
                if (bestModel.ShapeOffsets.Count <= 2) tinyAlreadySelected = true;
                board = board.PlaceAndClear(bestMask, bestRowsAff, bestColsAff, out _);
            }
            else
            {
                BlockModel fallback = FindPlayableShape(db, grid);
                batch[slot] = fallback ?? new BlockModel(new List<(int x, int y)> { (0, 0) });
            }
        }

        if (config.enableMercyMode && grid != null && !CanPlaceAny(batch, grid))
        {
            BlockModel mercy = FindPlayableShape(db, grid);
            batch[2] = mercy ?? new BlockModel(new List<(int x, int y)> { (0, 0) });
        }

        return batch;
    }

    private static List<BlockModel> GetUniqueRotations(ShapeData shapeData)
    {
        List<BlockModel> list = new List<BlockModel>();
        if (shapeData == null) return list;
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
            for (int i = 0; i < offsets.Count; i++) pureOffsets.Add((offsets[i].x - minOx, offsets[i].y - minOy));
            list.Add(new BlockModel(pureOffsets));
        }
        return list;
    }

    private static int EvaluatePlacement(
        ShapeData shapeData,
        BlockModel model,
        int originX,
        int originY,
        GridBitboard board,
        GridBitboard nextBoard,
        int linesCleared,
        bool isAlreadyInBatch,
        bool isUnderThreshold,
        bool allowBulkyCandidate,
        bool bulkyAlreadySelected,
        bool tinyAlreadySelected,
        bool allowTinyCandidate)
    {
        int remainingCellsAfter = nextBoard.Count;
        int tileCount = model.ShapeOffsets.Count;
        int score = 0;

        // 1. Board Shutdown & Tile Purge
        if (board.Count > 0)
        {
            if (remainingCellsAfter == 0) score += 500000;
            else if (linesCleared > 0)
            {
                if (remainingCellsAfter <= 2) score += 180000;
                else if (remainingCellsAfter <= 5) score += 90000;

                int cellsPurged = board.Count + tileCount - remainingCellsAfter;
                if (cellsPurged > tileCount) score += (cellsPurged - tileCount) * 4000;
            }

            score += (64 - remainingCellsAfter) * 300;
        }

        // 2. Line Clear Explosions
        if (linesCleared >= 3) score += 50000 + linesCleared * 10000;
        else if (linesCleared == 2) score += 25000;
        else if (linesCleared == 1) score += 8000;

        // Substantial shape line-clear bonus: incentivize 3-5 tile shapes over 1-2 tile trivial clears
        if (linesCleared >= 1 && tileCount >= 3)
        {
            score += (tileCount - 2) * 1500;
        }

        // 3. Intersection & Near-Full Line Contribution
        for (int i = 0; i < tileCount; i++)
        {
            int gx = originX + model.ShapeOffsets[i].x;
            int gy = originY + model.ShapeOffsets[i].y;

            int rowOcc = 0, colOcc = 0;
            for (int c = 0; c < 8; c++) if (board.IsCellOccupied(c, gy)) rowOcc++;
            for (int r = 0; r < 8; r++) if (board.IsCellOccupied(gx, r)) colOcc++;

            if (rowOcc >= 4) score += (rowOcc >= 6) ? 300 : 150;
            if (colOcc >= 4) score += (colOcc >= 6) ? 300 : 150;
            if (rowOcc >= 4 && colOcc >= 4) score += 1000;

            int neighbors = 0;
            if (gx > 0 && board.IsCellOccupied(gx - 1, gy)) neighbors++;
            if (gx < 7 && board.IsCellOccupied(gx + 1, gy)) neighbors++;
            if (gy > 0 && board.IsCellOccupied(gx, gy - 1)) neighbors++;
            if (gy < 7 && board.IsCellOccupied(gx, gy + 1)) neighbors++;

            if (neighbors >= 3) score += 350;
            else if (neighbors == 2) score += 180;
            else if (neighbors == 1) score += 60;

            if (gx == 0 || gx == 7) score += 30;
            if (gy == 0 || gy == 7) score += 30;
        }

        // 4. Shape Morphology
        int maxX = 0, maxY = 0;
        for (int i = 0; i < tileCount; i++)
        {
            var off = model.ShapeOffsets[i];
            if (off.x > maxX) maxX = off.x;
            if (off.y > maxY) maxY = off.y;
        }
        int w = maxX + 1;
        int h = maxY + 1;

        bool isCompactAngular = (w > 1 && h > 1 && tileCount >= 3 && tileCount <= 4);
        bool isStraightConnector = (w == 1 || h == 1) && tileCount >= 3 && tileCount <= 4;
        bool isLargeShape = (tileCount >= 5);

        if (isCompactAngular)
        {
            score += 250;
        }
        else if (isStraightConnector)
        {
            score += 250;
        }
        else if (isLargeShape)
        {
            if (isUnderThreshold)
            {
                // Under threshold: Bulky pieces (tileCount >= 5) are restricted.
                // Maximum 1 bulky piece per batch, and ONLY if it actively clears at least 1 line.
                if (bulkyAlreadySelected || linesCleared < 1)
                {
                    score -= 50000;
                }
                else if (linesCleared >= 2)
                {
                    score += 2000;
                }
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

        // 5. Tiny Shape Suppression (Prevent excessive 2x1 / 1x1 shapes)
        if (tileCount <= 2)
        {
            score -= 2500; // Base penalty even if it clears lines, prioritizing substantial shapes
            if (linesCleared == 0) score -= 2000; // Total -4500 when not clearing, destroying open-cell bias
            if (tinyAlreadySelected) score -= 5000; // Never select multiple tiny shapes in one batch
            if (!allowTinyCandidate) score -= 15000; // Gatekeeper: strictly suppress on open boards (<65% full)
        }

        if (isAlreadyInBatch) score -= 600;
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
            var uniqueRotations = GetUniqueRotations(shapeData);
            for (int r = 0; r < uniqueRotations.Count; r++)
            {
                if (CanPlaceBlockAnywhere(uniqueRotations[r], grid, out foundX, out foundY))
                    return uniqueRotations[r];
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
