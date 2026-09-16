using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class BlockController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private BlockModel model;
    private IGridService gridService;
    private Camera mainCamera;

    // Tọa độ gốc trên Khay để bay về nếu thả trượt (Chỉ view Controller mới quan tâm tọa độ thực)
    private Vector3 trayPosition;
    private int slotIndex;

    private IPoolService poolService;

    // Lắng nghe sự kiện để View biết phải vẽ hình gì
    public event Action<List<(int x, int y)>> OnShapeAssigned;
    public Vector2 CenterOffset => model != null ? model.CenterOffset : Vector2.zero;

    private void Awake()
    {
        mainCamera = Camera.main;
        gridService = ServiceLocator.Get<IGridService>();
        poolService = ServiceLocator.Get<IPoolService>();
    }

    public void Setup(BlockModel newModel, Vector3 trayPos, int slotId)
    {
        model = newModel;
        trayPosition = trayPos;
        slotIndex = slotId;
        transform.position = trayPosition;
        transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);

        OnShapeAssigned?.Invoke(model.ShapeOffsets);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Khi vừa chạm vào: Trượt lên độ cao y=1 và tiến tới z+2
        Vector3 newPos = transform.position;
        newPos.y = 1f;
        newPos.z += 2f;
        transform.position = newPos;

        // Phóng to lại kích thước gốc 1:1 để chuẩn bị ướm vào bàn cờ
        transform.localScale = Vector3.one;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 1. Lấy tọa độ thật của con trỏ chuột/ngón tay chiếu xuống mặt phẳng y=0
        Vector3 mouseWorld = GetWorldPositionFromMouse(eventData.position);

        // Cập nhật vị trí hiển thị cục gạch (luôn giữ offset y=1 và z=z+2 so với ngón tay)
        transform.position = new Vector3(mouseWorld.x, 1f, mouseWorld.z + 2f);

        Vector3 blockPos = transform.position;
        Vector2 center = CenterOffset;
        Vector3 originWorldPos = blockPos - new Vector3(center.x, 0, center.y);
        Vector2Int originGridPos = gridService.GetGridPositionFromWorld(originWorldPos);

        List<Vector2Int> occupiedPositions = new List<Vector2Int>();
        bool allInBounds = true;

        if (model != null && model.ShapeOffsets != null)
        {
            foreach (var offset in model.ShapeOffsets)
            {
                Vector2Int pos = originGridPos + new Vector2Int(offset.x, offset.y);
                if (pos.x < 0 || pos.x >= gridService.GridWidth || pos.y < 0 || pos.y >= gridService.GridHeight)
                {
                    allInBounds = false;
                }
                occupiedPositions.Add(pos);
            }
        }

        if (allInBounds)
        {
            gridService.RequestPreview(occupiedPositions);
        }
        else
        {
            gridService.RequestPreview(new List<Vector2Int>());
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // Tắt bóng mờ ngay lập tức
        gridService.RequestPreview(new List<Vector2Int>());

        // 1. Tính toán lại vị trí chuột lúc thả tay để chốt vị trí Block
        Vector3 mouseWorld = GetWorldPositionFromMouse(eventData.position);
        transform.position = new Vector3(mouseWorld.x, 1f, mouseWorld.z + 2f);

        Vector3 blockPos = transform.position;
        Vector2 center = CenterOffset;
        Vector3 originWorldPos = blockPos - new Vector3(center.x, 0, center.y);
        Vector2Int originGridPos = gridService.GetGridPositionFromWorld(originWorldPos);

        List<Vector2Int> occupiedPositions = new List<Vector2Int>();
        bool allInBounds = true;

        if (model != null && model.ShapeOffsets != null)
        {
            foreach (var offset in model.ShapeOffsets)
            {
                Vector2Int pos = originGridPos + new Vector2Int(offset.x, offset.y);
                if (pos.x < 0 || pos.x >= gridService.GridWidth || pos.y < 0 || pos.y >= gridService.GridHeight)
                {
                    allInBounds = false;
                }
                occupiedPositions.Add(pos);
            }
        }

        bool isPlaced = false;

        // 2. Xét đặt gạch theo tọa độ của Block
        if (allInBounds && gridService.CanPlaceBlocks(occupiedPositions))
        {
            // Chốt đơn!
            gridService.PlaceBlocks(occupiedPositions);
            isPlaced = true;
        }

        // Xử lý kết quả sau khi thả
        if (isPlaced)
        {
            // Báo cho SpawnService biết là slot này đã trống
            var spawnService = ServiceLocator.Get<ISpawnService>();
            if (spawnService != null)
            {
                spawnService.MarkSlotEmpty(slotIndex);
            }

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
            // Thất bại (Cấn gạch hoặc thả rớt ra ngoài) -> Trả về khay
            transform.position = trayPosition;

            // Thu nhỏ lại thành 0.75 khi nằm trên Khay

            transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
        }
    }

    // Hàm phụ trợ: Bắn tia Ray từ Camera xuống mặt phẳng y=0 để tìm tọa độ 3D của ngón tay
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
