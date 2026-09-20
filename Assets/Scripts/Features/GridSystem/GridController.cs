using System;
using System.Collections.Generic;
using UnityEngine;

public class GridController : MonoBehaviour, IGridService
{
    private const float StartX = -3.5f;
    private const float StartZ = 1.5f;

    private GridModel model;

    public event Action<bool, List<CellPlacementData>> OnPreviewStateChanged;
    public event Action<List<CellPlacementData>> OnBlockPlaced;
    public event Action<List<int>, List<int>, List<Vector3>> OnLinesCleared;
    public event Action<List<int>, List<int>> OnPreviewLinesToClear;
    public event Action<int, int, bool> OnPlacementResolved;

    public int GridWidth => model != null ? model.Cols : 8;
    public int GridHeight => model != null ? model.Rows : 8;
    public int OccupiedCellCount => model != null ? model.GetOccupiedCount() : 0;
    public float OccupancyRatio => (model != null && (model.Cols * model.Rows) > 0)
        ? (float)model.GetOccupiedCount() / (model.Cols * model.Rows)
        : 0f;

    private readonly List<int> cachedCandidateRows = new List<int>();
    private readonly List<int> cachedCandidateCols = new List<int>();
    private readonly List<int> emptyLineList = new List<int>();

    private void Awake()
    {
        model = new GridModel(8, 8);
        ServiceLocator.Register<IGridService>(this);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<IGridService>();
    }

    public Vector2Int GetGridPositionFromWorld(Vector3 worldPos)
    {
        int col = Mathf.RoundToInt(worldPos.x - StartX);
        int row = Mathf.RoundToInt(worldPos.z - StartZ);
        return new Vector2Int(col, row);
    }

    public Vector3 GetWorldPositionFromGrid(Vector2Int gridPos)
    {
        float x = gridPos.x + StartX;
        float z = gridPos.y + StartZ;
        return new Vector3(x, -1, z);
    }

    public bool CanPlaceBlocks(List<CellPlacementData> cells)
    {
        if (cells == null || cells.Count == 0) return false;
        foreach (var cell in cells)
        {
            if (!model.IsWithinBounds(cell.gridPos.x, cell.gridPos.y)) return false;
            if (model.IsOccupied(cell.gridPos.x, cell.gridPos.y)) return false;
        }
        return true;
    }

    public bool CanPlaceBlocks(List<Vector2Int> gridPositions)
    {
        if (gridPositions == null || gridPositions.Count == 0) return false;
        foreach (Vector2Int pos in gridPositions)
        {
            if (!model.IsWithinBounds(pos.x, pos.y)) return false;
            if (model.IsOccupied(pos.x, pos.y)) return false;
        }
        return true;
    }

    public bool IsCellOccupied(int col, int row)
    {
        if (model == null || !model.IsWithinBounds(col, row)) return false;
        return model.IsOccupied(col, row);
    }

    public void RequestPreview(List<CellPlacementData> cells)
    {
        bool isValid = CanPlaceBlocks(cells);
        OnPreviewStateChanged?.Invoke(isValid, cells);

        if (isValid && cells != null && cells.Count > 0)
        {
            model.GetPotentialLineClears(cells, cachedCandidateRows, cachedCandidateCols);
            OnPreviewLinesToClear?.Invoke(cachedCandidateRows, cachedCandidateCols);
        }
        else
        {
            OnPreviewLinesToClear?.Invoke(emptyLineList, emptyLineList);
        }
    }

    public void RequestPreview(List<Vector2Int> gridPositions)
    {
        List<CellPlacementData> cells = new List<CellPlacementData>();
        if (gridPositions != null)
        {
            for (int i = 0; i < gridPositions.Count; i++)
            {
                cells.Add(new CellPlacementData(gridPositions[i], "", 0));
            }
        }
        RequestPreview(cells);
    }

