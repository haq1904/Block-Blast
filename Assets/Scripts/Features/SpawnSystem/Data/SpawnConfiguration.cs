using UnityEngine;

[CreateAssetMenu(fileName = "SpawnConfiguration", menuName = "Block Blast/Spawn Configuration")]
public class SpawnConfiguration : ScriptableObject
{
    [Header("Difficulty Curves (Weight vs Score)")]
    [Tooltip("Weight curve for Tier 1 shapes by Score (0 to 1,000,000)")]
    public AnimationCurve tier1WeightCurve = new AnimationCurve(
        new Keyframe(0f, 0.15f),
        new Keyframe(200000f, 0.10f),
        new Keyframe(500000f, 0.08f),
        new Keyframe(1000000f, 0.05f));

    [Tooltip("Weight curve for Tier 2 shapes by Score (0 to 1,000,000)")]
    public AnimationCurve tier2WeightCurve = new AnimationCurve(
        new Keyframe(0f, 0.75f),
        new Keyframe(200000f, 0.65f),
        new Keyframe(500000f, 0.52f),
        new Keyframe(1000000f, 0.45f));

    [Tooltip("Weight curve for Tier 3 shapes by Score (0 to 1,000,000)")]
    public AnimationCurve tier3WeightCurve = new AnimationCurve(
        new Keyframe(0f, 0.10f),
        new Keyframe(200000f, 0.25f),
        new Keyframe(500000f, 0.40f),
        new Keyframe(1000000f, 0.50f));

    [Header("Combo Assistance (Honeymoon Phase)")]
    [Tooltip("Score threshold below which the game actively offers line-clearing combo shapes (default: 200,000)")]
    public int comboPhaseScoreThreshold = 200000;

    [Tooltip("Probability of offering a guaranteed line-clearing key shape when below the threshold")]
    [Range(0f, 1f)]
    public float comboAssistanceRate = 0.85f;

    [Header("Batch Constraints")]
    [Tooltip("Maximum allowed Tier 3 blocks in a single 3-block batch")]
    public int maxTier3Count = 1;

    [Tooltip("Prevent identical shapes from appearing in the same 3-block batch")]
    public bool preventDuplicatesInBatch = true;

    [Header("Ultra-Small Shapes Filter (1x1, 1x2)")]
    [Tooltip("Chance to allow 1x1 or 1x2 blocks during regular turns (0 = only appear during Mercy Mode)")]
    [Range(0f, 0.2f)]
    public float ultraSmallShapeChance = 0.02f;

    [Header("Dynamic Difficulty (Board Density)")]
    [Tooltip("Adjust tier probabilities based on grid occupancy percentage")]
    public bool enableBoardDensityAdjustment = true;

    [Tooltip("Occupancy ratio threshold (0.0 to 1.0) considered crowded, boosting easy pieces")]
    [Range(0.5f, 0.95f)]
    public float highDensityThreshold = 0.7f;

    [Tooltip("Occupancy ratio threshold (0.0 to 1.0) considered empty, allowing larger pieces")]
    [Range(0.05f, 0.45f)]
    public float lowDensityThreshold = 0.3f;

    [Header("Mercy Mode")]
    [Tooltip("If none of the 3 batch shapes can fit on the board, replace one with a playable piece")]
    public bool enableMercyMode = true;

    private void Reset()
    {
        tier1WeightCurve = new AnimationCurve(
            new Keyframe(0f, 0.15f),
            new Keyframe(200000f, 0.10f),
            new Keyframe(500000f, 0.08f),
            new Keyframe(1000000f, 0.05f));

        tier2WeightCurve = new AnimationCurve(
            new Keyframe(0f, 0.75f),
            new Keyframe(200000f, 0.65f),
            new Keyframe(500000f, 0.52f),
            new Keyframe(1000000f, 0.45f));

        tier3WeightCurve = new AnimationCurve(
            new Keyframe(0f, 0.10f),
            new Keyframe(200000f, 0.25f),
            new Keyframe(500000f, 0.40f),
            new Keyframe(1000000f, 0.50f));
    }



    public void GetTierWeights(int score, float occupancyRatio, out float w1, out float w2, out float w3)
    {
        w1 = Mathf.Max(0.01f, tier1WeightCurve != null && tier1WeightCurve.length > 0 ? tier1WeightCurve.Evaluate(score) : 0.35f);
        w2 = Mathf.Max(0.01f, tier2WeightCurve != null && tier2WeightCurve.length > 0 ? tier2WeightCurve.Evaluate(score) : 0.50f);
        w3 = Mathf.Max(0.01f, tier3WeightCurve != null && tier3WeightCurve.length > 0 ? tier3WeightCurve.Evaluate(score) : 0.15f);

        if (enableBoardDensityAdjustment)
        {
            if (occupancyRatio >= highDensityThreshold)
            {
                // Board is crowded -> boost Tier 1, suppress Tier 3
                w1 *= 2.0f;
                w3 *= 0.3f;
            }
            else if (occupancyRatio <= lowDensityThreshold)
            {
                // Board is mostly empty -> reduce tiny pieces, favor standard and large pieces
                w1 *= 0.5f;
                w2 *= 1.3f;
                w3 *= 1.5f;
            }
        }


        // Normalize weights
        float total = w1 + w2 + w3;
        if (total > 0f)
        {
            w1 /= total;
            w2 /= total;
            w3 /= total;
        }
    }
}
