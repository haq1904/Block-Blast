using System.Collections.Generic;
using UnityEngine;

public struct GridCellState
{
    public bool isOccupied;
    public string blockTypeId;
    public int variantId;
}

public enum GridEdgeDirection
{
    North = 0,
    South = 1,
    East = 2,
    West = 3
}

public struct DiscreteGridEdge
{
    public Vector2Int gridPos;
    public GridEdgeDirection direction;

    public DiscreteGridEdge(Vector2Int gridPos, GridEdgeDirection direction)
    {
        this.gridPos = gridPos;
        this.direction = direction;
    }
}

public class GridModel
{
    public int Cols { get; private set; }
    public int Rows { get; private set; }
    private GridCellState[,] gridCells;

    public GridModel(int cols = 8, int rows = 8)
    {
        Cols = cols;
        Rows = rows;
        gridCells = new GridCellState[Cols, Rows];
    }

    public bool IsOccupied(int col, int row)
    {
        if (!IsWithinBounds(col, row)) return false;
        return gridCells[col, row].isOccupied;
    }

    public void SetOccupied(int col, int row, bool isOccupied, string blockTypeId = "", int variantId = 0)
    {
        if (!IsWithinBounds(col, row)) return;
        gridCells[col, row].isOccupied = isOccupied;
        gridCells[col, row].blockTypeId = isOccupied ? blockTypeId : "";
        gridCells[col, row].variantId = isOccupied ? variantId : 0;
    }

    public string GetBlockTypeId(int col, int row)
    {
        if (!IsWithinBounds(col, row)) return "";
        return gridCells[col, row].blockTypeId;
    }

    public int GetVariantId(int col, int row)
    {
        if (!IsWithinBounds(col, row)) return 0;
        return gridCells[col, row].variantId;
    }

    public bool IsWithinBounds(int col, int row)
    {
        return col >= 0 && col < Cols && row >= 0 && row < Rows;
    }

    public bool IsRowFull(int row)
    {
        if (row < 0 || row >= Rows) return false;
        for (int col = 0; col < Cols; col++)
        {
            if (!gridCells[col, row].isOccupied) return false;
        }
        return true;
    }

    public bool IsColFull(int col)
    {
        if (col < 0 || col >= Cols) return false;
        for (int row = 0; row < Rows; row++)
        {
            if (!gridCells[col, row].isOccupied) return false;
        }
        return true;
    }

    public void ClearRow(int row)
    {
        if (row < 0 || row >= Rows) return;
        for (int col = 0; col < Cols; col++)
        {
            gridCells[col, row].isOccupied = false;
            gridCells[col, row].blockTypeId = "";
            gridCells[col, row].variantId = 0;
        }
    }

    public void ClearCol(int col)
    {
        if (col < 0 || col >= Cols) return;
        for (int row = 0; row < Rows; row++)
        {
            gridCells[col, row].isOccupied = false;
            gridCells[col, row].blockTypeId = "";
            gridCells[col, row].variantId = 0;
        }
    }

    public int GetOccupiedCount()
    {
        int count = 0;
        for (int col = 0; col < Cols; col++)
        {
            for (int row = 0; row < Rows; row++)
            {
                if (gridCells[col, row].isOccupied)
                {
                    count++;
                }
            }
        }
        return count;
    }

