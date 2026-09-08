using UnityEngine;

public class SpawnView : MonoBehaviour
{
    [SerializeField] private BlockController blockPrefab;
    private ISpawnService spawnService;
    private IPoolService poolService;

    private void Start()
    {
        spawnService = ServiceLocator.Get<ISpawnService>();
        poolService = ServiceLocator.Get<IPoolService>();

        if (spawnService != null)
        {
            spawnService.OnBatchSpawned += HandleBatchSpawned;
        }
    }

    private void OnDestroy()
    {
        if (spawnService != null)
        {
            spawnService.OnBatchSpawned -= HandleBatchSpawned;
        }
    }

    private void HandleBatchSpawned(BlockModel[] batch, Vector3[] trayPositions)
    {
        for (int i = 0; i < batch.Length; i++)
        {
            if (batch[i] != null)
            {
                BlockController block = poolService.SpawnObject(blockPrefab, trayPositions[i], Quaternion.identity);
                if (block != null)
                {
                    // Truyền thêm i (slotIndex) để BlockController biết nó nằm ở slot nào
                    block.Setup(batch[i], trayPositions[i], i);
                }
            }
        }
    }
}
