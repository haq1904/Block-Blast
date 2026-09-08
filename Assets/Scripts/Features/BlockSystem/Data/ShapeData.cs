using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Shape", menuName = "Block Blast/Shape Data")]
public class ShapeData : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Difficulty tier of the shape (1: Easy, 2: Med, 3: Hard)")]
    public int tier = 1;
    
    [Tooltip("Allow auto-generating rotated variants?")]
    public bool canRotate = true;

    [Header("Shape Definition (5x5 Grid)")]
    // Data for 5x5 grid. True = solid, False = empty.
    // This data is mainly used by the Editor Script for drawing.
    public bool[] grid = new bool[25]; 
    
    [HideInInspector]
    public List<Vector2Int> baseOffsets = new List<Vector2Int>();
    
    // Automatically called when the Editor Script (or Unity) saves changes
    private void OnValidate()
    {
        baseOffsets.Clear();
        // 5x5 grid center is at column 2, row 2 (0-indexed)
        int center = 2;
        for (int y = 0; y < 5; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                int index = y * 5 + x;
                if (grid[index])
                {
                    // Convert from grid (0->4) to coordinate system with origin (0,0) at center
                    // Unity's Y axis is UP, so Y=0 (Top row) must have positive coordinates.
                    baseOffsets.Add(new Vector2Int(x - center, -(y - center))); 
                }
            }
        }
    }
    
    // Automatic matrix rotation function
    public List<Vector2Int> GetRotatedOffsets(int angle)
    {
        List<Vector2Int> rotated = new List<Vector2Int>();
        // 0: No rotation, 90: Rotate once, 180: Rotate twice, 270: Rotate 3 times
        int rotations = angle / 90;
        
        foreach (var offset in baseOffsets)
        {
            Vector2Int current = offset;
            for (int i = 0; i < rotations; i++)
            {
                // Rotate 90 degrees clockwise: (x, y) -> (y, -x)
                current = new Vector2Int(current.y, -current.x);
            }
            rotated.Add(current);
        }
        return rotated;
    }
}
