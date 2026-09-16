using UnityEngine;

public class SpawnView : MonoBehaviour
{
    private ISpawnService spawnService;
    private IBlockService blockService;

    private void Start()
    {
        spawnService = ServiceLocator.Get<ISpawnService>();
        blockService = ServiceLocator.Get<IBlockService>();

        if (spawnService != null)
        {
            spawnService.OnBatchSpawned += HandleBatchSpawned;
            spawnService.OnNoMovesLeft += HandleNoMovesLeft;
        }
    }

    private void OnDestroy()
    {
        if (spawnService != null)
        {
            spawnService.OnBatchSpawned -= HandleBatchSpawned;
            spawnService.OnNoMovesLeft -= HandleNoMovesLeft;
        }
    }

    private void HandleBatchSpawned(BlockModel[] batch, Vector3[] trayPositions)
    {
        if (blockService == null)
        {
            blockService = ServiceLocator.Get<IBlockService>();
        }

        if (blockService != null)
        {
            blockService.SpawnBatch(batch, trayPositions);
        }
        else
        {
            Debug.LogError("[SpawnView] IBlockService is not registered in ServiceLocator!");
        }
    }

    private void HandleNoMovesLeft()
    {
        Debug.Log("[SpawnView] Game over: No valid moves left.");
    }
}
