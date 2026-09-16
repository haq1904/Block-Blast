using System.Collections.Generic;
using UnityEngine;

public class BlockModel
{
    // Cấu hình mảng tọa độ tương đối của các ô gạch trong khối
    // Ví dụ khối 2x2: [(0,0), (1,0), (0,1), (1,1)]
    public List<(int x, int y)> ShapeOffsets { get; private set; }
    public Vector2 CenterOffset { get; private set; }

    public BlockModel(List<(int x, int y)> shapeOffsets)
    {
        ShapeOffsets = shapeOffsets;
        CenterOffset = CalculateCenter(shapeOffsets);
    }

    private static Vector2 CalculateCenter(List<(int x, int y)> offsets)
    {
        if (offsets == null || offsets.Count == 0) return Vector2.zero;
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;
        for (int i = 0; i < offsets.Count; i++)
        {
            if (offsets[i].x < minX) minX = offsets[i].x;
            if (offsets[i].x > maxX) maxX = offsets[i].x;
            if (offsets[i].y < minY) minY = offsets[i].y;
            if (offsets[i].y > maxY) maxY = offsets[i].y;
        }
        return new Vector2((minX + maxX) / 2f, (minY + maxY) / 2f);
    }
}
