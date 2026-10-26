using System;
using System.Collections.Generic;
using UnityEngine;

public partial class BlockSpawnGeneratorTest
{
    private class MockThreeGapRowGrid : IGridService
    {
        public int GridWidth => 8;
        public int GridHeight => 8;
        public int OccupiedCellCount => 5;
        public float OccupancyRatio => 5f / 64f;

#pragma warning disable CS0067
        public event Action<bool, List<CellPlacementData>> OnPreviewStateChanged;
        public event Action<List<CellPlacementData>> OnBlockPlaced;
        public event Action<List<int>, List<int>, List<Vector3>> OnLinesCleared;
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
        public event Action<List<int>, List<int>, List<Vector3>> OnLinesCleared;
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
            if (row == 0 && (col < 6)) return true;
            if (row == 1 && (col < 5)) return true;
            return false;
        }
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
        public event Action<List<int>, List<int>, List<Vector3>> OnLinesCleared;
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
            if (row == 2 && (col != 3 && col != 4)) return true;
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
        public event Action<List<int>, List<int>, List<Vector3>> OnLinesCleared;
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
        public event Action<List<int>, List<int>, List<Vector3>> OnLinesCleared;
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
        public event Action<List<int>, List<int>, List<Vector3>> OnLinesCleared;
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
            if (gridPositions != null && gridPositions.Count == 1 && gridPositions[0] == Vector2Int.zero)
            {
                return true;
            }
            return false;
        }

        public bool IsCellOccupied(int col, int row) => !(col == 0 && row == 0);
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
        public event Action<List<int>, List<int>, List<Vector3>> OnLinesCleared;
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
}
