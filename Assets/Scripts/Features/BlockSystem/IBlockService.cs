using System;
using UnityEngine;

public interface IBlockService
{
    void SpawnBatch(BlockModel[] batch, Vector3[] trayPositions);
    void DespawnAll();
    void DespawnBlock(int slotIndex);
    BlockController GetBlock(int slotIndex);
    bool HasActiveBlocks { get; }

    BlockTypeSO CurrentBlockType { get; }
    BlockTypeDatabaseSO Database { get; }
    event Action<BlockTypeSO> OnBlockTypeChanged;
    void SetBlockType(string typeId);
    void SetBlockType(BlockTypeSO newType);
    GameObject GetCellPrefab(int variantId);
    int GetRandomVariantId();
}
