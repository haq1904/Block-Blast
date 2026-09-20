using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class GridView : MonoBehaviour
{
    private GameObject[,] visualGrid = new GameObject[8, 8];

    private struct RendererMatBackup
    {
        public Renderer renderer;
        public Material originalMaterial;
    }

    private class ShadowInstance
    {
        public GameObject gameObject;
        public List<RendererMatBackup> materialBackups = new List<RendererMatBackup>();
    }

    private struct AnimatingCell
    {
        public GameObject gameObject;
        public Vector2Int gridPos;
    }

    private List<ShadowInstance> activeShadows = new List<ShadowInstance>();
    private List<AnimatingCell> activePreClearCells = new List<AnimatingCell>();

    // Cache to prevent restarting animations/shadows when dragging within the same grid cell
    private List<Vector2Int> lastShadowPositions = new List<Vector2Int>();
    private List<int> lastPreviewRows = new List<int>();
    private List<int> lastPreviewCols = new List<int>();

    private IGridService gridService;
    private IPoolService poolService;
    private IBlockService blockService;
    private ISoundFXService soundService;

    private void Start()
    {
        gridService = ServiceLocator.Get<IGridService>();
        poolService = ServiceLocator.Get<IPoolService>();
        blockService = ServiceLocator.Get<IBlockService>();
        ServiceLocator.TryGet<ISoundFXService>(out soundService);

        if (gridService != null)
        {
            gridService.OnPreviewStateChanged += HandlePreview;
            gridService.OnBlockPlaced += HandleBlockPlaced;
            gridService.OnLinesCleared += HandleLinesCleared;
            gridService.OnPreviewLinesToClear += HandlePreviewLinesToClear;
        }
    }

    private void OnDisable()
    {
        ClearActiveShadows();
        CancelCurrentPreClearEffects();
        lastShadowPositions.Clear();
        lastPreviewRows.Clear();
        lastPreviewCols.Clear();
    }

    private void OnDestroy()
    {
        if (gridService != null)
        {
            gridService.OnPreviewStateChanged -= HandlePreview;
            gridService.OnBlockPlaced -= HandleBlockPlaced;
            gridService.OnLinesCleared -= HandleLinesCleared;
            gridService.OnPreviewLinesToClear -= HandlePreviewLinesToClear;
        }

        ClearActiveShadows();
        CancelCurrentPreClearEffects();
        lastShadowPositions.Clear();
        lastPreviewRows.Clear();
        lastPreviewCols.Clear();
    }

    private void HandlePreview(bool isValid, List<CellPlacementData> positions)
    {
        if (!isValid || positions == null || positions.Count == 0)
        {
            if (activeShadows.Count > 0)
            {
                ClearActiveShadows();
                lastShadowPositions.Clear();
            }
            return;
        }

        // If shadow positions are identical to currently active shadows, skip recreating
        if (ArePositionsEqual(lastShadowPositions, positions))
        {
            return;
        }

        lastShadowPositions.Clear();
        for (int i = 0; i < positions.Count; i++)
        {
            lastShadowPositions.Add(positions[i].gridPos);
        }

        ClearActiveShadows();

        var theme = blockService?.CurrentBlockType;
        Material shadowMat = theme != null ? theme.shadowMaterial : null;

        foreach (var cell in positions)
        {
            Vector3 worldPos = gridService.GetWorldPositionFromGrid(cell.gridPos);

            GameObject prefabToSpawn = blockService != null ? blockService.GetCellPrefab(cell.variantIndex) : null;
            if (prefabToSpawn == null) continue;

            GameObject shadowObj = poolService.SpawnObject(prefabToSpawn, worldPos, Quaternion.identity);
            var shadowInstance = new ShadowInstance { gameObject = shadowObj };

            if (shadowMat != null)
            {
                var renderers = shadowObj.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < renderers.Length; i++)
                {
                    var r = renderers[i];
                    shadowInstance.materialBackups.Add(new RendererMatBackup
                    {
                        renderer = r,
                        originalMaterial = r.sharedMaterial
                    });
                    r.sharedMaterial = shadowMat;
                }
            }

            activeShadows.Add(shadowInstance);
        }
    }

    private void ClearActiveShadows()
    {
        if (poolService == null) return;

        for (int i = 0; i < activeShadows.Count; i++)
        {
            var shadow = activeShadows[i];
            if (shadow.gameObject != null)
            {
                for (int j = 0; j < shadow.materialBackups.Count; j++)
                {
                    var backup = shadow.materialBackups[j];
                    if (backup.renderer != null && backup.originalMaterial != null)
                    {
                        backup.renderer.sharedMaterial = backup.originalMaterial;
                    }
                }

                shadow.gameObject.transform.DOKill();
                shadow.gameObject.transform.localScale = Vector3.one;
                shadow.gameObject.transform.localRotation = Quaternion.identity;
                poolService.ReturnObjectToPool(shadow.gameObject);
            }
        }
        activeShadows.Clear();
    }

    private void HandlePreviewLinesToClear(List<int> rows, List<int> cols)
    {
        // If the candidate rows and cols are identical to what is already animating, keep running
        if (AreListsEqual(lastPreviewRows, rows) && AreListsEqual(lastPreviewCols, cols))
        {
            return;
        }

        lastPreviewRows = rows != null ? new List<int>(rows) : new List<int>();
        lastPreviewCols = cols != null ? new List<int>(cols) : new List<int>();

        CancelCurrentPreClearEffects();

        bool hasLines = (rows != null && rows.Count > 0) || (cols != null && cols.Count > 0);
        if (!hasLines) return;

        var theme = blockService?.CurrentBlockType;
        var effect = theme?.preClearEffect;
        if (effect == null) return;

        Dictionary<GameObject, Vector2Int> affectedBlocks = new Dictionary<GameObject, Vector2Int>();

        if (rows != null)
        {
            foreach (int row in rows)
            {
                if (row < 0 || row >= 8) continue;
                for (int col = 0; col < 8; col++)
                {
                    var block = visualGrid[col, row];
                    if (block != null && !affectedBlocks.ContainsKey(block))
                    {
                        affectedBlocks.Add(block, new Vector2Int(col, row));
                    }
                }
            }
        }

        if (cols != null)
        {
            foreach (int col in cols)
            {
                if (col < 0 || col >= 8) continue;
                for (int row = 0; row < 8; row++)
                {
                    var block = visualGrid[col, row];
                    if (block != null && !affectedBlocks.ContainsKey(block))
                    {
                        affectedBlocks.Add(block, new Vector2Int(col, row));
                    }
                }
            }
        }

        Vector3 previewCenterWorld = (lastShadowPositions != null && lastShadowPositions.Count > 0 && gridService != null)
            ? gridService.GetWorldCenter(lastShadowPositions)
            : Vector3.zero;

        foreach (var kvp in affectedBlocks)
        {
            GameObject block = kvp.Key;
            Vector2Int gridPos = kvp.Value;
            Vector3 canonicalPos = gridService != null 
                ? gridService.GetWorldPositionFromGrid(gridPos) 
                : block.transform.position;

            // Clean hand-off: Kill any active placement or movement tween and force to canonical state
            block.transform.DOKill();
            block.transform.position = canonicalPos;
            block.transform.rotation = Quaternion.identity;
            block.transform.localScale = Vector3.one;

            activePreClearCells.Add(new AnimatingCell
            {
                gameObject = block,
                gridPos = gridPos
            });

            effect.Apply(block.transform, previewCenterWorld);
        }
    }

    private void CancelCurrentPreClearEffects()
    {
        if (activePreClearCells.Count == 0) return;

        var theme = blockService?.CurrentBlockType;
        var effect = theme?.preClearEffect;

        for (int i = 0; i < activePreClearCells.Count; i++)
        {
            var anim = activePreClearCells[i];
            if (anim.gameObject != null)
            {
                Vector3 canonicalPos = gridService != null 
                    ? gridService.GetWorldPositionFromGrid(anim.gridPos) 
                    : anim.gameObject.transform.position;

                if (effect != null)
                {
                    effect.Cancel(anim.gameObject.transform, canonicalPos, Quaternion.identity);
                }

                // Explicit safety net: ensure transform is 100% canonical after cancel
                anim.gameObject.transform.DOKill();
                anim.gameObject.transform.position = canonicalPos;
                anim.gameObject.transform.rotation = Quaternion.identity;
                anim.gameObject.transform.localScale = Vector3.one;
            }
        }
        activePreClearCells.Clear();
    }

    private void HandleBlockPlaced(List<CellPlacementData> positions)
    {
        lastShadowPositions.Clear();
        lastPreviewRows.Clear();
        lastPreviewCols.Clear();

        var theme = blockService?.CurrentBlockType;

        foreach (var cell in positions)
        {
            Vector3 worldPos = gridService.GetWorldPositionFromGrid(cell.gridPos);

            GameObject prefabToSpawn = blockService != null ? blockService.GetCellPrefab(cell.variantIndex) : null;

            if (prefabToSpawn != null)
            {
                GameObject block = poolService.SpawnObject(prefabToSpawn, worldPos, Quaternion.identity);
                visualGrid[cell.gridPos.x, cell.gridPos.y] = block;

                if (theme != null)
                {
                    if (theme.placementEffect != null)
                    {
                        theme.placementEffect.Apply(block.transform);
                    }

                    if (theme.placeVFXPrefab != null && poolService != null)
                    {
                        poolService.SpawnObject(theme.placeVFXPrefab, worldPos, Quaternion.identity, PoolType.ParticleSystem);
                    }
                }
            }
        }

        if (soundService != null && theme != null)
        {
            soundService.PlaySound(theme.placeSound);
        }
    }

    private void HandleLinesCleared(List<int> rows, List<int> cols, List<Vector3> comboPositions)
    {
        lastPreviewRows.Clear();
        lastPreviewCols.Clear();

        var theme = blockService?.CurrentBlockType;

        // Spawn combo celebration VFX if multiple lines are cleared simultaneously (pre-calculated by Controller)
        if (comboPositions != null && comboPositions.Count > 0 && theme != null && theme.comboClearVFXPrefab != null && poolService != null)
        {
            for (int i = 0; i < comboPositions.Count; i++)
            {
                poolService.SpawnObject(theme.comboClearVFXPrefab, comboPositions[i], Quaternion.identity, PoolType.ParticleSystem);
            }
        }

        if (rows != null)
        {
            foreach (int row in rows)
            {
                for (int col = 0; col < 8; col++)
                {
                    ClearVisualBlock(col, row, theme);
                }
            }
        }

        if (cols != null)
        {
            foreach (int col in cols)
            {
                for (int row = 0; row < 8; row++)
                {
                    ClearVisualBlock(col, row, theme);
                }
            }
        }

        if (soundService != null && theme != null)
        {
            soundService.PlaySound(theme.clearSound);
        }
    }

    private void ClearVisualBlock(int col, int row, BlockTypeSO theme)
    {
        GameObject block = visualGrid[col, row];
        if (block != null)
        {
            Vector3 canonicalPos = gridService != null 
                ? gridService.GetWorldPositionFromGrid(new Vector2Int(col, row)) 
                : block.transform.position;

            for (int i = activePreClearCells.Count - 1; i >= 0; i--)
            {
                if (activePreClearCells[i].gameObject == block)
                {
                    activePreClearCells.RemoveAt(i);
                    break;
                }
            }

            if (theme != null && theme.clearVFXPrefab != null && poolService != null)
            {
                poolService.SpawnObject(theme.clearVFXPrefab, canonicalPos, Quaternion.identity, PoolType.ParticleSystem);
            }

            block.transform.DOKill();
            block.transform.position = canonicalPos;
            block.transform.rotation = Quaternion.identity;
            block.transform.localScale = Vector3.one;
            poolService.ReturnObjectToPool(block);
            visualGrid[col, row] = null;
        }
    }

    private static bool AreListsEqual(List<int> a, List<int> b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }

    private static bool ArePositionsEqual(List<Vector2Int> cached, List<CellPlacementData> current)
    {
        if (cached == null && current == null) return true;
        if (cached == null || current == null) return false;
        if (cached.Count != current.Count) return false;
        for (int i = 0; i < cached.Count; i++)
        {
            if (cached[i] != current[i].gridPos) return false;
        }
        return true;
    }
}
