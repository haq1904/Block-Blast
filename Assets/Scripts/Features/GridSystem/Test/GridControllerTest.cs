using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class GridControllerTest
{
    private GameObject gridObject;
    private GridController gridController;

    // [SetUp] là hàm tự động chạy TRƯỚC MỖI kịch bản Test
    [SetUp]
    public void Setup()
    {
        // Tạo một GameObject ảo tàng hình trong bộ nhớ
        gridObject = new GameObject("TestGridController");
        
        // Gắn script GridController vào nó 
        gridController = gridObject.AddComponent<GridController>();

        // LƯU Ý QUAN TRỌNG: Trong môi trường EditMode Test của Unity, hàm Awake() của MonoBehaviour 
        // đôi khi KHÔNG ĐƯỢC TỰ ĐỘNG GỌI. Dẫn tới biến 'model' bị Null.
        // Ta phải dùng Reflection để ép nó chạy hàm Awake() bằng tay.
        typeof(GridController).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(gridController, null);
    }

    // [TearDown] là hàm tự động chạy SAU KHI Test xong để dọn dẹp rác
    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(gridObject);
    }

    [Test]
    public void GetGridPositionFromWorld_ReturnsCorrectCoordinates()
    {
        // --- TÌNH HUỐNG 1: TRÚNG NGAY ĐIỂM NEO GỐC ---
        // Gửi vào tọa độ y hệt điểm neo (StartX = -3.5f, StartZ = 1.5f)
        Vector2Int result1 = gridController.GetGridPositionFromWorld(new Vector3(-3.5f, 0f, 1.5f));
        
        // Assert: Phải trả về đúng Cột 0, Hàng 0
        Assert.AreEqual(new Vector2Int(0, 0), result1);

        // --- TÌNH HUỐNG 2: DỊCH CHUYỂN SANG Ô KHÁC ---
        // Nhích sang phải 2 ô, nhích lên trên 1 ô 
        // X = -3.5 + 2 = -1.5f
        // Z = 1.5 + 1 = 2.5f
        Vector2Int result2 = gridController.GetGridPositionFromWorld(new Vector3(-1.5f, 0f, 2.5f));
        
        // Assert: Phải trả về đúng Cột 2, Hàng 1
        Assert.AreEqual(new Vector2Int(2, 1), result2);

        // --- TÌNH HUỐNG 3: THẢ BỊ LỆCH (TEST LÀM TRÒN NAM CHÂM) ---
        // Thả ở X = -3.2 (Cách gốc -3.5 một khoảng 0.3) -> Làm tròn về 0
        // Thả ở Z = 1.8 (Cách gốc 1.5 một khoảng 0.3) -> Làm tròn về 0
        Vector2Int result3 = gridController.GetGridPositionFromWorld(new Vector3(-3.2f, 0f, 1.8f));
        
        // Assert: Nam châm phải tự động hút nó về ô [0, 0]
        Assert.AreEqual(new Vector2Int(0, 0), result3);
    }

    [Test]
    public void PlaceBlocks_FiresBlockPlacedEvent()
    {
        bool eventFired = false;
        
        // Đăng ký nghe lén cái loa (Event)
        gridController.OnBlockPlaced += (positions) => { eventFired = true; };
        
        // Thả 1 cục gạch
        gridController.PlaceBlocks(new List<Vector2Int> { new Vector2Int(0, 0) });
        
        // Loa phải được phát ra
        Assert.IsTrue(eventFired);
    }

    [Test]
    public void PlaceBlocks_WhenLineFull_FiresLinesClearedEvent()
    {
        bool eventFired = false;
        int clearedRowCount = 0;
        
        // Đăng ký nghe lén loa báo nổ hàng
        gridController.OnLinesCleared += (rows, cols) => {
            eventFired = true;
            clearedRowCount = rows.Count;
        };
        
        // Bước 1: Xếp gạch đầy 7 ô đầu tiên của Hàng 0 (Thiếu 1 ô cuối)
        var blocks = new List<Vector2Int>();
        for (int i = 0; i < 7; i++) blocks.Add(new Vector2Int(i, 0));
        gridController.PlaceBlocks(blocks);
        
        Assert.IsFalse(eventFired); // Chưa đầy nên loa không được nổ

        // Bước 2: Ném nốt viên gạch cuối cùng vào ô [7, 0] để lấp đầy hàng
        gridController.PlaceBlocks(new List<Vector2Int> { new Vector2Int(7, 0) });
        
        // Assert: Đã lấp đầy, loa nổ phải được phát ra và gom đúng 1 hàng
        Assert.IsTrue(eventFired);
        Assert.AreEqual(1, clearedRowCount);
    }
}
