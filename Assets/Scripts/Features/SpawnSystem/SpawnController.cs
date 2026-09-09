using System;
using UnityEngine;

public class SpawnController : MonoBehaviour, ISpawnService
{
    [SerializeField] private SpawnConfiguration config;

    private SpawnModel model;
    
    public event Action<BlockModel[], Vector3[]> OnBatchSpawned;
    public event Action OnNoMovesLeft;

    private void Awake()
    {
        model = new SpawnModel();
        ServiceLocator.Register<ISpawnService>(this);
    }

    private void Start()
    {
        // Initial spawn for the first batch
        SpawnBatch(0);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<ISpawnService>();
    }

    public void SpawnBatch(int score)
    {
        ShapeDatabase db = ServiceLocator.Get<ShapeDatabase>();
        IGridService gridService = ServiceLocator.Get<IGridService>();

        BlockModel[] newBatch = BlockSpawnGenerator.GenerateBatch(score, gridService, db, config);

        // Update model
        model.SetBatch(newBatch);

        // Notify View
        OnBatchSpawned?.Invoke(model.CurrentBatch, model.TrayPositions);

        // Check for immediate game over condition
        CheckGameOver();
    }

    public void MarkSlotEmpty(int slotIndex)
    {
        model.MarkSlotEmpty(slotIndex);

        // If tray is completely empty, spawn a new batch
        if (model.IsTrayEmpty())
        {
            // Score can be fetched from GameFlowController/ScoreService in future iterations
            SpawnBatch(0);
        }
        else
        {
            // Check if remaining tray blocks can still be placed
            CheckGameOver();
        }
    }

    private void CheckGameOver()
    {
        IGridService gridService = ServiceLocator.Get<IGridService>();
        if (gridService == null) return;

        bool canPlaceAny = false;
        for (int i = 0; i < 3; i++)
        {
            if (!model.IsSlotEmpty[i] && model.CurrentBatch[i] != null)
            {
                if (BlockSpawnGenerator.CanPlaceBlockAnywhere(model.CurrentBatch[i], gridService))
                {
                    canPlaceAny = true;
                    break;
                }
            }
        }

        if (!canPlaceAny)
        {
            OnNoMovesLeft?.Invoke();
            Debug.Log("[SpawnController] Game over: No valid moves left.");
        }
    }
}

