using UnityEngine;

[CreateAssetMenu(fileName = "ScoreConfiguration", menuName = "Block Blast/Score Configuration")]
public class ScoreConfiguration : ScriptableObject
{
    [Header("Placement Points")]
    [Tooltip("Points awarded per placed tile/cell (default: 1)")]
    public int pointsPerTile = 1;

    [Header("Line Clear Base Points")]
    [Tooltip("Points for clearing 1 line")]
    public int points1Line = 10;

    [Tooltip("Points for clearing 2 lines simultaneously")]
    public int points2Lines = 30;

    [Tooltip("Points for clearing 3 lines simultaneously")]
    public int points3Lines = 60;

    [Tooltip("Points for clearing 4 lines simultaneously")]
    public int points4Lines = 100;

    [Tooltip("Base points for clearing 5 lines simultaneously")]
    public int points5Lines = 150;

    [Tooltip("Additional points per line beyond 5")]
    public int pointsPerExtraLine = 30;

    [Header("Combo Streak Bonus")]
    [Tooltip("Bonus points added per combo streak level (comboCount * comboBonusStep)")]
    public int comboBonusStep = 10;

    [Tooltip("Number of placements allowed after prewarm (first clear) to ignite a combo (default: 3)")]
    public int prewarmWindowTurns = 3;

    [Tooltip("Number of consecutive non-clearing placements allowed before active combo ends (default: 3)")]
    public int comboGraceTurns = 3;

    [Header("All Clear Bonus")]
    [Tooltip("Extra bonus points awarded when the board is completely emptied")]
    public int allClearBonus = 300;

    [Header("Persistence")]
    [Tooltip("PlayerPrefs key used to persist high score")]
    public string highScoreSaveKey = "BLOCK_BLAST_HIGH_SCORE";

    public int CalculateLineClearScore(int linesCount)
    {
        if (linesCount <= 0) return 0;
        switch (linesCount)
        {
            case 1: return points1Line;
            case 2: return points2Lines;
            case 3: return points3Lines;
            case 4: return points4Lines;
            case 5: return points5Lines;
            default: return points5Lines + (linesCount - 5) * pointsPerExtraLine;
        }
    }

    public int CalculateComboBonus(int comboCount)
    {
        if (comboCount <= 0) return 0;
        return comboCount * comboBonusStep;
    }

    public int CalculateAllClearBonus(int comboCount)
    {
        int multiplier = Mathf.Max(1, comboCount);
        return allClearBonus * multiplier;
    }
}
