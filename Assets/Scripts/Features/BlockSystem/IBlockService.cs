using UnityEngine;

public interface IBlockService
{
    void SpawnBatch(BlockModel[] batch, Vector3[] trayPositions);
    void DespawnAll();
    void DespawnBlock(int slotIndex);
    BlockController GetBlock(int slotIndex);
    bool HasActiveBlocks { get; }
}
