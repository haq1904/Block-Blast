using System;
using System.Collections.Generic;
using UnityEngine;

public class GridController : MonoBehaviour, IGridService
{
    private const float StartX = -3.5f;
    private const float StartZ = 1.5f;

    private GridModel model;

    public event Action<bool, List<Vector2Int>> OnPreviewStateChanged;
    public event Action<List<Vector2Int>> OnBlockPlaced;
    public event Action<List<int>, List<int>> OnLinesCleared;

    public int GridWidth => model.Cols;
    public int GridHeight => model.Rows;
    public int OccupiedCellCount => model.GetOccupiedCount();
    public float OccupancyRatio => (model.Cols * model.Rows) > 0 ? (float)model.GetOccupiedCount() / (model.Cols * model.Rows) : 0f;

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

    public bool CanPlaceBlocks(List<Vector2Int> gridPositions)
    {
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


    public void RequestPreview(List<Vector2Int> gridPositions)
    {
        bool isValid = CanPlaceBlocks(gridPositions);
        OnPreviewStateChanged?.Invoke(isValid, gridPositions);
    }

    public void PlaceBlocks(List<Vector2Int> gridPositions)
    {
        if (!CanPlaceBlocks(gridPositions)) return; // Safety check

        // 1. Lưu Data
        foreach (Vector2Int pos in gridPositions)
        {
            model.SetOccupied(pos.x, pos.y, true);
        }

        // Phát sự kiện thả gạch thành công cho View
        OnBlockPlaced?.Invoke(gridPositions);

        // 2. Quét kiểm tra xem có hàng/cột nào đầy không
        List<int> clearedRows = new List<int>();
        List<int> clearedCols = new List<int>();

        HashSet<int> rowsToCheck = new HashSet<int>();
        HashSet<int> colsToCheck = new HashSet<int>();

        foreach (Vector2Int pos in gridPositions)
        {
            rowsToCheck.Add(pos.y);
            colsToCheck.Add(pos.x);
        }

        foreach (int row in rowsToCheck)
        {
            if (model.IsRowFull(row)) clearedRows.Add(row);
        }

        foreach (int col in colsToCheck)
        {
            if (model.IsColFull(col)) clearedCols.Add(col);
        }

        // 3. Tiến hành xóa Data và bắn Event nổ
        if (clearedRows.Count > 0 || clearedCols.Count > 0)
        {
            foreach (int row in clearedRows)
            {
                model.ClearRow(row);
                Debug.Log($"[GridController] Cleared Row: {row}");
            }

            foreach (int col in clearedCols)
            {
                model.ClearCol(col);
                Debug.Log($"[GridController] Cleared Col: {col}");
            }

            OnLinesCleared?.Invoke(clearedRows, clearedCols);
        }
    }
}
