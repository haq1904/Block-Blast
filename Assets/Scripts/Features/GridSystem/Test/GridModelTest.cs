using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

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

    [Test]
    public void GetExposedEdges_SingleCell_Returns4Edges()
    {
        var model = new GridModel(8, 8);
        var cells = new List<Vector2Int> { new Vector2Int(3, 3) };

        var edges = model.GetExposedEdges(cells);

        Assert.AreEqual(4, edges.Count);
        Assert.IsTrue(edges.Exists(e => e.gridPos == new Vector2Int(3, 3) && e.direction == GridEdgeDirection.North));
        Assert.IsTrue(edges.Exists(e => e.gridPos == new Vector2Int(3, 3) && e.direction == GridEdgeDirection.South));
        Assert.IsTrue(edges.Exists(e => e.gridPos == new Vector2Int(3, 3) && e.direction == GridEdgeDirection.East));
        Assert.IsTrue(edges.Exists(e => e.gridPos == new Vector2Int(3, 3) && e.direction == GridEdgeDirection.West));
    }

    [Test]
    public void GetExposedEdges_HorizontalLine_CancelsInternalEdges()
    {
        var model = new GridModel(8, 8);
        var cells = new List<Vector2Int>
        {
            new Vector2Int(2, 3),
            new Vector2Int(3, 3),
            new Vector2Int(4, 3)
        };

        var edges = model.GetExposedEdges(cells);

        // 3 cells * 4 = 12 edges total, minus 4 internal shared edges (2 pairs) = 8 outer edges
        Assert.AreEqual(8, edges.Count);

        // Cell (3,3) in the middle must NOT have East or West edges
        Assert.IsFalse(edges.Exists(e => e.gridPos == new Vector2Int(3, 3) && e.direction == GridEdgeDirection.East));
        Assert.IsFalse(edges.Exists(e => e.gridPos == new Vector2Int(3, 3) && e.direction == GridEdgeDirection.West));

        // Outer ends must be present
        Assert.IsTrue(edges.Exists(e => e.gridPos == new Vector2Int(2, 3) && e.direction == GridEdgeDirection.West));
        Assert.IsTrue(edges.Exists(e => e.gridPos == new Vector2Int(4, 3) && e.direction == GridEdgeDirection.East));
    }

    [Test]
    public void GetExposedEdges_Square2x2_CancelsInternalEdges()
    {
        var model = new GridModel(8, 8);
        var cells = new List<Vector2Int>
        {
            new Vector2Int(2, 2), new Vector2Int(3, 2),
            new Vector2Int(2, 3), new Vector2Int(3, 3)
        };

        var edges = model.GetExposedEdges(cells);

        // 4 cells * 4 = 16 edges total, minus 8 internal shared edges (4 pairs) = 8 perimeter edges
        Assert.AreEqual(8, edges.Count);

        // Bottom-left cell (2, 2) has exposed South and West, but North and East are internal
        Assert.IsTrue(edges.Exists(e => e.gridPos == new Vector2Int(2, 2) && e.direction == GridEdgeDirection.South));
        Assert.IsTrue(edges.Exists(e => e.gridPos == new Vector2Int(2, 2) && e.direction == GridEdgeDirection.West));
        Assert.IsFalse(edges.Exists(e => e.gridPos == new Vector2Int(2, 2) && e.direction == GridEdgeDirection.North));
        Assert.IsFalse(edges.Exists(e => e.gridPos == new Vector2Int(2, 2) && e.direction == GridEdgeDirection.East));
    }

    [Test]
    public void GetExposedEdges_AdjacentToOccupiedCell_DoesNotEmitOnBlockedEdge()
    {
        var model = new GridModel(8, 8);
        // Pre-existing block on the board at (4, 3)
        model.SetOccupied(4, 3, true);

        // Placing a new single block at (3, 3) right next to (4, 3)
        var cells = new List<Vector2Int> { new Vector2Int(3, 3) };

        var edges = model.GetExposedEdges(cells);

        // East direction is blocked by (4, 3), so only North, South, West are exposed
        Assert.AreEqual(3, edges.Count);
        Assert.IsFalse(edges.Exists(e => e.direction == GridEdgeDirection.East));
        Assert.IsTrue(edges.Exists(e => e.direction == GridEdgeDirection.North));
        Assert.IsTrue(edges.Exists(e => e.direction == GridEdgeDirection.South));
        Assert.IsTrue(edges.Exists(e => e.direction == GridEdgeDirection.West));
    }

    [Test]
    public void GetExposedEdges_AtGridBorder_BorderEdgeIsExposed()
    {
        var model = new GridModel(8, 8);
        // Corner cell at (0, 0)
        var cells = new List<Vector2Int> { new Vector2Int(0, 0) };

        var edges = model.GetExposedEdges(cells);

        // All 4 directions exposed (South and West are out of bounds, which count as open space)
        Assert.AreEqual(4, edges.Count);
        Assert.IsTrue(edges.Exists(e => e.direction == GridEdgeDirection.South));
        Assert.IsTrue(edges.Exists(e => e.direction == GridEdgeDirection.West));
    }
}
