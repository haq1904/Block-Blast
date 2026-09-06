using NUnit.Framework;

public class GridModelTest
{
    [Test]
    public void GridModel_Initialization_HasCorrectDimensions()
    {
        // Khởi tạo một bàn cờ 8x8
        var model = new GridModel(8, 8);

        Assert.AreEqual(8, model.Cols);
        Assert.AreEqual(8, model.Rows);
    }

    [Test]
    public void IsWithinBounds_ValidCoordinates_ReturnsTrue()
    {
        var model = new GridModel(8, 8);

        // Kiểm tra các góc và điểm ở giữa (hợp lệ)
        Assert.IsTrue(model.IsWithinBounds(0, 0));
        Assert.IsTrue(model.IsWithinBounds(7, 7));
        Assert.IsTrue(model.IsWithinBounds(4, 3));
    }

    [Test]
    public void IsWithinBounds_InvalidCoordinates_ReturnsFalse()
    {
        var model = new GridModel(8, 8);

        // Kiểm tra các trường hợp văng ra ngoài bàn cờ
        Assert.IsFalse(model.IsWithinBounds(-1, 0)); // Âm
        Assert.IsFalse(model.IsWithinBounds(0, -1)); // Âm
        Assert.IsFalse(model.IsWithinBounds(8, 0));  // Lố kích thước (vì index tối đa là 7)
        Assert.IsFalse(model.IsWithinBounds(0, 8));  // Lố kích thước
        Assert.IsFalse(model.IsWithinBounds(8, 8));
    }

    [Test]
    public void SetOccupied_SetsAndGetsCorrectly()
    {
        var model = new GridModel(8, 8);

        // Đặt gạch vào ô (3, 4)
        model.SetOccupied(3, 4, true);

        // Kiểm tra lại xem ô đó có báo là có gạch không
        Assert.IsTrue(model.IsOccupied(3, 4));

        // Kiểm tra thử một ô khác xem có bị vạ lây không (phải là trống)
        Assert.IsFalse(model.IsOccupied(0, 0));
    }

    [Test]
    public void IsRowFull_WhenFull_ReturnsTrue()
    {
        var model = new GridModel(8, 8);
        // Đắp đầy hàng số 3
        for (int i = 0; i < 8; i++) model.SetOccupied(i, 3, true);
        
        Assert.IsTrue(model.IsRowFull(3));
        Assert.IsFalse(model.IsRowFull(4)); // Hàng khác phải là trống
    }

    [Test]
    public void IsColFull_WhenFull_ReturnsTrue()
    {
        var model = new GridModel(8, 8);
        // Đắp đầy cột số 2
        for (int i = 0; i < 8; i++) model.SetOccupied(2, i, true);
        
        Assert.IsTrue(model.IsColFull(2));
        Assert.IsFalse(model.IsColFull(3));
    }

    [Test]
    public void ClearRow_ClearsAllCellsInRow()
    {
        var model = new GridModel(8, 8);
        for (int i = 0; i < 8; i++) model.SetOccupied(i, 3, true);
        
        model.ClearRow(3); // Kích hoạt lệnh xóa
        
        Assert.IsFalse(model.IsRowFull(3));
        Assert.IsFalse(model.IsOccupied(0, 3)); // Check ngẫu nhiên 1 ô trong hàng
    }

    [Test]
    public void ClearCol_ClearsAllCellsInCol()
    {
        var model = new GridModel(8, 8);
        for (int i = 0; i < 8; i++) model.SetOccupied(2, i, true);
        
        model.ClearCol(2); // Kích hoạt lệnh xóa
        
        Assert.IsFalse(model.IsColFull(2));
        Assert.IsFalse(model.IsOccupied(2, 7)); // Check ngẫu nhiên ô cuối cùng của cột
    }
}
