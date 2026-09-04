using UnityEngine;

public interface IPoolService
{
    /// <summary>
    /// Spawn một Object có kiểu dữ liệu là Component (ví dụ: BlockController)
    /// </summary>
    T SpawnObject<T>(T typePrefab, Vector3 spawnPos, Quaternion spawnRot, PoolType poolType = PoolType.GameObject) where T : Component;

    /// <summary>
    /// Spawn một GameObject bình thường
    /// </summary>
    GameObject SpawnObject(GameObject typePrefab, Vector3 spawnPos, Quaternion spawnRot, PoolType poolType = PoolType.GameObject);

    /// <summary>
    /// Trả Object về lại Pool (Giải phóng)
    /// </summary>
    void ReturnObjectToPool(GameObject obj, PoolType poolType = PoolType.GameObject);
}
