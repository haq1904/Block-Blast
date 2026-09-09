using System.Collections.Generic;
using UnityEngine;

public static class BlockSpawnGenerator
{
    public static BlockModel[] GenerateBatch(
        int score,
        IGridService gridService,
        ShapeDatabase shapeDatabase,
        SpawnConfiguration config)
    {
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<SpawnConfiguration>();
        }

        BlockModel[] batch = new BlockModel[3];
        if (shapeDatabase == null)
        {
            for (int i = 0; i < 3; i++)
            {
                batch[i] = new BlockModel(new List<(int x, int y)> { (0, 0) });
            }
            return batch;
        }

        float occupancyRatio = gridService != null ? gridService.OccupancyRatio : 0f;
        config.GetTierWeights(score, occupancyRatio, out float w1, out float w2, out float w3);

        int tier3Count = 0;
        HashSet<ShapeData> chosenShapes = new HashSet<ShapeData>();
        int startIndex = 0;

        // HONEYMOON / COMBO ASSISTANCE (< 200,000 points)
        // Actively provide a key shape that can clear 1-3 lines immediately
        if (score < config.comboPhaseScoreThreshold 
            && Random.value < config.comboAssistanceRate 
            && gridService != null 
            && gridService.OccupiedCellCount > 0)
        {
            BlockModel keyShape = FindBestLineClearingShape(shapeDatabase, gridService, config.ultraSmallShapeChance);
            if (keyShape != null)
            {
                batch[0] = keyShape;
                startIndex = 1; // Slot 0 is the Key Shape, remaining slots are builder blocks
            }
        }

        for (int i = startIndex; i < 3; i++)
        {
            int tier = SampleTier(w1, w2, w3);

            // Anti-frustration constraint: limit max Tier 3 blocks
            if (tier == 3 && tier3Count >= config.maxTier3Count)
            {
                tier = Random.Range(1, 3); // Fallback to Tier 1 or 2
            }
            if (tier == 3) tier3Count++;

            ShapeData shape = GetShapeFromTier(shapeDatabase, tier, chosenShapes, config.preventDuplicatesInBatch, config.ultraSmallShapeChance);
            if (shape != null)
            {
                chosenShapes.Add(shape);
            }

            batch[i] = CreateBlockModel(shape);
        }

        // Smart Mercy Mode: if batch is unplayable, replace slot 2 with a shape that fits
        if (config.enableMercyMode && gridService != null && !CanPlaceAny(batch, gridService))
        {
            BlockModel mercyBlock = FindPlayableShape(shapeDatabase, gridService);
            if (mercyBlock != null)
            {
                batch[2] = mercyBlock;
            }
            else
            {
                // Fallback to minimal 1x1 block if no database shape fits
                batch[2] = new BlockModel(new List<(int x, int y)> { (0, 0) });
            }
        }

