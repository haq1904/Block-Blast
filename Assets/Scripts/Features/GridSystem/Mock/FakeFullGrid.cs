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

        public event Action<bool, List<Vector2Int>> OnPreviewStateChanged;
        public event Action<List<Vector2Int>> OnBlockPlaced;
        public event Action<List<int>, List<int>> OnLinesCleared;

        public bool CanPlaceBlocks(List<Vector2Int> gridPositions)
        {
            // Forces the system to believe the grid cannot accommodate the blocks
            return false;
        }

        public bool IsCellOccupied(int col, int row) => true;


        public Vector2Int GetGridPositionFromWorld(Vector3 worldPos) => Vector2Int.zero;
        public Vector3 GetWorldPositionFromGrid(Vector2Int gridPos) => Vector3.zero;
        
        public void RequestPreview(List<Vector2Int> gridPositions) 
        {
            // Dummy implementation
        }

        public void PlaceBlocks(List<Vector2Int> gridPositions) 
        {
            // Dummy implementation
        }
    }
}
#endif
