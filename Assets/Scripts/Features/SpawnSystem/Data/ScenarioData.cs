using System;
using System.Collections.Generic;
using UnityEngine;

public enum ScenarioType
{
    CrossClear,       // Clear 1 row + 1 column simultaneously
    MultiLineClear,   // Clear 2-4 lines simultaneously
    ComboBaiting,     // Maintain continuous combo streak
    AllClear,         // Wipe the entire 8x8 board clean
    ClutchRecovery    // Save the player from near-death board state
}

[Serializable]
public class ScenarioBatch
{
    [Tooltip("Description or intended purpose of this batch")]
    public string note = "Batch Step";

    [Tooltip("First shape in the tray (Slot 0)")]
    public ShapeData slot0;

    [Tooltip("Second shape in the tray (Slot 1)")]
    public ShapeData slot1;

    [Tooltip("Third shape in the tray (Slot 2)")]
    public ShapeData slot2;

    public bool HasAnyShape => slot0 != null || slot1 != null || slot2 != null;

    public int GetTotalTiles()
    {
        int count = 0;
        if (slot0 != null && slot0.baseOffsets != null) count += slot0.baseOffsets.Count;
        if (slot1 != null && slot1.baseOffsets != null) count += slot1.baseOffsets.Count;
        if (slot2 != null && slot2.baseOffsets != null) count += slot2.baseOffsets.Count;
        return count;
    }

    public List<ShapeData> GetShapes()
    {
        List<ShapeData> list = new List<ShapeData>(3);
        if (slot0 != null) list.Add(slot0);
        if (slot1 != null) list.Add(slot1);
        if (slot2 != null) list.Add(slot2);
        return list;
    }
}

[CreateAssetMenu(fileName = "New Scenario", menuName = "Block Blast/Scenario Data")]
public class ScenarioData : ScriptableObject
{
    [Header("1. Basic Information")]
    [Tooltip("Readable name of the scenario")]
    public string scenarioName = "New Scenario";

    [Tooltip("Category of gameplay scenario")]
    public ScenarioType scenarioType = ScenarioType.ComboBaiting;

    [Header("2. Dynamic Transformation")]
    [Tooltip("Allow automatic 90, 180, 270 degree rotation when spawning in gameplay")]
    public bool allowRotation = true;

    [Tooltip("Allow automatic mirror/reflection along X or Y axis")]
    public bool allowMirror = true;

    [Header("3. Phase 1 - Setup Batch (Build the Board)")]
    [Tooltip("3 shapes given to the player in phase 1 to construct the target layout")]
    public ScenarioBatch setupBatch = new ScenarioBatch { note = "Setup Batch: Build target layout" };

    [Header("4. Target Board Layout (Expected 8x8 state after Setup)")]
    [Tooltip("64 booleans representing 8x8 grid. true = occupied cell after phase 1")]
    public bool[] targetBoard = new bool[64];

    [Tooltip("Allowed difference in cells (tolerance). Default: 1 cell")]
    [Range(0, 3)]
    public int matchTolerance = 1;

    [HideInInspector]
    public List<Vector2Int> requiredTargetCoords = new List<Vector2Int>();

    [Header("5. Phase 2 - Finisher Batch (Trigger Clear)")]
    [Tooltip("3 shapes given to the player in phase 2 to trigger the massive clear/combo")]
    public ScenarioBatch finisherBatch = new ScenarioBatch { note = "Finisher Batch: Trigger line clear" };

    private void OnValidate()
    {
        SyncTargetCoordinates();
    }

    public void SyncTargetCoordinates()
    {
        if (requiredTargetCoords == null)
        {
            requiredTargetCoords = new List<Vector2Int>();
        }
        else
        {
            requiredTargetCoords.Clear();
        }

        if (targetBoard == null || targetBoard.Length != 64)
        {
            targetBoard = new bool[64];
            return;
        }

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                int index = row * 8 + col;
                if (targetBoard[index])
                {
                    requiredTargetCoords.Add(new Vector2Int(col, row));
                }
            }
        }
    }

    public int GetSetupTotalTiles() => setupBatch != null ? setupBatch.GetTotalTiles() : 0;

    public int GetTargetOccupiedCount()
    {
        if (targetBoard == null) return 0;
        int count = 0;
        for (int i = 0; i < targetBoard.Length; i++)
        {
            if (targetBoard[i]) count++;
        }
        return count;
    }

    /// <summary>
    /// Rotates an 8x8 grid clockwise by angle (0, 90, 180, 270) and optionally mirrors along X.
    /// </summary>
    public static bool[] TransformGrid(bool[] source, int angle, bool mirrorX)
    {
        if (source == null || source.Length != 64) return new bool[64];
        bool[] result = new bool[64];

        int normalizedAngle = ((angle % 360) + 360) % 360;

        for (int row = 0; row < 8; row++)
        {
            for (int col = 0; col < 8; col++)
            {
                int x = col;
                int y = row;

                if (mirrorX)
                {
                    x = 7 - x;
                }

                // Clockwise rotation:
                // 90 deg:  (x, y) -> (y, 7 - x)
                // 180 deg: (x, y) -> (7 - x, 7 - y)
                // 270 deg: (x, y) -> (7 - y, x)
                int newX = x;
                int newY = y;
                switch (normalizedAngle)
                {
                    case 90:
                        newX = y;
                        newY = 7 - x;
                        break;
                    case 180:
                        newX = 7 - x;
                        newY = 7 - y;
                        break;
                    case 270:
                        newX = 7 - y;
                        newY = x;
                        break;
                }

                int srcIndex = row * 8 + col;
                int dstIndex = newY * 8 + newX;
                result[dstIndex] = source[srcIndex];
            }
        }

        return result;
    }
}
