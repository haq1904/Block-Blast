using NUnit.Framework;

public class GridModelTest
{
    [Test]
    public void GridModel_Initialization_HasCorrectDimensions()
    {
        // Initialize an 8x8 grid
        var model = new GridModel(8, 8);

        Assert.AreEqual(8, model.Cols);
        Assert.AreEqual(8, model.Rows);
    }

    [Test]
    public void IsWithinBounds_ValidCoordinates_ReturnsTrue()
    {
        var model = new GridModel(8, 8);

        // Check corners and center points (valid)
        Assert.IsTrue(model.IsWithinBounds(0, 0));
        Assert.IsTrue(model.IsWithinBounds(7, 7));
        Assert.IsTrue(model.IsWithinBounds(4, 3));
    }

    [Test]
    public void IsWithinBounds_InvalidCoordinates_ReturnsFalse()
    {
        var model = new GridModel(8, 8);

        // Check out-of-bounds coordinates
        Assert.IsFalse(model.IsWithinBounds(-1, 0)); // Negative
        Assert.IsFalse(model.IsWithinBounds(0, -1)); // Negative
        Assert.IsFalse(model.IsWithinBounds(8, 0));  // Exceeds max dimension (max index is 7)
        Assert.IsFalse(model.IsWithinBounds(0, 8));  // Exceeds max dimension
        Assert.IsFalse(model.IsWithinBounds(8, 8));
    }

    [Test]
    public void SetOccupied_SetsAndGetsCorrectly()
    {
        var model = new GridModel(8, 8);

        // Place block at cell (3, 4)
        model.SetOccupied(3, 4, true);

        // Verify that the cell is occupied
        Assert.IsTrue(model.IsOccupied(3, 4));

        // Verify that another cell remains unoccupied
        Assert.IsFalse(model.IsOccupied(0, 0));
    }

    [Test]
    public void IsRowFull_WhenFull_ReturnsTrue()
    {
        var model = new GridModel(8, 8);
        // Fill row 3
        for (int i = 0; i < 8; i++) model.SetOccupied(i, 3, true);
        
        Assert.IsTrue(model.IsRowFull(3));
        Assert.IsFalse(model.IsRowFull(4)); // Other rows must remain empty
    }

    [Test]
    public void IsColFull_WhenFull_ReturnsTrue()
    {
        var model = new GridModel(8, 8);
        // Fill column 2
        for (int i = 0; i < 8; i++) model.SetOccupied(2, i, true);
        
        Assert.IsTrue(model.IsColFull(2));
        Assert.IsFalse(model.IsColFull(3));
    }

    [Test]
    public void ClearRow_ClearsAllCellsInRow()
    {
        var model = new GridModel(8, 8);
        for (int i = 0; i < 8; i++) model.SetOccupied(i, 3, true);
        
        model.ClearRow(3); // Trigger row clear
        
        Assert.IsFalse(model.IsRowFull(3));
        Assert.IsFalse(model.IsOccupied(0, 3)); // Verify arbitrary cell in row
    }

    [Test]
    public void ClearCol_ClearsAllCellsInCol()
    {
        var model = new GridModel(8, 8);
        for (int i = 0; i < 8; i++) model.SetOccupied(2, i, true);
        
        model.ClearCol(2); // Trigger column clear
        
        Assert.IsFalse(model.IsColFull(2));
        Assert.IsFalse(model.IsOccupied(2, 7)); // Verify last cell of column
    }
}
