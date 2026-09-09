public class GridModel
{
    public int Cols { get; private set; }
    public int Rows { get; private set; }
    private bool[,] gridOccupied;

    public GridModel(int cols = 8, int rows = 8)
    {
        Cols = cols;
        Rows = rows;
        gridOccupied = new bool[Cols, Rows];
    }

    public bool IsOccupied(int col, int row)
    {
        return gridOccupied[col, row];
    }

    public void SetOccupied(int col, int row, bool isOccupied)
    {
        gridOccupied[col, row] = isOccupied;
    }

    public bool IsWithinBounds(int col, int row)
    {
        return col >= 0 && col < Cols && row >= 0 && row < Rows;
    }

    public bool IsRowFull(int row)
    {
        for (int col = 0; col < Cols; col++)
        {
            if (!gridOccupied[col, row]) return false;
        }
        return true;
    }

    public bool IsColFull(int col)
    {
        for (int row = 0; row < Rows; row++)
        {
            if (!gridOccupied[col, row]) return false;
        }
        return true;
    }

    public void ClearRow(int row)
    {
        for (int col = 0; col < Cols; col++)
        {
            gridOccupied[col, row] = false;
        }
    }

    public void ClearCol(int col)
    {
        for (int row = 0; row < Rows; row++)
        {
            gridOccupied[col, row] = false;
        }
    }

    public int GetOccupiedCount()
    {
        int count = 0;
        for (int col = 0; col < Cols; col++)
        {
            for (int row = 0; row < Rows; row++)
            {
                if (gridOccupied[col, row])
                {
                    count++;
                }
            }
        }
        return count;
    }
}

