using UnityEngine;

public interface IPoolService
{
    T SpawnObject<T>(T typePrefab, Vector3 spawnPos, Quaternion spawnRot, PoolType poolType = PoolType.GameObject) where T : Component;
    GameObject SpawnObject(GameObject typePrefab, Vector3 spawnPos, Quaternion spawnRot, PoolType poolType = PoolType.GameObject);
    void ReturnObjectToPool(GameObject obj, PoolType poolType = PoolType.GameObject);
}
