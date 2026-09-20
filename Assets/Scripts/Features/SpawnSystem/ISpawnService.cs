using System;
using UnityEngine;

public interface ISpawnService
{
    // Event: Broadcasts when a new batch is spawned with 3 BlockModels and their corresponding tray positions
    event Action<BlockModel[], Vector3[]> OnBatchSpawned;
    
    // Event: Fired when no remaining tray blocks can be placed on the grid (Game Over)
    event Action OnNoMovesLeft;

    void SpawnBatch(int score);
    void MarkSlotEmpty(int slotIndex);
}
