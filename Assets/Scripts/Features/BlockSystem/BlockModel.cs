using System.Collections.Generic;

public class BlockModel
{
    // Cấu hình mảng tọa độ tương đối của các ô gạch trong khối
    // Ví dụ khối 2x2: [(0,0), (1,0), (0,1), (1,1)]
    public List<(int x, int y)> ShapeOffsets { get; private set; }

    public BlockModel(List<(int x, int y)> shapeOffsets)
    {
        ShapeOffsets = shapeOffsets;
    }
}
