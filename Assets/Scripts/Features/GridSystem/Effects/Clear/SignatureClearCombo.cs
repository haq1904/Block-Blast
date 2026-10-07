using System;
using UnityEngine;

/// <summary>
/// Configures a sweeper motion profile along with a pool of theme-specific 3D prop prefabs.
/// Adheres strictly to visual_effects.md and architecture.md.
/// </summary>
[Serializable]
public class SweeperOption
{
    [Tooltip("Optional variant identifier or display name (e.g. 'Boomerang Bone', 'Tennis Ball').")]
    public string variantId;

    [Tooltip("Motion choreography profile (Chop & Drag, Spin & Cut, etc.).")]
    public ClearSweeperBase sweeper;

    [Tooltip("List of theme-specific 3D prop prefabs for this sweeper (e.g. Axe variations, Saw variations). One is picked randomly.")]
    public GameObject[] propPrefabs;

    [Range(0, 100)]
    [Tooltip("Selection percentage for this sweeper within the combo. Values are normalized against the total positive weight.")]
    public int weightPercent = 100;

    public GameObject GetRandomPrefab()
    {
        if (propPrefabs == null || propPrefabs.Length == 0) return null;
        return propPrefabs[UnityEngine.Random.Range(0, propPrefabs.Length)];
    }
}

/// <summary>
/// Cohesive signature combo tying 1 cell animation, compatible wave staggers, and sweepers with custom prop prefabs.
/// Guarantees that directional sweepers always run with compatible directional staggers.
/// </summary>
[Serializable]
public class SignatureClearCombo
{
    [Tooltip("Identifier for this combo in Inspector (e.g. 'Axe Serpentine Chop', 'SawBlade Rotary Cut').")]
    public string comboName;

    [Tooltip("Cell explosion animation executed on each block (flash, compression, tremor, theme debris).")]
    public ClearAnimationSO animation;

    [Tooltip("Pool of compatible wave stagger rhythms randomly chosen for this combo (e.g. SequentialForward, SequentialBackward).")]
    public ClearStaggerSO[] staggers;

    [Tooltip("Pool of compatible sweepers, each with its own list of theme prop prefabs.")]
    public SweeperOption[] sweepers;

    [Range(1, 100)]
    [Tooltip("Relative probability weight when choosing between signature combos.")]
    public int weight = 10;

    public ClearStaggerSO GetRandomStagger()
    {
        if (staggers == null || staggers.Length == 0) return null;
        return staggers[UnityEngine.Random.Range(0, staggers.Length)];
    }

    public SweeperOption GetRandomSweeperOption()
    {
        if (sweepers == null || sweepers.Length == 0) return null;

        int totalWeight = 0;
        int nonNullCount = 0;
        for (int i = 0; i < sweepers.Length; i++)
        {
            SweeperOption option = sweepers[i];
            if (option == null) continue;
            nonNullCount++;
            if (option.weightPercent > 0)
            {
                totalWeight += option.weightPercent;
            }
        }

        if (nonNullCount == 0) return null;

        if (totalWeight > 0)
        {
            int roll = UnityEngine.Random.Range(0, totalWeight);
            int accumulatedWeight = 0;
            for (int i = 0; i < sweepers.Length; i++)
            {
                SweeperOption option = sweepers[i];
                if (option == null || option.weightPercent <= 0) continue;
                accumulatedWeight += option.weightPercent;
                if (roll < accumulatedWeight)
                {
                    return option;
                }
            }
        }

        // Fallback: When all weights are 0, pick uniformly among non-null options.
        int pickIndex = UnityEngine.Random.Range(0, nonNullCount);
        for (int i = 0; i < sweepers.Length; i++)
        {
            if (sweepers[i] != null)
            {
                if (pickIndex == 0) return sweepers[i];
                pickIndex--;
            }
        }

        return null;
    }
}
