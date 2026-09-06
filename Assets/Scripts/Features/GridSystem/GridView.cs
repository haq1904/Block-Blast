using System.Collections.Generic;
using UnityEngine;

public class GridView : MonoBehaviour
{
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private GameObject shadowPrefab;

    private GameObject[,] visualGrid = new GameObject[8, 8];
    private List<GameObject> activeShadows = new List<GameObject>();
    
    private IGridService gridService;
    private IPoolService poolService;

    private void Start()
    {
        gridService = ServiceLocator.Get<IGridService>();
        poolService = ServiceLocator.Get<IPoolService>();

        gridService.OnPreviewStateChanged += HandlePreview;
        gridService.OnBlockPlaced += HandleBlockPlaced;
        gridService.OnLinesCleared += HandleLinesCleared;
    }

    private void OnDestroy()
    {
        if (gridService != null)
        {
            gridService.OnPreviewStateChanged -= HandlePreview;
            gridService.OnBlockPlaced -= HandleBlockPlaced;
            gridService.OnLinesCleared -= HandleLinesCleared;
        }
    }

    private void HandlePreview(bool isValid, List<Vector2Int> positions)
    {
        // 1. Dọn sạch bóng mờ cũ (trả về Pool)
        foreach (var shadow in activeShadows)
        {
            poolService.ReturnObjectToPool(shadow);
        }
        activeShadows.Clear();

        // 2. Nếu hợp lệ, vẽ bóng mờ mới
        if (isValid && positions != null)
        {
            foreach (var pos in positions)
            {
                Vector3 worldPos = gridService.GetWorldPositionFromGrid(pos);
                GameObject shadow = poolService.SpawnObject(shadowPrefab, worldPos, Quaternion.identity);
                activeShadows.Add(shadow);
            }
        }
    }

    private void HandleBlockPlaced(List<Vector2Int> positions)
    {
        foreach (var pos in positions)
        {
            Vector3 worldPos = gridService.GetWorldPositionFromGrid(pos);
            GameObject block = poolService.SpawnObject(blockPrefab, worldPos, Quaternion.identity);
            visualGrid[pos.x, pos.y] = block;
        }
    }

    private void HandleLinesCleared(List<int> rows, List<int> cols)
    {
        foreach (int row in rows)
        {
            for (int col = 0; col < 8; col++)
            {
                ClearVisualBlock(col, row);
            }
        }

        foreach (int col in cols)
        {
            for (int row = 0; row < 8; row++)
            {
                ClearVisualBlock(col, row);
            }
        }
    }

    private void ClearVisualBlock(int col, int row)
    {
        GameObject block = visualGrid[col, row];
        if (block != null)
        {
            poolService.ReturnObjectToPool(block);
            visualGrid[col, row] = null;
        }
    }
}
