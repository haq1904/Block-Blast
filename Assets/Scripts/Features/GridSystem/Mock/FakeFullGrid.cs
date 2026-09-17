#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockBlast.Mocking
{
    /// <summary>
    /// Mock grid for testing Game Over conditions in Play Mode.
    /// Always reports that the grid is full and cannot accept any blocks.
    /// </summary>
    public class FakeFullGrid : IGridService
    {
        public int GridWidth => 8;
        public int GridHeight => 8;
        public int OccupiedCellCount => 64;
        public float OccupancyRatio => 1f;

        public event Action<bool, List<CellPlacementData>> OnPreviewStateChanged;
        public event Action<List<CellPlacementData>> OnBlockPlaced;
        public event Action<List<int>, List<int>> OnLinesCleared;
        public event Action<List<int>, List<int>> OnPreviewLinesToClear;
        public event Action<int, int, bool> OnPlacementResolved;

        public bool CanPlaceBlocks(List<CellPlacementData> cells) => false;
        public bool CanPlaceBlocks(List<Vector2Int> gridPositions) => false;

        public bool IsCellOccupied(int col, int row) => true;

        public Vector2Int GetGridPositionFromWorld(Vector3 worldPos) => Vector2Int.zero;
        public Vector3 GetWorldPositionFromGrid(Vector2Int gridPos) => Vector3.zero;

        public void RequestPreview(List<CellPlacementData> cells) { }
        public void RequestPreview(List<Vector2Int> gridPositions) { }

        public void PlaceBlocks(List<CellPlacementData> cells) { }
        public void PlaceBlocks(List<Vector2Int> gridPositions) { }
    }
}
#endif
