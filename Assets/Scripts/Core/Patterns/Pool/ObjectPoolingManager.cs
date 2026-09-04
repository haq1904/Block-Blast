using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public enum PoolType
{
    ParticleSystem,
    GameObject,
    SoundFX
}

public class ObjectPoolingManager : MonoBehaviour
{

    private GameObject _emptyHolder;
    private GameObject _particleSystemEmpty;
    private GameObject _gameObjectEmpty;
    private GameObject _soundFXEmpty;

    private Dictionary<GameObject, ObjectPool<GameObject>> _objectPools;
    private Dictionary<GameObject, GameObject> _cloneToPrefabMap;

    protected void Awake()
    {
        _objectPools = new Dictionary<GameObject, ObjectPool<GameObject>>();
        _cloneToPrefabMap = new Dictionary<GameObject, GameObject>();
        SetupEmpties();
    }

    private void SetupEmpties()
    {
        _emptyHolder = new GameObject("Object Pools");
        _emptyHolder.transform.SetParent(transform);

        _particleSystemEmpty = new GameObject("Particle Systems");
        _particleSystemEmpty.transform.SetParent(_emptyHolder.transform);

        _gameObjectEmpty = new GameObject("Game objects");
        _gameObjectEmpty.transform.SetParent(_emptyHolder.transform);

        _soundFXEmpty = new GameObject("Sound Effects");
        _soundFXEmpty.transform.SetParent(_emptyHolder.transform);
    }

    private void CreatePool(GameObject prefab, Vector3 pos, Quaternion rot, PoolType poolType = PoolType.GameObject)
    {
        ObjectPool<GameObject> pool = new ObjectPool<GameObject>(
            createFunc: () => CreateObject(prefab, pos, rot, poolType),
            actionOnGet: OnGetObject,
            actionOnRelease: OnReleaseObject,
            actionOnDestroy: OnDestroyObject
            );

        _objectPools.Add(prefab, pool);
    }

    private GameObject CreateObject(GameObject prefab, Vector3 pos, Quaternion rot, PoolType poolType = PoolType.GameObject)
    {
        prefab.SetActive(false);

        GameObject obj = Instantiate(prefab, pos, rot);

        prefab.SetActive(true);

        GameObject parentObject = SetParentObject(poolType);
        obj.transform.SetParent(parentObject.transform);

        return obj;
    }

    private void OnGetObject(GameObject obj)
    {
        //optional get
    }

    private void OnReleaseObject(GameObject obj)
    {
        obj.SetActive(false);
    }

    private void OnDestroyObject(GameObject obj)
    {
        if (_cloneToPrefabMap.ContainsKey(obj))
            _cloneToPrefabMap.Remove(obj);
    }

    private GameObject SetParentObject(PoolType poolType)
    {
        switch (poolType)
        {
            case PoolType.ParticleSystem:
                return _particleSystemEmpty;
            case PoolType.GameObject:
                return _gameObjectEmpty;
            case PoolType.SoundFX:
                return _soundFXEmpty;
            default:
                return null;
        }
    }

    private T SpawnObject<T>(GameObject objToSpawn, Vector3 spawnPos, Quaternion spawnRot, PoolType poolType = PoolType.GameObject) where T : UnityEngine.Object
    {
        if (!_objectPools.ContainsKey(objToSpawn))
            CreatePool(objToSpawn, spawnPos, spawnRot, poolType);

        GameObject obj = _objectPools[objToSpawn].Get();

        if (obj != null)
        {
            if (!_cloneToPrefabMap.ContainsKey(obj))
                _cloneToPrefabMap.Add(obj, objToSpawn);

            obj.transform.position = spawnPos;
            obj.transform.rotation = spawnRot;
            obj.SetActive(true);

            if (typeof(T) == typeof(GameObject))
                return obj as T;

            T component = obj.GetComponent<T>();
            if (component == null)
            {
                Debug.LogError($"The object {objToSpawn.name} doesn't have component type of {typeof(T)} ");
                return null;
            }
            return component;
        }
        return null;
    }

    public T SpawnObject<T>(T typePrefab, Vector3 spawnPos, Quaternion spawnRot, PoolType poolType = PoolType.GameObject) where T : Component
    {
        return SpawnObject<T>(typePrefab.gameObject, spawnPos, spawnRot, poolType);
    }

    public GameObject SpawnObject(GameObject typePrefab, Vector3 spawnPos, Quaternion spawnRot, PoolType poolType = PoolType.GameObject)
    {
        return SpawnObject<GameObject>(typePrefab, spawnPos, spawnRot, poolType);
    }

    public void ReturnObjectToPool(GameObject obj, PoolType poolType = PoolType.GameObject)
    {
        if (_cloneToPrefabMap.TryGetValue(obj, out GameObject prefab))
        {
            GameObject parentObject = SetParentObject(poolType);
            if (obj.transform.parent != parentObject)
            {
                obj.transform.SetParent(parentObject.transform);
            }
            if (_objectPools.TryGetValue(prefab, out ObjectPool<GameObject> pool))
            {
                pool.Release(obj);
            }
        }
        else
        {
            Debug.LogWarning($"Trying to return an object that is not pooled : {obj.name}");
        }
    }
}
