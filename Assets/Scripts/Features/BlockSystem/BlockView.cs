using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(BlockController))]
public class BlockView : MonoBehaviour
{
    [SerializeField] private GameObject cellPrefab;

    private BlockController blockController;
    private IPoolService poolService;
    private IBlockService blockService;
    private List<GameObject> activeCells = new List<GameObject>();

    private void Awake()
    {
        blockController = GetComponent<BlockController>();
        blockController.OnShapeAssigned += DrawShape;
    }

    private void OnDestroy()
    {
        if (blockController != null)
        {
            blockController.OnShapeAssigned -= DrawShape;
        }

        foreach (var cell in activeCells)
        {
            if (cell != null)
            {
                cell.transform.DOKill();
            }
        }
    }

    private void DrawShape(List<(int x, int y)> offsets)
    {
        if (poolService == null) poolService = ServiceLocator.Get<IPoolService>();
        if (blockService == null) blockService = ServiceLocator.Get<IBlockService>();

        ClearShape();

        Vector2 center = blockController != null ? blockController.CenterOffset : Vector2.zero;
        int[] variantIds = blockController?.Model?.VariantIds;

        for (int i = 0; i < offsets.Count; i++)
        {
            var offset = offsets[i];
            int variantId = (variantIds != null && i < variantIds.Length) ? variantIds[i] : 0;

            GameObject prefabToSpawn = null;
            if (blockService != null)
            {
                prefabToSpawn = blockService.GetCellPrefab(variantId);
            }

            if (prefabToSpawn == null)
            {
                prefabToSpawn = cellPrefab;
            }

            if (prefabToSpawn == null)
            {
                Debug.LogError("BlockView: No cell prefab found for " + gameObject.name);
                continue;
            }

            GameObject cell = poolService.SpawnObject(prefabToSpawn, Vector3.zero, Quaternion.identity);
            cell.transform.SetParent(this.transform, false);
            cell.transform.localScale = Vector3.one;
            cell.transform.localPosition = new Vector3(offset.x - center.x, 0, offset.y - center.y);
            activeCells.Add(cell);
        }
    }

    private void ClearShape()
    {
        if (poolService == null) return;

        foreach (var cell in activeCells)
        {
            if (cell != null)
            {
                cell.transform.DOKill();
                cell.transform.localScale = Vector3.one;
                cell.transform.localRotation = Quaternion.identity;
                poolService.ReturnObjectToPool(cell);
            }
        }
        activeCells.Clear();
    }
}