    public void GetPotentialLineClears(List<CellPlacementData> hypotheticalPositions, List<int> outRows, List<int> outCols)
    {
        if (outRows == null || outCols == null) return;
        outRows.Clear();
        outCols.Clear();

        if (hypotheticalPositions == null || hypotheticalPositions.Count == 0) return;

        HashSet<int> rowsToCheck = new HashSet<int>();
        HashSet<int> colsToCheck = new HashSet<int>();
        HashSet<Vector2Int> hypotheticalSet = new HashSet<Vector2Int>();

        for (int i = 0; i < hypotheticalPositions.Count; i++)
        {
            Vector2Int pos = hypotheticalPositions[i].gridPos;
            if (IsWithinBounds(pos.x, pos.y))
            {
                rowsToCheck.Add(pos.y);
                colsToCheck.Add(pos.x);
                hypotheticalSet.Add(pos);
            }
        }

        foreach (int row in rowsToCheck)
        {
            bool rowWillBeFull = true;
            for (int col = 0; col < Cols; col++)
            {
                if (!gridCells[col, row].isOccupied && !hypotheticalSet.Contains(new Vector2Int(col, row)))
                {
                    rowWillBeFull = false;
                    break;
                }
            }
            if (rowWillBeFull)
            {
                outRows.Add(row);
            }
        }

        foreach (int col in colsToCheck)
        {
            bool colWillBeFull = true;
            for (int row = 0; row < Rows; row++)
            {
                if (!gridCells[col, row].isOccupied && !hypotheticalSet.Contains(new Vector2Int(col, row)))
                {
                    colWillBeFull = false;
                    break;
                }
            }
            if (colWillBeFull)
            {
                outCols.Add(col);
            }
        }
    }

    public void GetPotentialLineClears(List<Vector2Int> hypotheticalPositions, List<int> outRows, List<int> outCols)
    {
        if (outRows == null || outCols == null) return;
        outRows.Clear();
        outCols.Clear();

        if (hypotheticalPositions == null || hypotheticalPositions.Count == 0) return;

        HashSet<int> rowsToCheck = new HashSet<int>();
        HashSet<int> colsToCheck = new HashSet<int>();
        HashSet<Vector2Int> hypotheticalSet = new HashSet<Vector2Int>();

        for (int i = 0; i < hypotheticalPositions.Count; i++)
        {
            Vector2Int pos = hypotheticalPositions[i];
            if (IsWithinBounds(pos.x, pos.y))
            {
                rowsToCheck.Add(pos.y);
                colsToCheck.Add(pos.x);
                hypotheticalSet.Add(pos);
            }
        }

        foreach (int row in rowsToCheck)
        {
            bool rowWillBeFull = true;
            for (int col = 0; col < Cols; col++)
            {
                if (!gridCells[col, row].isOccupied && !hypotheticalSet.Contains(new Vector2Int(col, row)))
                {
                    rowWillBeFull = false;
                    break;
                }
            }
            if (rowWillBeFull)
            {
                outRows.Add(row);
            }
        }

        foreach (int col in colsToCheck)
        {
            bool colWillBeFull = true;
            for (int row = 0; row < Rows; row++)
            {
                if (!gridCells[col, row].isOccupied && !hypotheticalSet.Contains(new Vector2Int(col, row)))
                {
                    colWillBeFull = false;
                    break;
                }
            }
            if (colWillBeFull)
            {
                outCols.Add(col);
            }
        }
    }

    public List<DiscreteGridEdge> GetExposedEdges(IReadOnlyList<Vector2Int> cells)
    {
        var result = new List<DiscreteGridEdge>();
        if (cells == null || cells.Count == 0) return result;

        var placedSet = new HashSet<Vector2Int>(cells);

        Vector2Int[] dirVectors = {
            new Vector2Int(0, 1),   // North (+Y)
            new Vector2Int(0, -1),  // South (-Y)
            new Vector2Int(1, 0),   // East (+X)
            new Vector2Int(-1, 0)   // West (-X)
        };

        GridEdgeDirection[] directions = {
            GridEdgeDirection.North,
            GridEdgeDirection.South,
            GridEdgeDirection.East,
            GridEdgeDirection.West
        };

        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int pos = cells[i];

            for (int d = 0; d < 4; d++)
            {
                Vector2Int neighbor = pos + dirVectors[d];

                // If the neighbor cell belongs to the same placed block, internal edge -> ignore
                if (placedSet.Contains(neighbor)) continue;

                // An edge is exposed if the neighbor is out of bounds or empty in the grid
                bool isExposed = !IsWithinBounds(neighbor.x, neighbor.y) || !IsOccupied(neighbor.x, neighbor.y);
                if (isExposed)
                {
                    result.Add(new DiscreteGridEdge(pos, directions[d]));
                }
            }
        }

        return result;
    }
}
