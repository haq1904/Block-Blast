using UnityEngine;

[CreateAssetMenu(fileName = "SpawnConfiguration", menuName = "Block Blast/Spawn Configuration")]
public class SpawnConfiguration : ScriptableObject
{
    [Header("Assistance Settings")]
    [Tooltip("Score threshold below which the game actively offers line-clearing combo shapes (default: 200,000)")]
    public int comboPhaseScoreThreshold = 200000;

    [Tooltip("Probability of offering a guaranteed line-clearing key shape when below the threshold")]
    [Range(0f, 1f)]
    public float comboAssistanceRate = 0.85f;

    [Tooltip("Enable complementary multi-piece synergy batches")]
    public bool enableSynergisticBatches = true;

    [Tooltip("Chance to generate synergistic batches when below the score threshold")]
    [Range(0f, 1f)]
    public float synergyRateUnderThreshold = 0.80f;

    [Tooltip("Enable scripted scenario chains before 200,000 pts (Setup tray -> Finisher multi-blast)")]
    public bool enableScenarioChains = true;

    [Header("Batch Constraints")]
    [Tooltip("Prevent identical shapes from appearing in the same 3-block batch")]
    public bool preventDuplicatesInBatch = true;

    [Tooltip("Chance to allow 1x1 or 1x2 blocks during regular turns (0 = only appear during Mercy Mode)")]
    [Range(0f, 0.2f)]
    public float ultraSmallShapeChance = 0.02f;

    [Header("Mercy Mode")]
    [Tooltip("If none of the 3 batch shapes can fit on the board, replace one with a playable piece")]
    public bool enableMercyMode = true;

    [Header("Endgame Difficulty (Above Threshold)")]
    [Tooltip("Probability of triggering automatic board shutdown when score >= threshold")]
    [Range(0f, 1f)]
    public float highScoreAssistanceRate = 0.35f;

    [Tooltip("Chance to introduce bulky pieces (3x3, Line 5) when score >= threshold and board occupancy is low")]
    [Range(0f, 1f)]
    public float highScoreBulkyPieceChance = 0.40f;

    private void OnEnable()
    {
        if (comboPhaseScoreThreshold == 0) comboPhaseScoreThreshold = 200000;
        if (comboAssistanceRate == 0f) comboAssistanceRate = 0.85f;
        if (synergyRateUnderThreshold == 0f) synergyRateUnderThreshold = 0.80f;
        if (highScoreAssistanceRate == 0f) highScoreAssistanceRate = 0.35f;
        if (highScoreBulkyPieceChance == 0f) highScoreBulkyPieceChance = 0.40f;
        enableSynergisticBatches = true;
        enableScenarioChains = true;
        preventDuplicatesInBatch = true;
        enableMercyMode = true;
    }
}
