using System;
using UnityEngine;

public class ScoreController : MonoBehaviour, IScoreService
{
    [SerializeField] private ScoreConfiguration config;

    private ScoreModel model;
    private IGridService gridService;

    public int CurrentScore => model != null ? model.CurrentScore : 0;
    public int HighScore => model != null ? model.HighScore : 0;
    public int CurrentCombo => model != null ? model.CurrentCombo : 0;
    public int MaxCombo => model != null ? model.MaxCombo : 0;
    public bool IsPrewarmed => model != null && model.IsPrewarmed;
    public int TurnsRemaining => model != null ? model.TurnsRemaining : 0;

    public event Action<int, int> OnScoreChanged;
    public event Action<int> OnComboChanged;
    public event Action<int> OnHighScoreChanged;

    private void Awake()
    {
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<ScoreConfiguration>();
        }

        int savedHighScore = PlayerPrefs.GetInt(config.highScoreSaveKey, 0);
        model = new ScoreModel(savedHighScore);

        ServiceLocator.Register<IScoreService>(this);
    }

    private void Start()
    {
        gridService = ServiceLocator.Get<IGridService>();
        if (gridService != null)
        {
            gridService.OnPlacementResolved += HandlePlacementResolved;
        }
    }

    private void OnDestroy()
    {
        if (gridService != null)
        {
            gridService.OnPlacementResolved -= HandlePlacementResolved;
        }

        ServiceLocator.Unregister<IScoreService>();
    }

    public void Initialize(ScoreConfiguration customConfig, IGridService customGridService = null)
    {
        if (customConfig != null) config = customConfig;

        int savedHighScore = PlayerPrefs.GetInt(config.highScoreSaveKey, 0);
        model = new ScoreModel(savedHighScore);

        if (customGridService != null)
        {
            if (gridService != null) gridService.OnPlacementResolved -= HandlePlacementResolved;
            gridService = customGridService;
            gridService.OnPlacementResolved += HandlePlacementResolved;
        }
    }

    public void HandlePlacementResolved(int tilesPlaced, int totalLinesCleared, bool isAllClear)
    {
        if (model == null) return;

        int gainedPoints = 0;

        // 1. Placement Points
        int placementScore = tilesPlaced * config.pointsPerTile;
        gainedPoints += placementScore;

        int previousCombo = model.CurrentCombo;
        int prewarmLimit = config != null ? config.prewarmWindowTurns : 3;
        int comboLimit = config != null ? config.comboGraceTurns : 3;

        // 2. Process placement in model (handles Prewarm, combo ignition, and combo streak)
        model.RecordPlacement(totalLinesCleared, prewarmLimit, comboLimit);

        // 3. Line Clears & Combo Bonus Points
        if (totalLinesCleared > 0)
        {
            int lineClearScore = config.CalculateLineClearScore(totalLinesCleared);
            gainedPoints += lineClearScore;

            // Only award combo bonus points when active combo is > 0
            if (model.CurrentCombo > 0)
            {
                int comboBonus = config.CalculateComboBonus(model.CurrentCombo);
                gainedPoints += comboBonus;
            }

            if (isAllClear)
            {
                gainedPoints += config.CalculateAllClearBonus(model.CurrentCombo);
            }
        }

        // 4. Fire Combo Event if combo changed
        if (model.CurrentCombo != previousCombo)
        {
            OnComboChanged?.Invoke(model.CurrentCombo);
        }

        // 5. Update Total Score & Check High Score
        if (gainedPoints > 0)
        {
            int previousHighScore = model.HighScore;
            model.AddScore(gainedPoints);

            OnScoreChanged?.Invoke(model.CurrentScore, gainedPoints);

            if (model.HighScore > previousHighScore)
            {
                PlayerPrefs.SetInt(config.highScoreSaveKey, model.HighScore);
                PlayerPrefs.Save();
                OnHighScoreChanged?.Invoke(model.HighScore);
            }
        }
    }

    public void ResetScore()
    {
        if (model == null) return;
        model.Reset();
        OnScoreChanged?.Invoke(0, 0);
        OnComboChanged?.Invoke(0);
    }
}
