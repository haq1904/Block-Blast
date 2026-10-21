using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class OpeningBoardInitializationTest
{
    private GameObject gridObject;
    private GridController gridController;

    [SetUp]
    public void SetUp()
    {
        gridObject = new GameObject("TestGridController");
        gridController = gridObject.AddComponent<GridController>();
        typeof(GridController).GetMethod("Awake",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(gridController, null);
    }

    [TearDown]
    public void TearDown()
    {
        if (gridObject != null)
        {
            Object.DestroyImmediate(gridObject);
        }
    }

    [Test]
    public void TryInitializeBoard_WhenGridEmpty_PopulatesGridModelAndFiresEvent()
    {
        var cells = new List<CellPlacementData>
        {
            new CellPlacementData(new Vector2Int(0, 0), "wood", 1, 90),
            new CellPlacementData(new Vector2Int(1, 0), "wood", 2, 0),
            new CellPlacementData(new Vector2Int(2, 0), "wood", 3, 180),
        };

        bool eventFired = false;
        List<CellPlacementData> eventPayload = null;

        gridController.OnBoardInitialized += payload =>
        {
            eventFired = true;
            eventPayload = payload;
        };

        bool result = gridController.TryInitializeBoard(cells);

        Assert.IsTrue(result, "TryInitializeBoard should succeed on empty grid.");
        Assert.AreEqual(3, gridController.OccupiedCellCount);
        Assert.IsTrue(gridController.IsCellOccupied(0, 0));
        Assert.IsTrue(gridController.IsCellOccupied(1, 0));
        Assert.IsTrue(gridController.IsCellOccupied(2, 0));
        Assert.IsFalse(gridController.IsCellOccupied(3, 0));

        Assert.IsTrue(eventFired, "OnBoardInitialized event must be fired.");
        Assert.AreEqual(3, eventPayload.Count);
    }

    [Test]
    public void TryInitializeBoard_DoesNotFirePlacementOrClearOrScoreEvents()
    {
        // Setup 8 cells in a row (would normally trigger a line clear if PlaceBlocks was used)
        var fullRowCells = new List<CellPlacementData>();
        for (int c = 0; c < 8; c++)
        {
            fullRowCells.Add(new CellPlacementData(new Vector2Int(c, 0), "wood", 0));
        }

        bool blockPlacedFired = false;
        bool linesClearedFired = false;
        bool placementResolvedFired = false;

        gridController.OnBlockPlaced += _ => blockPlacedFired = true;
        gridController.OnLinesCleared += (_, _, _) => linesClearedFired = true;
        gridController.OnPlacementResolved += (_, _, _) => placementResolvedFired = true;

        bool result = gridController.TryInitializeBoard(fullRowCells);

        Assert.IsTrue(result);
        Assert.IsFalse(blockPlacedFired, "OnBlockPlaced must NOT be fired during board initialization.");
        Assert.IsFalse(linesClearedFired, "OnLinesCleared must NOT be fired during board initialization.");
        Assert.IsFalse(placementResolvedFired, "OnPlacementResolved must NOT be fired during board initialization.");
    }

    [Test]
    public void TryInitializeBoard_WhenGridAlreadyOccupied_RejectsAndReturnsFalse()
    {
        // First place one cell normally
        var initialCell = new List<CellPlacementData>
        {
            new CellPlacementData(new Vector2Int(0, 0), "wood", 0)
        };
        gridController.PlaceBlocks(initialCell);
        Assert.AreEqual(1, gridController.OccupiedCellCount);

        // Attempt initialization on non-empty grid
        var initCells = new List<CellPlacementData>
        {
            new CellPlacementData(new Vector2Int(3, 3), "wood", 0)
        };

        bool result = gridController.TryInitializeBoard(initCells);

        Assert.IsFalse(result, "TryInitializeBoard must reject non-empty grid.");
        Assert.AreEqual(1, gridController.OccupiedCellCount);
        Assert.IsFalse(gridController.IsCellOccupied(3, 3));
    }

    [Test]
    public void TryInitializeBoard_WhenCellsNullOrEmpty_RejectsAndReturnsFalse()
    {
        Assert.IsFalse(gridController.TryInitializeBoard(null));
        Assert.IsFalse(gridController.TryInitializeBoard(new List<CellPlacementData>()));
        Assert.AreEqual(0, gridController.OccupiedCellCount);
    }

    [Test]
    public void TryInitializeBoard_WhenOutOfBounds_RejectsAndReturnsFalse_Atomically()
    {
        var cells = new List<CellPlacementData>
        {
            new CellPlacementData(new Vector2Int(0, 0), "wood", 0),
            new CellPlacementData(new Vector2Int(8, 0), "wood", 0), // out of bounds col
        };

        bool result = gridController.TryInitializeBoard(cells);

        Assert.IsFalse(result, "Must reject out-of-bounds coordinates.");
        Assert.AreEqual(0, gridController.OccupiedCellCount, "Atomic failure: No cells should be occupied.");
        Assert.IsFalse(gridController.IsCellOccupied(0, 0));
    }

    [Test]
    public void TryInitializeBoard_WhenDuplicateCoordinates_RejectsAndReturnsFalse_Atomically()
    {
        var cells = new List<CellPlacementData>
        {
            new CellPlacementData(new Vector2Int(2, 2), "wood", 0),
            new CellPlacementData(new Vector2Int(2, 2), "wood", 1), // duplicate
        };

        bool result = gridController.TryInitializeBoard(cells);

        Assert.IsFalse(result, "Must reject duplicate coordinates.");
        Assert.AreEqual(0, gridController.OccupiedCellCount, "Atomic failure: No cells should be occupied.");
    }
}
