using System;
using System.Collections.Generic;
using UnityEngine;

public interface IGridService
{
    // --- EVENTS (Dành cho View lắng nghe) ---
    event Action<bool, List<Vector2Int>> OnPreviewStateChanged;
    event Action<List<Vector2Int>> OnBlockPlaced;
    event Action<List<int>, List<int>> OnLinesCleared;

    // --- COMMANDS (Dành cho Block/Input gọi) ---
    Vector2Int GetGridPositionFromWorld(Vector3 worldPos);
    Vector3 GetWorldPositionFromGrid(Vector2Int gridPos);
    void RequestPreview(List<Vector2Int> gridPositions);
    void PlaceBlocks(List<Vector2Int> gridPositions);
}
