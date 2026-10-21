using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnController : MonoBehaviour, ISpawnService
{
    [SerializeField] private SpawnConfiguration config;

    private SpawnModel model;
    private IGridService cachedGridService;
    private bool pendingLegacyRestart;

    public SpawnModel Model => model;

    public event Action<BlockModel[], Vector3[]> OnBatchSpawned;
    public event Action OnNoMovesLeft;

    private void Awake()
    {
        model = new SpawnModel();
        ServiceLocator.Register<ISpawnService>(this);
    }

    private IEnumerator Start()
    {
        cachedGridService = ServiceLocator.Get<IGridService>();
        if (cachedGridService != null)
        {
            cachedGridService.OnPlacementResolved += HandlePlacementResolved;
        }

        int initialScore = ServiceLocator.Get<IScoreService>()?.CurrentScore ?? 0;
        OpeningGenerationMode mode = OpeningScenarioSelector.SelectMode(config, UnityEngine.Random.value);
        model.SetGenerationMode(mode);

        if (mode == OpeningGenerationMode.LegacyScenarioChain)
        {
            SpawnBatch(initialScore);
            yield break;
        }

        yield return null; // allow GridView and BlockManager Start subscriptions/setup

        if (!TryInitializeOpeningBoard())
        {
            model.SetGenerationMode(OpeningGenerationMode.LegacyScenarioChain);
        }

        SpawnBatch(initialScore);
    }

    private void OnDestroy()
    {
        if (cachedGridService != null)
        {
            cachedGridService.OnPlacementResolved -= HandlePlacementResolved;
        }
        ServiceLocator.Unregister<ISpawnService>();
    }

    private bool TryInitializeOpeningBoard()
    {
        if (config == null || config.openingScenarioDatabase == null) return false;
        if (!OpeningScenarioSelector.TrySelectScenario(config.openingScenarioDatabase, out var selection))
        {
            return false;
        }

        List<Vector2Int> cellCoords = selection.GetOccupiedCells();
        if (cellCoords == null || cellCoords.Count == 0) return false;

        var blockService = ServiceLocator.Get<IBlockService>();
        BlockTypeSO theme = blockService?.CurrentBlockType;

        var placementCells = new List<CellPlacementData>(cellCoords.Count);
        for (int i = 0; i < cellCoords.Count; i++)
        {
            int variant = blockService != null ? blockService.GetRandomVariantId() : 0;
            int rotationY = (theme != null && theme.randomCellRotationY) ? UnityEngine.Random.Range(0, 4) * 90 : 0;
            string typeId = theme != null ? theme.typeId : "";
            placementCells.Add(new CellPlacementData(cellCoords[i], typeId, variant, rotationY));
        }

        if (cachedGridService == null)
        {
            cachedGridService = ServiceLocator.Get<IGridService>();
            if (cachedGridService != null)
            {
                cachedGridService.OnPlacementResolved -= HandlePlacementResolved;
                cachedGridService.OnPlacementResolved += HandlePlacementResolved;
            }
        }

        if (cachedGridService == null) return false;
        return cachedGridService.TryInitializeBoard(placementCells);
    }

    private void HandlePlacementResolved(int tilesPlaced, int linesCleared, bool isAllClear)
    {
        if (isAllClear && model.GenerationMode == OpeningGenerationMode.IntelligentOnly)
        {
            pendingLegacyRestart = true;
        }
    }

    public void SpawnBatch(int score)
    {
        ShapeDatabase db = config != null ? config.shapeDatabase : null;
        IGridService gridService = cachedGridService ?? ServiceLocator.Get<IGridService>();

        BlockModel[] newBatch = model.GenerationMode == OpeningGenerationMode.IntelligentOnly
            ? BlockSpawnGenerator.GenerateIntelligentBatch(db, gridService, config, score)
            : BlockSpawnGenerator.GenerateBatch(score, gridService, db, config, model);

        var blockService = ServiceLocator.Get<IBlockService>();
        if (blockService != null && blockService.CurrentBlockType != null)
        {
            BlockSpawnGenerator.AssignThemeVariants(newBatch, blockService.CurrentBlockType);
        }

        // Update model
        model.SetBatch(newBatch);

        // Inter-system bridging: spawn blocks via IBlockService
        if (blockService != null)
        {
            blockService.SpawnBatch(model.CurrentBatch, model.TrayPositions);
        }

        // Notify View
        OnBatchSpawned?.Invoke(model.CurrentBatch, model.TrayPositions);

        // Check for immediate game over condition
        CheckGameOver();
    }

    public void MarkSlotEmpty(int slotIndex)
    {
        model.MarkSlotEmpty(slotIndex);

        if (pendingLegacyRestart)
        {
            pendingLegacyRestart = false;
            model.SetGenerationMode(OpeningGenerationMode.LegacyScenarioChain);
            model.ResetScenario();
            int currentScore = ServiceLocator.Get<IScoreService>()?.CurrentScore ?? 0;
            SpawnBatch(currentScore);
            return;
        }

        // If tray is completely empty, spawn a new batch with current score
        if (model.IsTrayEmpty())
        {
            int currentScore = ServiceLocator.Get<IScoreService>()?.CurrentScore ?? 0;
            SpawnBatch(currentScore);
        }
        else
        {
            // Check if remaining tray blocks can still be placed
            CheckGameOver();
        }
    }

    private void CheckGameOver()
    {
        IGridService gridService = cachedGridService ?? ServiceLocator.Get<IGridService>();
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
