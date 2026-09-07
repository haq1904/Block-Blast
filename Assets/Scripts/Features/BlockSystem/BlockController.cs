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

    private IPoolService poolService;

    // Lắng nghe sự kiện để View biết phải vẽ hình gì
    public event Action<List<(int x, int y)>> OnShapeAssigned;

    // Đổi lại thành Start để tránh lỗi Execution Order khi game vừa bật lên (các Manager chưa kịp đăng ký vào ServiceLocator).
    // Sau này làm Hệ thống Khay (Tray), ta sẽ viết hàm public void Setup() riêng để gọi lúc lấy ra từ Pool.
    private void Start()
    {
        mainCamera = Camera.main;
        gridService = ServiceLocator.Get<IGridService>();
        poolService = ServiceLocator.Get<IPoolService>();

        // Lấy hình dáng ngẫu nhiên

        IBlockService blockService = ServiceLocator.Get<IBlockService>();
        model = blockService.GetRandomShape();

        // Báo cho View biết hình dáng để nó vẽ

        OnShapeAssigned?.Invoke(model.ShapeOffsets);

        // Lưu lại vị trí trên khay để bay về nếu thả trật

        trayPosition = transform.position;

        // Ép Scale nhỏ lại 0.75 khi nằm trên Khay
        transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);

        // Ở đây Sếp sẽ tự code phần View (vẽ các cục gạch con) dựa trên model.ShapeOffsets
        // Hoặc tự kéo thả Prefab gạch con vào GameObject này cho lẹ.
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

        // DÙNG TỌA ĐỘ CỦA BLOCK ĐỂ TÍNH TOÁN

        Vector3 blockPos = transform.position;

        // 2. Kiểm tra vùng an toàn (Xét theo tọa độ của Block)
        if (blockPos.x >= -3.5f && blockPos.x <= 3.5f && blockPos.z >= 1.5f && blockPos.z <= 8.5f)
        {
            // 3. Tính toán ô mà Block đang nằm lên
            Vector2Int centerGridPos = gridService.GetGridPositionFromWorld(blockPos);
            List<Vector2Int> occupiedPositions = new List<Vector2Int>();


            foreach (var offset in model.ShapeOffsets)
            {
                occupiedPositions.Add(centerGridPos + new Vector2Int(offset.x, offset.y));
            }

            // Yêu cầu Grid bật bóng mờ

            gridService.RequestPreview(occupiedPositions);
        }
        else
        {
            // Nếu Block nằm ngoài vùng an toàn -> Tắt bóng mờ
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
        bool isPlaced = false;

        // 2. Xét đặt gạch theo tọa độ của Block
        if (blockPos.x >= -3.5f && blockPos.x <= 3.5f && blockPos.z >= 1.5f && blockPos.z <= 8.5f)
        {
            Vector2Int centerGridPos = gridService.GetGridPositionFromWorld(blockPos);
            List<Vector2Int> occupiedPositions = new List<Vector2Int>();


            foreach (var offset in model.ShapeOffsets)
            {
                occupiedPositions.Add(centerGridPos + new Vector2Int(offset.x, offset.y));
            }

            // Hỏi ý kiến Sếp Grid xem chỗ này có trống không
            if (gridService.CanPlaceBlocks(occupiedPositions))
            {
                // Chốt đơn!
                gridService.PlaceBlocks(occupiedPositions);
                isPlaced = true;
            }
        }

        // Xử lý kết quả sau khi thả
        if (isPlaced)
        {
            // // TẠM THỜI: Vì cục gạch đang đặt tay trong Scene, nó không thuộc về Pool nên trả về sẽ báo lỗi chữ Vàng.
            // // Nếu có lỗi này, ta tạm thời Destroy nó. Sau này có Tray System đẻ ra từ Pool thì xài ReturnObjectToPool.
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