    public void PlaceBlocks(List<CellPlacementData> cells)
    {
        if (!CanPlaceBlocks(cells)) return;

        // Reset any pending pre-clear indicators
        OnPreviewLinesToClear?.Invoke(emptyLineList, emptyLineList);

        // 1. Record data in model
        foreach (var cell in cells)
        {
            model.SetOccupied(cell.gridPos.x, cell.gridPos.y, true, cell.blockTypeId, cell.variantIndex);
        }

        // Broadcast placement event to View
        OnBlockPlaced?.Invoke(cells);

        // 2. Scan rows and columns for full lines
        List<int> clearedRows = new List<int>();
        List<int> clearedCols = new List<int>();

        HashSet<int> rowsToCheck = new HashSet<int>();
        HashSet<int> colsToCheck = new HashSet<int>();

        foreach (var cell in cells)
        {
            rowsToCheck.Add(cell.gridPos.y);
            colsToCheck.Add(cell.gridPos.x);
        }

        foreach (int row in rowsToCheck)
        {
            if (model.IsRowFull(row)) clearedRows.Add(row);
        }

        foreach (int col in colsToCheck)
        {
            if (model.IsColFull(col)) clearedCols.Add(col);
        }

        // 3. Clear data and broadcast clear event
        if (clearedRows.Count > 0 || clearedCols.Count > 0)
        {
            foreach (int row in clearedRows)
            {
                model.ClearRow(row);
            }

            foreach (int col in clearedCols)
            {
                model.ClearCol(col);
            }

            List<Vector3> comboVFXPositions = GetComboVFXPositions(clearedRows, clearedCols);
            OnLinesCleared?.Invoke(clearedRows, clearedCols, comboVFXPositions);
        }

        // 4. Broadcast placement resolution for ScoreSystem and game flow
        int totalLinesCleared = clearedRows.Count + clearedCols.Count;
        bool isAllClear = model.GetOccupiedCount() == 0;
        OnPlacementResolved?.Invoke(cells.Count, totalLinesCleared, isAllClear);
    }

    public void PlaceBlocks(List<Vector2Int> gridPositions)
    {
        List<CellPlacementData> cells = new List<CellPlacementData>();
        if (gridPositions != null)
        {
            for (int i = 0; i < gridPositions.Count; i++)
            {
                cells.Add(new CellPlacementData(gridPositions[i], "", 0));
            }
        }
        PlaceBlocks(cells);
    }

    public List<Vector3> GetComboVFXPositions(List<int> rows, List<int> cols)
    {
        List<Vector3> positions = new List<Vector3>();
        int totalLines = (rows != null ? rows.Count : 0) + (cols != null ? cols.Count : 0);
        if (totalLines < 2) return positions;

        // If intersecting rows and columns are cleared simultaneously, return all intersection points
        if (rows != null && cols != null && rows.Count > 0 && cols.Count > 0)
        {
            for (int r = 0; r < rows.Count; r++)
            {
                for (int c = 0; c < cols.Count; c++)
                {
                    positions.Add(GetWorldPositionFromGrid(new Vector2Int(cols[c], rows[r])));
                }
            }
        }
        else
        {
            // Parallel rows or parallel columns: compute geometric center of cleared lines
            Vector3 centerWorld = Vector3.zero;
            int count = 0;

            if (rows != null && rows.Count > 0)
            {
                for (int r = 0; r < rows.Count; r++)
                {
                    for (int c = 0; c < 8; c++)
                    {
                        centerWorld += GetWorldPositionFromGrid(new Vector2Int(c, rows[r]));
                        count++;
                    }
                }
            }
            else if (cols != null && cols.Count > 0)
            {
                for (int c = 0; c < cols.Count; c++)
                {
                    for (int r = 0; r < 8; r++)
                    {
                        centerWorld += GetWorldPositionFromGrid(new Vector2Int(cols[c], r));
                        count++;
                    }
                }
            }

            if (count > 0)
            {
                positions.Add(centerWorld / count);
            }
        }

        return positions;
    }

    public Vector3 GetWorldCenter(List<Vector2Int> gridPositions)
    {
        if (gridPositions == null || gridPositions.Count == 0) return Vector3.zero;

        Vector3 sum = Vector3.zero;
        for (int i = 0; i < gridPositions.Count; i++)
        {
            sum += GetWorldPositionFromGrid(gridPositions[i]);
        }
        return sum / gridPositions.Count;
    }
}
