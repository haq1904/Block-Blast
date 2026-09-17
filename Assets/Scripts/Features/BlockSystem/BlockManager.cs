using System;
using UnityEngine;

public class BlockManager : MonoBehaviour, IBlockService
{
    [SerializeField] private BlockController blockPrefab;

    [Header("Block Theme Configuration")]
    [SerializeField] private BlockTypeDatabaseSO blockTypeDatabase;
    [SerializeField] private string defaultBlockTypeId = "wood_crate";

    public BlockTypeSO CurrentBlockType { get; private set; }
    public BlockTypeDatabaseSO Database => blockTypeDatabase;
    public event Action<BlockTypeSO> OnBlockTypeChanged;

    private IPoolService poolService;
    private readonly BlockController[] activeBlocks = new BlockController[3];

    public bool HasActiveBlocks =>
        activeBlocks[0] != null || activeBlocks[1] != null || activeBlocks[2] != null;

    private void Awake()
    {
        InitializeTheme();
        ServiceLocator.Register<IBlockService>(this);
    }

    private void InitializeTheme()
    {
        if (CurrentBlockType == null && blockTypeDatabase != null)
        {
            CurrentBlockType = blockTypeDatabase.GetBlockType(defaultBlockTypeId);
        }
    }

    public void SetBlockType(string typeId)
    {
        if (blockTypeDatabase != null)
        {
            BlockTypeSO type = blockTypeDatabase.GetBlockType(typeId);
            if (type != null)
            {
                SetBlockType(type);
            }
        }
    }

    public void SetBlockType(BlockTypeSO newType)
    {
        if (newType == null || newType == CurrentBlockType) return;
        CurrentBlockType = newType;
        OnBlockTypeChanged?.Invoke(CurrentBlockType);
    }

    public GameObject GetCellPrefab(int variantId)
    {
        if (CurrentBlockType != null)
        {
            return CurrentBlockType.GetPrefab(variantId);
        }
        return null;
    }

    public int GetRandomVariantId()
    {
        if (CurrentBlockType != null && CurrentBlockType.variants != null && CurrentBlockType.variants.Length > 0)
        {
            int idx = UnityEngine.Random.Range(0, CurrentBlockType.variants.Length);
            return CurrentBlockType.variants[idx].variantId;
        }
        return 0;
    }

    private void Start()
    {
        poolService = ServiceLocator.Get<IPoolService>();
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<IBlockService>();
    }

    public void SpawnBatch(BlockModel[] batch, Vector3[] trayPositions)
    {
        if (batch == null || trayPositions == null)
        {
            Debug.LogWarning("[BlockManager] Invalid batch or trayPositions passed to SpawnBatch.");
            return;
        }

        if (poolService == null)
        {
            poolService = ServiceLocator.Get<IPoolService>();
        }

        if (blockPrefab == null)
        {
            Debug.LogError("[BlockManager] BlockPrefab is not assigned in the Inspector!");
            return;
        }

        // Clean up any remaining active blocks before spawning a new batch
        DespawnAll();

        for (int i = 0; i < 3; i++)
        {
            if (i < batch.Length && batch[i] != null && i < trayPositions.Length)
            {
                BlockController block = poolService != null
                    ? poolService.SpawnObject(blockPrefab, trayPositions[i], Quaternion.identity)
                    : Instantiate(blockPrefab, trayPositions[i], Quaternion.identity);

                if (block != null)
                {
                    block.Setup(batch[i], trayPositions[i], i);
                    activeBlocks[i] = block;
                }
                else
                {
                    activeBlocks[i] = null;
                }
            }
            else
            {
                activeBlocks[i] = null;
            }
        }
    }

    public void DespawnAll()
    {
        if (poolService == null)
        {
            poolService = ServiceLocator.Get<IPoolService>();
        }

        for (int i = 0; i < 3; i++)
        {
            DespawnBlock(i);
        }
    }

    public void DespawnBlock(int slotIndex)
    {
        if ((uint)slotIndex >= 3) return;

        BlockController block = activeBlocks[slotIndex];
        activeBlocks[slotIndex] = null;

        if (block != null)
        {
            if (block.gameObject != null && block.gameObject.activeInHierarchy)
            {
                if (poolService != null)
                {
                    try
                    {
                        poolService.ReturnObjectToPool(block.gameObject);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[BlockManager] Failed to return block to pool: {ex.Message}. Falling back to Destroy.");
                        Destroy(block.gameObject);
                    }
                }
                else
                {
                    Destroy(block.gameObject);
                }
            }
        }
    }

    public BlockController GetBlock(int slotIndex)
    {
        if ((uint)slotIndex >= 3) return null;
        return activeBlocks[slotIndex];
    }
}
