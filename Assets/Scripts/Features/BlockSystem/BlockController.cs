using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BlockController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private BlockModel model;
    private IGridService gridService;
    private Camera mainCamera;

    // Original tray position to return to if drop fails
    private Vector3 trayPosition;
    private int slotIndex;

    private IPoolService poolService;
    private Vector2Int lastOriginGridPos = new Vector2Int(int.MinValue, int.MinValue);
    private bool wasAllInBounds = false;

    // Pure C# events for View communication (Tier 1 Event standard)
    public event Action<List<(int x, int y)>, Vector2, int[]> OnShapeAssigned;
    public event Action<Vector3> OnTrayPositionSet;
    public event Action<Vector3> OnDragPositionUpdated;
    public event Action<Vector3> OnDragVelocityUpdated;
    public event Action OnDragStarted;
    public event Action<Vector3> OnBlockLiftRequested;
    public event Action<Vector3, Action> OnBlockPlaceSlideRequested;
    public event Action<bool> OnDragEnded;
    public event Action OnBlockReset;

    private Vector2 CenterOffset => model != null ? model.CenterOffset : Vector2.zero;

    private bool isDragging;
    private int activePointerId = -1;
    private Vector3 lastDragPosition;

    private void Awake()
    {
        mainCamera = Camera.main;
        gridService = ServiceLocator.Get<IGridService>();
        poolService = ServiceLocator.Get<IPoolService>();
    }

    private void Update()
    {
        if (isDragging)
        {
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 displacement = transform.position - lastDragPosition;
            Vector3 velocity = displacement / dt;
            lastDragPosition = transform.position;
            OnDragVelocityUpdated?.Invoke(velocity);
        }
    }

    private void OnDisable()
    {
        if (ServiceLocator.TryGet<IBlockService>(out var blockService))
        {
            blockService.ReleaseDragLock(this);
        }

        if (isDragging)
        {
            isDragging = false;
            activePointerId = -1;
            OnDragEnded?.Invoke(false);
        }
    }

    public void Setup(BlockModel newModel, Vector3 trayPos, int slotId)
    {
        model = newModel;
        trayPosition = trayPos;
        slotIndex = slotId;
        isDragging = false;
        activePointerId = -1;

        OnTrayPositionSet?.Invoke(trayPosition);
        OnShapeAssigned?.Invoke(model.ShapeOffsets, model.CenterOffset, model.VariantIds);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (ServiceLocator.TryGet<IBlockService>(out var blockService) && !blockService.TryAcquireDragLock(this, eventData.pointerId))
        {
            // Another block is already actively dragging -> reject this drag attempt
            return;
        }

        activePointerId = eventData.pointerId;
        lastOriginGridPos = new Vector2Int(int.MinValue, int.MinValue);
        wasAllInBounds = false;

        // On initial touch: slide up to height y=1 and advance z+2
        Vector3 targetLiftPos = new Vector3(trayPosition.x, 1f, trayPosition.z + 2f);
        isDragging = true;
        lastDragPosition = targetLiftPos;
        OnDragStarted?.Invoke();
        OnBlockLiftRequested?.Invoke(targetLiftPos);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || eventData.pointerId != activePointerId) return;
        // 1. Get pointer world position projected onto plane y=0
        Vector3 mouseWorld = GetWorldPositionFromMouse(eventData.position);

        // Update displayed block position (maintaining y=1 and z=z+2 offset from pointer)
        Vector3 dragPos = new Vector3(mouseWorld.x, 1f, mouseWorld.z + 2f);
        OnDragPositionUpdated?.Invoke(dragPos);

        Vector2 center = CenterOffset;
        Vector3 originWorldPos = dragPos - new Vector3(center.x, 0, center.y);
        Vector2Int originGridPos = gridService.GetGridPositionFromWorld(originWorldPos);

        List<CellPlacementData> placementData = GetCellPlacementData(originGridPos, out bool allInBounds);

        // Suppress redundant calls when pointer moves within the same grid coordinate
        if (originGridPos == lastOriginGridPos && allInBounds == wasAllInBounds)
        {
            return;
        }

        lastOriginGridPos = originGridPos;
        wasAllInBounds = allInBounds;

        if (allInBounds)
        {
            gridService.RequestPreview(placementData);
        }
        else
        {
            gridService.RequestPreview(new List<CellPlacementData>());
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging || eventData.pointerId != activePointerId) return;

        isDragging = false;
        activePointerId = -1;

        lastOriginGridPos = new Vector2Int(int.MinValue, int.MinValue);
        wasAllInBounds = false;

        // Dismiss ghost preview immediately
        gridService.RequestPreview(new List<CellPlacementData>());

        // 1. Recalculate pointer position on release to finalize block position
        Vector3 mouseWorld = GetWorldPositionFromMouse(eventData.position);
        Vector3 releasePos = new Vector3(mouseWorld.x, 1f, mouseWorld.z + 2f);
        OnDragPositionUpdated?.Invoke(releasePos);

        Vector2 center = CenterOffset;
        Vector3 originWorldPos = releasePos - new Vector3(center.x, 0, center.y);
        Vector2Int originGridPos = gridService.GetGridPositionFromWorld(originWorldPos);

        List<CellPlacementData> placementData = GetCellPlacementData(originGridPos, out bool allInBounds);
        bool isPlaced = false;

        // 2. Evaluate block placement on the grid
        if (allInBounds && gridService.CanPlaceBlocks(placementData))
        {
            isPlaced = true;
        }

        if (isPlaced)
        {
            Vector3 snappedOrigin = gridService.GetWorldPositionFromGrid(originGridPos);
            Vector3 groundSnapPos = new Vector3(snappedOrigin.x + center.x, -1f, snappedOrigin.z + center.y);

            OnDragEnded?.Invoke(true);

            OnBlockPlaceSlideRequested?.Invoke(groundSnapPos, () =>
            {
                if (ServiceLocator.TryGet<IBlockService>(out var blockService))
                {
                    blockService.ReleaseDragLock(this);
                }

                // Placement confirmed on grid!
                gridService.PlaceBlocks(placementData);
                OnBlockReset?.Invoke();

                // 1. Return block to pool before triggering next batch spawn
                if (blockService != null)
                {
                    blockService.DespawnBlock(slotIndex);
                }
                else if (poolService != null)
                {
                    try
                    {
                        poolService.ReturnObjectToPool(gameObject);
                    }
                    catch
                    {
                        Destroy(gameObject);
                    }
                }
                else
                {
                    Destroy(gameObject);
                }

                // 2. Once block is safely returned to pool, notify SpawnService
                if (ServiceLocator.TryGet<ISpawnService>(out var spawnService))
                {
                    spawnService.MarkSlotEmpty(slotIndex);
                }
            });
        }
        else
        {
            if (ServiceLocator.TryGet<IBlockService>(out var blockService))
            {
                blockService.ReleaseDragLock(this);
            }

            OnDragEnded?.Invoke(false);
            // Failed placement (blocked or dropped out of bounds) -> return to tray
            OnTrayPositionSet?.Invoke(trayPosition);
        }
    }

    private List<CellPlacementData> GetCellPlacementData(Vector2Int originGridPos, out bool allInBounds)
    {
        allInBounds = true;
        var result = new List<CellPlacementData>();

        if (model != null && model.ShapeOffsets != null)
        {
            for (int i = 0; i < model.ShapeOffsets.Count; i++)
            {
                var offset = model.ShapeOffsets[i];
                Vector2Int pos = originGridPos + new Vector2Int(offset.x, offset.y);
                if (pos.x < 0 || pos.x >= gridService.GridWidth || pos.y < 0 || pos.y >= gridService.GridHeight)
                {
                    allInBounds = false;
                }

                int variantId = (model.VariantIds != null && i < model.VariantIds.Length) ? model.VariantIds[i] : 0;
                result.Add(new CellPlacementData(pos, model.BlockTypeId, variantId));
            }
        }

        return result;
    }

    // Helper: Raycast from Camera to plane y=0 to find 3D pointer coordinates
    private Vector3 GetWorldPositionFromMouse(Vector2 screenPos)
    {
        Plane plane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = mainCamera.ScreenPointToRay(screenPos);


        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }


        return transform.position;
    }
}
