using System;
using System.Collections.Generic;
using UnityEngine;

public class SpawnController : MonoBehaviour, ISpawnService
{
    private SpawnModel model;
    
    public event Action<BlockModel[], Vector3[]> OnBatchSpawned;

    private void Awake()
    {
        model = new SpawnModel();
        ServiceLocator.Register<ISpawnService>(this);
    }

    private void Start()
    {
        // Gọi thử nghiệm để tạo ra lứa gạch đầu tiên
        SpawnBatch(0);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<ISpawnService>();
    }

    public void SpawnBatch(int score)
    {
        ShapeDatabase db = ServiceLocator.Get<ShapeDatabase>();
        IGridService gridService = ServiceLocator.Get<IGridService>();

        BlockModel[] newBatch = new BlockModel[3];
        int tier3Count = 0;

        for (int i = 0; i < 3; i++)
        {
            int tier = DetermineTier(score);
            
            // Anti-frustration: max 1 Tier 3 block
            if (tier == 3 && tier3Count >= 1)
            {
                tier = UnityEngine.Random.Range(1, 3); // Fallback to 1 or 2
            }
            if (tier == 3) tier3Count++;

            ShapeData shape = GetRandomShapeFromTier(db, tier);
            newBatch[i] = CreateBlockModel(shape);
        }

        // Mercy Mode
        if (gridService != null && !CanPlaceAny(newBatch, gridService))
        {
            newBatch[2] = new BlockModel(new List<(int x, int y)> { (0, 0) });
        }

        // Lưu vào model
        model.SetBatch(newBatch);

        // Bắn sự kiện cho View
        OnBatchSpawned?.Invoke(model.CurrentBatch, model.TrayPositions);
    }

    public void MarkSlotEmpty(int slotIndex)
    {
        model.MarkSlotEmpty(slotIndex);

        // Kiểm tra nếu khay đã trống thì tự động sinh batch mới
        if (model.IsTrayEmpty())
        {
            // Tạm thời truyền điểm = 0. Sau này điểm sẽ lấy từ GameFlowController hoặc ScoreService
            SpawnBatch(0);
        }
    }

    private int DetermineTier(int score)
    {
        float t = Mathf.Clamp01(score / 1000f);
        float r = UnityEngine.Random.value;

        if (r < Mathf.Lerp(0.8f, 0.3f, t)) return 1;
        if (r < Mathf.Lerp(0.95f, 0.7f, t)) return 2;
        return 3;
    }

    private ShapeData GetRandomShapeFromTier(ShapeDatabase db, int tier)
    {
        List<ShapeData> list = tier == 1 ? db.tier1Shapes : (tier == 2 ? db.tier2Shapes : db.tier3Shapes);
        if (list == null || list.Count == 0)
        {
            list = db.tier1Shapes; 
            if (list == null || list.Count == 0) return null;
        }
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    private BlockModel CreateBlockModel(ShapeData data)
    {
        if (data == null) return new BlockModel(new List<(int, int)> { (0, 0) });

        List<Vector2Int> unityOffsets;
        if (data.canRotate)
        {
            int angle = UnityEngine.Random.Range(0, 4) * 90;
            unityOffsets = data.GetRotatedOffsets(angle);
        }
        else
        {
            unityOffsets = data.baseOffsets;
        }

        List<(int x, int y)> pureCSharpOffsets = new List<(int x, int y)>();
        foreach (var v in unityOffsets)
        {
            pureCSharpOffsets.Add((v.x, v.y));
        }

        return new BlockModel(pureCSharpOffsets);
    }

    private bool CanPlaceAny(BlockModel[] batch, IGridService grid)
    {
        foreach (var block in batch)
        {
            if (CanPlaceBlockAnywhere(block, grid))
            {
                return true;
            }
        }
        return false;
    }

    private bool CanPlaceBlockAnywhere(BlockModel block, IGridService grid)
    {
        for (int x = 0; x < grid.GridWidth; x++)
        {
            for (int y = 0; y < grid.GridHeight; y++)
            {
                List<Vector2Int> testPositions = new List<Vector2Int>();
                foreach (var offset in block.ShapeOffsets)
                {
                    testPositions.Add(new Vector2Int(x + offset.x, y + offset.y));
                }

                if (grid.CanPlaceBlocks(testPositions))
                {
                    return true;
                }
            }
        }
        return false;
    }
}