        return batch;
    }


    private static int SampleTier(float w1, float w2, float w3)
    {
        float r = Random.value;
        if (r < w1) return 1;
        if (r < w1 + w2) return 2;
        return 3;
    }

    private static ShapeData GetShapeFromTier(
        ShapeDatabase db,
        int tier,
        HashSet<ShapeData> chosenShapes,
        bool preventDuplicates,
        float ultraSmallChance)
    {
        List<ShapeData> list = tier == 1 ? db.tier1Shapes : (tier == 2 ? db.tier2Shapes : db.tier3Shapes);
        if (list == null || list.Count == 0)
        {
            list = db.tier1Shapes;
            if (list == null || list.Count == 0) return null;
        }

        // Try to pick a non-duplicate and non-ultra-small shape
        for (int attempt = 0; attempt < 15; attempt++)
        {
            ShapeData candidate = list[Random.Range(0, list.Count)];
            if (candidate == null) continue;

            // Heavily suppress 1x1 and 1x2 shapes during regular gameplay
            if (candidate.baseOffsets != null && candidate.baseOffsets.Count <= 2)
            {
                if (Random.value > ultraSmallChance)
                {
                    continue; // Reroll to avoid giving 1x1 or 1x2 in regular turns
                }
            }

            if (preventDuplicates && list.Count > 1 && chosenShapes.Contains(candidate))
            {
                continue;
            }

            return candidate;
        }

        return list[Random.Range(0, list.Count)];
    }


    public static BlockModel CreateBlockModel(ShapeData data)
    {
        if (data == null) return new BlockModel(new List<(int x, int y)> { (0, 0) });

        List<Vector2Int> unityOffsets;
        if (data.canRotate)
        {
            int angle = Random.Range(0, 4) * 90;
            unityOffsets = data.GetRotatedOffsets(angle);
        }
        else
        {
            unityOffsets = data.baseOffsets;
        }

        List<(int x, int y)> pureCSharpOffsets = new List<(int x, int y)>();
        foreach (var v in unityOffsets)
        {
            pureCSharpOffsets.Add((v.x, v.y));
        }

        return new BlockModel(pureCSharpOffsets);
    }

    public static bool CanPlaceAny(BlockModel[] batch, IGridService grid)
    {
        if (batch == null || grid == null) return false;

        foreach (var block in batch)
        {
            if (block != null && CanPlaceBlockAnywhere(block, grid))
            {
                return true;
            }
        }
        return false;
    }

    public static bool CanPlaceBlockAnywhere(BlockModel block, IGridService grid)
    {
        if (block == null || grid == null) return false;

        for (int x = 0; x < grid.GridWidth; x++)
        {
            for (int y = 0; y < grid.GridHeight; y++)
            {
                List<Vector2Int> testPositions = new List<Vector2Int>();
                foreach (var offset in block.ShapeOffsets)
                {
                    testPositions.Add(new Vector2Int(x + offset.x, y + offset.y));
                }

                if (grid.CanPlaceBlocks(testPositions))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static BlockModel FindPlayableShape(ShapeDatabase db, IGridService grid)
    {
        // Search Tier 1 shapes first for the best fit
        if (db.tier1Shapes != null)
        {
            foreach (var shapeData in db.tier1Shapes)
            {
                if (shapeData == null) continue;

                // Try all 4 rotation angles if rotatable, or angle 0
                int maxRotations = shapeData.canRotate ? 4 : 1;
                for (int i = 0; i < maxRotations; i++)
                {
                    List<Vector2Int> offsets = shapeData.canRotate 
                        ? shapeData.GetRotatedOffsets(i * 90) 
                        : shapeData.baseOffsets;

                    List<(int x, int y)> pureOffsets = new List<(int x, int y)>();
                    foreach (var v in offsets)
                    {
                        pureOffsets.Add((v.x, v.y));
                    }

                    BlockModel candidate = new BlockModel(pureOffsets);
                    if (CanPlaceBlockAnywhere(candidate, grid))
                    {
                        return candidate;
                    }
                }
            }
        }

        return null;
    }

    public static int CountLinesClearedIfPlaced(BlockModel block, int originX, int originY, IGridService grid)
    {
        if (block == null || grid == null) return 0;

        HashSet<Vector2Int> blockCells = new HashSet<Vector2Int>();
        HashSet<int> rowsAffected = new HashSet<int>();
        HashSet<int> colsAffected = new HashSet<int>();

        foreach (var offset in block.ShapeOffsets)
        {
            int gx = originX + offset.x;
            int gy = originY + offset.y;

            if (gx < 0 || gx >= grid.GridWidth || gy < 0 || gy >= grid.GridHeight) return 0;
            if (grid.IsCellOccupied(gx, gy)) return 0;

            blockCells.Add(new Vector2Int(gx, gy));
            rowsAffected.Add(gy);
            colsAffected.Add(gx);
        }

        int linesCleared = 0;

        foreach (int r in rowsAffected)
        {
            bool rowFull = true;
            for (int c = 0; c < grid.GridWidth; c++)
            {
                if (!grid.IsCellOccupied(c, r) && !blockCells.Contains(new Vector2Int(c, r)))
                {
                    rowFull = false;
                    break;
                }
            }
            if (rowFull) linesCleared++;
        }

        foreach (int c in colsAffected)
        {
            bool colFull = true;
            for (int r = 0; r < grid.GridHeight; r++)
            {
                if (!grid.IsCellOccupied(c, r) && !blockCells.Contains(new Vector2Int(c, r)))
                {
                    colFull = false;
                    break;
                }
            }
            if (colFull) linesCleared++;
        }

        return linesCleared;
    }

    private static BlockModel FindBestLineClearingShape(ShapeDatabase db, IGridService grid, float ultraSmallChance)
    {
        if (db == null || grid == null || grid.OccupiedCellCount == 0) return null;

        BlockModel bestShape = null;
        int maxLinesCleared = 0;

        // Build candidate search pool: prioritize Tier 2 (standard puzzle blocks), then Tier 1 and Tier 3
        List<ShapeData> searchPool = new List<ShapeData>();
        if (db.tier2Shapes != null) searchPool.AddRange(db.tier2Shapes);
        if (db.tier1Shapes != null)
        {
            foreach (var s in db.tier1Shapes)
            {
                if (s == null) continue;
                if (s.baseOffsets != null && s.baseOffsets.Count <= 2)
                {
                    if (Random.value < ultraSmallChance) searchPool.Add(s);
                }
                else
                {
                    searchPool.Add(s);
                }
            }
        }
        if (db.tier3Shapes != null) searchPool.AddRange(db.tier3Shapes);

        ShuffleList(searchPool);

        foreach (var shapeData in searchPool)
        {
            if (shapeData == null) continue;

            int maxRotations = shapeData.canRotate ? 4 : 1;
            for (int r = 0; r < maxRotations; r++)
            {
                List<Vector2Int> offsets = shapeData.canRotate 
                    ? shapeData.GetRotatedOffsets(r * 90) 
                    : shapeData.baseOffsets;

                List<(int x, int y)> pureOffsets = new List<(int x, int y)>();
                foreach (var v in offsets) pureOffsets.Add((v.x, v.y));
                BlockModel candidate = new BlockModel(pureOffsets);

                for (int x = 0; x < grid.GridWidth; x++)
                {
                    for (int y = 0; y < grid.GridHeight; y++)
                    {
                        int clears = CountLinesClearedIfPlaced(candidate, x, y, grid);
                        if (clears > maxLinesCleared)
                        {
                            maxLinesCleared = clears;
                            bestShape = candidate;

                            // If this block triggers a double/triple multi-blast, pick it immediately!
                            if (clears >= 2) return bestShape;
                        }
                    }
                }
            }
        }

        return bestShape;
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

