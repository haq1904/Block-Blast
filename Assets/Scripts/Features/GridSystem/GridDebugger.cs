using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GridDebugger : MonoBehaviour
{
    private IGridService gridService;

    void Start()
    {
        // Chờ 1 khung hình để đảm bảo GridController đã đăng ký xong Service
        Invoke(nameof(Init), 0.1f);
    }

    void Init()
    {
        gridService = ServiceLocator.Get<IGridService>();
        Debug.Log("[GridDebugger] Đã kết nối với Sếp GridController! Bấm phím 1,2,3,4 để test.");
    }

    void Update()
    {
        if (gridService == null) return;
        if (Keyboard.current == null) return; // Tránh lỗi văng nếu máy không có bàn phím

        // Phím 1: Bật bóng mờ ở 3 ô đầu tiên của dòng 0
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            List<Vector2Int> testPos = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
            gridService.RequestPreview(testPos);
        }

        // Phím 2: Cố tình gửi tọa độ sai (Nằm ngoài mảng) để xem nó có tắt bóng mờ không
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            List<Vector2Int> badPos = new List<Vector2Int> { new Vector2Int(9, 9) };
            gridService.RequestPreview(badPos);
        }

        // Phím 3: Đặt gạch thật xuống 3 ô đầu tiên của dòng 0
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            List<Vector2Int> testPos = new List<Vector2Int> { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) };
            gridService.PlaceBlocks(testPos);
            // Sau khi đặt thật, tắt bóng mờ đi
            gridService.RequestPreview(new List<Vector2Int>());
        }

        // Phím 4: Đặt nốt 5 viên gạch còn lại của dòng 0 để KÍCH HOẠT NỔ HÀNG
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            List<Vector2Int> remainingBlocks = new List<Vector2Int>();
            for (int i = 3; i < 8; i++) remainingBlocks.Add(new Vector2Int(i, 0));
            gridService.PlaceBlocks(remainingBlocks);
        }

        // --- CÁC TRƯỜNG HỢP TEST MỚI THÊM ---

        // Phím 5: Dọn cỗ CỘT DỌC. Đặt 7 viên gạch ở Cột 7 (chừa lại ô trên cùng [7,7])
        if (Keyboard.current.digit5Key.wasPressedThisFrame)
        {
            List<Vector2Int> colBlocks = new List<Vector2Int>();
            for (int i = 0; i < 7; i++) colBlocks.Add(new Vector2Int(7, i));
            gridService.PlaceBlocks(colBlocks);
        }

        // Phím 6: Chốt hạ NỔ CỘT DỌC. Ném viên gạch cuối cùng vào ô [7,7]
        if (Keyboard.current.digit6Key.wasPressedThisFrame)
        {
            gridService.PlaceBlocks(new List<Vector2Int> { new Vector2Int(7, 7) });
        }

        // Phím 7: Dọn cỗ COMBO 2 HÀNG. Lấp đầy Hàng 2 và Hàng 3, nhưng CHỪA LẠI Cột số 4
        if (Keyboard.current.digit7Key.wasPressedThisFrame)
        {
            List<Vector2Int> comboBlocks = new List<Vector2Int>();
            for (int col = 0; col < 8; col++)
            {
                if (col == 4) continue; // Trống ở giữa
                comboBlocks.Add(new Vector2Int(col, 2));
                comboBlocks.Add(new Vector2Int(col, 3));
            }
            gridService.PlaceBlocks(comboBlocks);
        }

        // Phím 8: Chốt hạ COMBO 2 HÀNG. Thả cục gạch dọc (1x2) vào ngay cái khe hở Cột 4
        if (Keyboard.current.digit8Key.wasPressedThisFrame)
        {
            gridService.PlaceBlocks(new List<Vector2Int> { new Vector2Int(4, 2), new Vector2Int(4, 3) });
        }
    }
}
