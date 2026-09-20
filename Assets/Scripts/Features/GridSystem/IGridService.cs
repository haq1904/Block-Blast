using System;
using System.Collections.Generic;
using UnityEngine;

public interface IGridService
{
    // --- PROPERTIES ---
    int GridWidth { get; }
    int GridHeight { get; }
    int OccupiedCellCount { get; }
    float OccupancyRatio { get; }

    // --- EVENTS (View listeners) ---
    event Action<bool, List<CellPlacementData>> OnPreviewStateChanged;
    event Action<List<CellPlacementData>> OnBlockPlaced;
    event Action<List<int>, List<int>, List<Vector3>> OnLinesCleared;
    event Action<List<int>, List<int>> OnPreviewLinesToClear;
    event Action<int, int, bool> OnPlacementResolved; // (tilesPlaced, totalLinesCleared, isAllClear)

    // --- COMMANDS (Invoked by Block/Input) ---
    Vector2Int GetGridPositionFromWorld(Vector3 worldPos);
    Vector3 GetWorldPositionFromGrid(Vector2Int gridPos);
    bool CanPlaceBlocks(List<CellPlacementData> cells);
    bool CanPlaceBlocks(List<Vector2Int> gridPositions);
    bool IsCellOccupied(int col, int row);
    void RequestPreview(List<CellPlacementData> cells);
    void RequestPreview(List<Vector2Int> gridPositions);
    void PlaceBlocks(List<CellPlacementData> cells);
    void PlaceBlocks(List<Vector2Int> gridPositions);

    // --- PRESENTATION HELPERS (Humble View Support) ---
    List<Vector3> GetComboVFXPositions(List<int> rows, List<int> cols) => new List<Vector3>();
    Vector3 GetWorldCenter(List<Vector2Int> gridPositions) => Vector3.zero;
    List<PlacementEdgeData> GetExposedEdges(List<CellPlacementData> cells) => new List<PlacementEdgeData>();
}
