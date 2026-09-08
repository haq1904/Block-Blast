using System;
using UnityEngine;

public interface ISpawnService
{
    // Event: Bắn ra cho View biết đã spawn đợt mới.
    // Dữ liệu gồm mảng 3 BlockModel và mảng 3 tọa độ tương ứng
    event Action<BlockModel[], Vector3[]> OnBatchSpawned;

    void SpawnBatch(int score);
    void MarkSlotEmpty(int slotIndex);
}
