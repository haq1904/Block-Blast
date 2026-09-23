using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class GridControllerTest
{
    private GameObject gridObject;
    private GridController gridController;

    // [SetUp] runs before each test scenario
    [SetUp]
    public void Setup()
    {
        // Create an invisible in-memory GameObject
        gridObject = new GameObject("TestGridController");
        
        // Attach the GridController component
        gridController = gridObject.AddComponent<GridController>();

        // IMPORTANT NOTE: In Unity EditMode test environment, MonoBehaviour Awake()
        // is sometimes not called automatically. We invoke Awake manually via reflection.
        typeof(GridController).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(gridController, null);
    }

    // [TearDown] runs after each test scenario to clean up
    [TearDown]
    public void Teardown()
    {
        Object.DestroyImmediate(gridObject);
    }

    [Test]
    public void GetGridPositionFromWorld_ReturnsCorrectCoordinates()
    {
        // --- SCENARIO 1: EXACT MATCH AT ORIGIN ANCHOR ---
        // Query coordinate at the anchor point (StartX = -3.5f, StartZ = 1.5f)
        Vector2Int result1 = gridController.GetGridPositionFromWorld(new Vector3(-3.5f, 0f, 1.5f));
        
        // Assert: Must return Col 0, Row 0
        Assert.AreEqual(new Vector2Int(0, 0), result1);

        // --- SCENARIO 2: OFFSET TO ANOTHER CELL ---
        // Shift right by 2 cells, shift up by 1 cell:
        // X = -3.5 + 2 = -1.5f
        // Z = 1.5 + 1 = 2.5f
        Vector2Int result2 = gridController.GetGridPositionFromWorld(new Vector3(-1.5f, 0f, 2.5f));
        
        // Assert: Must return Col 2, Row 1
        Assert.AreEqual(new Vector2Int(2, 1), result2);

        // --- SCENARIO 3: OFFSET SNAPPING (ROUNDING TEST) ---
        // Drop at X = -3.2 (offset 0.3 from -3.5) -> Rounds to 0
        // Drop at Z = 1.8 (offset 0.3 from 1.5) -> Rounds to 0
        Vector2Int result3 = gridController.GetGridPositionFromWorld(new Vector3(-3.2f, 0f, 1.8f));
        
        // Assert: Magnetic rounding snaps to cell [0, 0]
        Assert.AreEqual(new Vector2Int(0, 0), result3);
    }

    [Test]
    public void PlaceBlocks_FiresBlockPlacedEvent()
    {
        bool eventFired = false;
        
        // Subscribe to placement event
        gridController.OnBlockPlaced += (positions) => { eventFired = true; };
        
        // Place 1 block
        gridController.PlaceBlocks(new List<Vector2Int> { new Vector2Int(0, 0) });
        
        // Event must be broadcast
        Assert.IsTrue(eventFired);
    }

    [Test]
    public void PlaceBlocks_WhenLineFull_FiresLinesClearedEvent()
    {
        bool eventFired = false;
        int clearedRowCount = 0;
        
        // Subscribe to line clear event
        gridController.OnLinesCleared += (rows, cols, comboPositions) => {
            eventFired = true;
            clearedRowCount = rows.Count;
        };
        
        // Step 1: Fill first 7 cells of Row 0 (leaving 1 cell open)
        var blocks = new List<Vector2Int>();
        for (int i = 0; i < 7; i++) blocks.Add(new Vector2Int(i, 0));
        gridController.PlaceBlocks(blocks);
        
        Assert.IsFalse(eventFired); // Not full yet, no clear event

        // Step 2: Place final block at [7, 0] to complete the row
        gridController.PlaceBlocks(new List<Vector2Int> { new Vector2Int(7, 0) });
        
        // Assert: Line completed, clear event fired for 1 row
        Assert.IsTrue(eventFired);
        Assert.AreEqual(1, clearedRowCount);
    }

    [Test]
    public void PlaceBlocks_WhenMultipleLinesFull_ProvidesComboVFXPositions()
    {
        List<Vector3> receivedComboPositions = null;
        gridController.OnLinesCleared += (rows, cols, comboPositions) => {
            receivedComboPositions = comboPositions;
        };

        // Fill Row 0 (cols 0..7) and Col 0 (rows 0..7) simultaneously
        var blocks = new List<Vector2Int>();
        for (int i = 1; i < 8; i++)
        {
            blocks.Add(new Vector2Int(i, 0));
            blocks.Add(new Vector2Int(0, i));
        }
        gridController.PlaceBlocks(blocks);

        // Place intersection cell [0, 0] to trigger simultaneous 2-line clear
        gridController.PlaceBlocks(new List<Vector2Int> { new Vector2Int(0, 0) });

        Assert.IsNotNull(receivedComboPositions);
        Assert.AreEqual(1, receivedComboPositions.Count);
        Assert.AreEqual(gridController.GetWorldPositionFromGrid(new Vector2Int(0, 0)), receivedComboPositions[0]);
    }

    [Test]
    public void CanPlaceBlocks_WhenCellIsClearing_ReturnsFalse()
    {
        // Fill Row 0 to trigger line clear
        var blocks = new List<Vector2Int>();
        for (int i = 0; i < 8; i++) blocks.Add(new Vector2Int(i, 0));
        gridController.PlaceBlocks(blocks);

        // Cells in row 0 are now clearing (waiting for tween animation to complete)
        Assert.IsTrue(gridController.IsCellClearing(0, 0));

        // Attempting to place on a clearing cell must return false
        bool canPlace = gridController.CanPlaceBlocks(new List<Vector2Int> { new Vector2Int(0, 0) });
        Assert.IsFalse(canPlace);
    }

    [Test]
    public void ReleaseClearingCell_UnblocksPlacementOnCell()
    {
        // Fill Row 0 to trigger line clear
        var blocks = new List<Vector2Int>();
        for (int i = 0; i < 8; i++) blocks.Add(new Vector2Int(i, 0));
        gridController.PlaceBlocks(blocks);

        // Before release: locked
        Assert.IsTrue(gridController.IsCellClearing(0, 0));
        Assert.IsFalse(gridController.CanPlaceBlocks(new List<Vector2Int> { new Vector2Int(0, 0) }));

        // Release cell (simulating tween onExplode finishing)
        gridController.ReleaseClearingCell(new Vector2Int(0, 0));

        // After release: unlocked and valid for placement
        Assert.IsFalse(gridController.IsCellClearing(0, 0));
        Assert.IsTrue(gridController.CanPlaceBlocks(new List<Vector2Int> { new Vector2Int(0, 0) }));
    }
}
