using UnityEngine;

public class SpawnView : MonoBehaviour
{
    private ISpawnService spawnService;

    private void Start()
    {
        spawnService = ServiceLocator.Get<ISpawnService>();

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
        // Passive View callback for visual feedback (e.g., tray animations, VFX) if needed
    }

    private void HandleNoMovesLeft()
    {
        Debug.Log("[SpawnView] Game over: No valid moves left.");
    }
}
