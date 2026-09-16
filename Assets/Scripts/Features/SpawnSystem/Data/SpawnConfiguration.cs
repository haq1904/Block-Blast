using UnityEngine;

[CreateAssetMenu(fileName = "SpawnConfiguration", menuName = "Block Blast/Spawn Configuration")]
public class SpawnConfiguration : ScriptableObject
{
    [Header("Databases")]
    [Tooltip("Master shape database containing all playable shapes in the game")]
    public ShapeDatabase shapeDatabase;

    [Header("Scenario Settings")]
    [Tooltip("Score threshold below which scenarios are active (default: 250,000)")]
    public int comboPhaseScoreThreshold = 250000;

    [Tooltip("Authored scenario database containing 2-phase setup and finisher sequences")]
    public ScenarioDatabase scenarioDatabase;

    [Tooltip("Enable scripted scenario chains below score threshold (Setup tray -> Finisher multi-blast)")]
    public bool enableScenarioChains = true;

    [Header("Mercy Mode")]
    [Tooltip("If none of the 3 batch shapes can fit on the board, replace one with a playable piece")]
    public bool enableMercyMode = true;

    [Header("Endgame Difficulty (Above Threshold)")]
    [Tooltip("Chance to introduce bulky pieces (3x3, Line 5) when score >= threshold and board occupancy is low")]
    [Range(0f, 1f)]
    public float highScoreBulkyPieceChance = 0.40f;

    private void OnEnable()
    {
        if (comboPhaseScoreThreshold == 0) comboPhaseScoreThreshold = 250000;
        if (highScoreBulkyPieceChance == 0f) highScoreBulkyPieceChance = 0.40f;
        enableScenarioChains = true;
        enableMercyMode = true;
    }
}
