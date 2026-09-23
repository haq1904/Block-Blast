using System;
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

    private struct ClearingBlock
    {
        public GameObject gameObject;
        public Vector3 canonicalPos;
    }

    private struct ClearCellTarget
    {
        public GameObject gameObject;
        public Vector2Int gridPos;
        public Vector3 canonicalPos;
        public float delay;
    }

    private List<ShadowInstance> activeShadows = new List<ShadowInstance>();
    private List<AnimatingCell> activePreClearCells = new List<AnimatingCell>();
    private List<ClearingBlock> activeClearingBlocks = new List<ClearingBlock>();
    private PreClearAnimationSO currentActivePreClearAnimation;
    private Vector3 lastPlacedWorldCenter = Vector3.zero;

    // Cache to prevent restarting animations/shadows when dragging within the same grid cell
    private List<Vector2Int> lastShadowPositions = new List<Vector2Int>();
    private List<int> lastPreviewRows = new List<int>();
    private List<int> lastPreviewCols = new List<int>();

    private IGridService gridService;
    private IPoolService poolService;
    private IBlockService blockService;
    private ISoundFXService soundService;

    [Header("Line Clear Timing")]
    [Tooltip("Delay in seconds after block placement before full lines explode, ensuring player sees the block placed.")]
    [SerializeField] private float lineClearDelay = 0.20f;

    private Tween lineClearDelayTween;

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
        lineClearDelayTween?.Kill();
        lineClearDelayTween = null;

        ClearActiveShadows();
        CancelCurrentPreClearEffects();
        ClearActiveClearingBlocks();
        lastShadowPositions.Clear();
        lastPreviewRows.Clear();
        lastPreviewCols.Clear();
    }

    private void OnDestroy()
    {
        lineClearDelayTween?.Kill();
        lineClearDelayTween = null;

        if (gridService != null)
        {
            gridService.OnPreviewStateChanged -= HandlePreview;
            gridService.OnBlockPlaced -= HandleBlockPlaced;
            gridService.OnLinesCleared -= HandleLinesCleared;
            gridService.OnPreviewLinesToClear -= HandlePreviewLinesToClear;
        }

        ClearActiveShadows();
        CancelCurrentPreClearEffects();
        ClearActiveClearingBlocks();
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
        var effect = theme?.GetPreClearAnimation();
        if (effect == null) return;
        currentActivePreClearAnimation = effect;

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

        var effect = currentActivePreClearAnimation;

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
        currentActivePreClearAnimation = null;
    }

    private void HandleBlockPlaced(List<CellPlacementData> positions)
    {
        lastShadowPositions.Clear();
        lastPreviewRows.Clear();
        lastPreviewCols.Clear();
        CancelCurrentPreClearEffects();

        var theme = blockService?.CurrentBlockType;
        List<Vector3> cellWorldPositions = new List<Vector3>(positions.Count);
        Vector3 worldCenterSum = Vector3.zero;

        foreach (var cell in positions)
        {
            Vector3 worldPos = gridService.GetWorldPositionFromGrid(cell.gridPos);
            cellWorldPositions.Add(worldPos);
            worldCenterSum += worldPos;

            GameObject prefabToSpawn = blockService != null ? blockService.GetCellPrefab(cell.variantIndex) : null;

            if (prefabToSpawn != null)
            {
                GameObject block = poolService.SpawnObject(prefabToSpawn, worldPos, Quaternion.identity);
                visualGrid[cell.gridPos.x, cell.gridPos.y] = block;

                if (theme != null && theme.placementAnimation != null)
                {
                    theme.placementAnimation.Apply(block.transform);
                }
            }
        }

        if (positions.Count > 0)
        {
            lastPlacedWorldCenter = worldCenterSum / positions.Count;
        }

        // Trigger placement particle effects (smoke puff along exposed edges + debris at cell centers)
        if (theme != null && theme.placementVFX != null && poolService != null && gridService != null)
        {
            List<PlacementEdgeData> edges = gridService.GetExposedEdges(positions);
            theme.placementVFX.Play(edges, cellWorldPositions, poolService);
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

        var cachedRows = rows != null ? new List<int>(rows) : null;
        var cachedCols = cols != null ? new List<int>(cols) : null;
        var cachedCombos = comboPositions != null ? new List<Vector3>(comboPositions) : null;

        lineClearDelayTween?.Kill();

        if (lineClearDelay > 0f)
        {
            lineClearDelayTween = DOVirtual.DelayedCall(lineClearDelay, () =>
            {
                ExecuteLinesCleared(cachedRows, cachedCols, cachedCombos);
                lineClearDelayTween = null;
            }).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }
        else
        {
            ExecuteLinesCleared(cachedRows, cachedCols, cachedCombos);
        }
    }

    private void ExecuteLinesCleared(List<int> rows, List<int> cols, List<Vector3> comboPositions)
    {
        var theme = blockService?.CurrentBlockType;
        var clearEffect = theme?.clearAnimation;
        var stagger = theme?.GetClearStagger();

        // 1. Collect unique cells to clear across rows and columns
        List<ClearCellTarget> cellsToClear = new List<ClearCellTarget>();

        if (rows != null)
        {
            foreach (int row in rows)
            {
                if (row < 0 || row >= 8) continue;
                for (int col = 0; col < 8; col++)
                {
                    TryQueueCellToClear(col, row, col, 8, stagger, cellsToClear);
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
                    TryQueueCellToClear(col, row, row, 8, stagger, cellsToClear);
                }
            }
        }

        // 2. Animate and clear collected cells
        for (int i = 0; i < cellsToClear.Count; i++)
        {
            AnimateAndClearCell(cellsToClear[i], theme, clearEffect, stagger);
        }

        if (soundService != null && theme != null)
        {
            soundService.PlaySound(theme.clearSound);
        }
    }

    private void TryQueueCellToClear(
        int col, 
        int row, 
        int indexInLine, 
        int totalInLine, 
        ClearStaggerSO stagger, 
        List<ClearCellTarget> list)
    {
        GameObject block = visualGrid[col, row];
        if (block == null) return;

        // Immediately unbind from visual grid so the slot is considered free
        visualGrid[col, row] = null;

        Vector2Int gridPos = new Vector2Int(col, row);
        Vector3 canonicalPos = gridService != null 
            ? gridService.GetWorldPositionFromGrid(gridPos) 
            : block.transform.position;

        for (int i = activePreClearCells.Count - 1; i >= 0; i--)
        {
            if (activePreClearCells[i].gameObject == block)
            {
                activePreClearCells.RemoveAt(i);
                break;
            }
        }

        float delay = stagger != null
            ? stagger.CalculateDelay(indexInLine, totalInLine, canonicalPos, lastPlacedWorldCenter)
            : 0f;

        list.Add(new ClearCellTarget
        {
            gameObject = block,
            gridPos = gridPos,
            canonicalPos = canonicalPos,
            delay = delay
        });
    }

    private void AnimateAndClearCell(
        ClearCellTarget target, 
        BlockTypeSO theme, 
        ClearAnimationSO clearEffect, 
        ClearStaggerSO stagger)
    {
        GameObject block = target.gameObject;
        if (block == null) return;

        Vector3 canonicalPos = target.canonicalPos;
        var clearingEntry = new ClearingBlock { gameObject = block, canonicalPos = canonicalPos };
        activeClearingBlocks.Add(clearingEntry);

        // Force canonical ground transform before clear animation (kills mid-air placement or pre-clear tweens)
        block.transform.DOKill();
        block.transform.position = canonicalPos;
        block.transform.rotation = Quaternion.identity;
        block.transform.localScale = Vector3.one;

        Action onExplode = () =>
        {
            if (theme != null && poolService != null)
            {
                Vector3 burstPos = block != null ? block.transform.position : canonicalPos;
                Quaternion burstRot = block != null ? block.transform.rotation : Quaternion.identity;

                if (stagger != null)
                {
                    stagger.Play(burstPos, burstRot, poolService);
                }

                if (theme.clearVFX != null)
                {
                    poolService.SpawnObject(theme.clearVFX, burstPos, Quaternion.identity, PoolType.ParticleSystem);
                }
            }

            if (block != null)
            {
                block.transform.DOKill();
                block.transform.position = canonicalPos;
                block.transform.rotation = Quaternion.identity;
                block.transform.localScale = Vector3.one;

                if (poolService != null)
                {
                    poolService.ReturnObjectToPool(block);
                }
            }

            gridService?.ReleaseClearingCell(target.gridPos);
            activeClearingBlocks.Remove(clearingEntry);
        };

        if (clearEffect != null)
        {
            ClearCellContext context = new ClearCellContext
            {
                gridPos = target.gridPos,
                canonicalPos = canonicalPos,
                indexInLine = 0,
                totalInLine = 8,
                delay = target.delay,
                placementOrigin = lastPlacedWorldCenter
            };

            clearEffect.Play(block.transform, context, onExplode);
        }
        else
        {
            onExplode();
        }
    }

    private void ClearActiveClearingBlocks()
    {
        for (int i = activeClearingBlocks.Count - 1; i >= 0; i--)
        {
            var entry = activeClearingBlocks[i];
            if (entry.gameObject != null)
            {
                entry.gameObject.transform.DOKill();
                entry.gameObject.transform.position = entry.canonicalPos;
                entry.gameObject.transform.rotation = Quaternion.identity;
                entry.gameObject.transform.localScale = Vector3.one;

                if (poolService != null)
                {
                    poolService.ReturnObjectToPool(entry.gameObject);
                }
            }
        }
        activeClearingBlocks.Clear();
        gridService?.ReleaseAllClearingCells();
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
