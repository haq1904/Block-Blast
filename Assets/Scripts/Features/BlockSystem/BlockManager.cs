using System.Collections.Generic;
using UnityEngine;

public class BlockManager : MonoBehaviour, IBlockService
{
    private void Awake()
    {
        ServiceLocator.Register<IBlockService>(this);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<IBlockService>();
    }

    public BlockModel GetRandomShape()
    {
        // HARDCODE: Tạm thời fix cứng một khối hình chữ L (3 mảnh) để dễ test
        // Tọa độ: Gốc (0,0), Lên trên (0,1), Sang phải (1,0)
        List<(int x, int y)> lShape = new List<(int x, int y)>
        {
            (0, 0),
            (0, 1),
            (1, 0)
        };
        
        return new BlockModel(lShape);
    }
}
