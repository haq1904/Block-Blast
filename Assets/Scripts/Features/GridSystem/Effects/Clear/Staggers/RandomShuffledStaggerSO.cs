using UnityEngine;

/// <summary>
/// True randomized pop order across cells in a cleared line, providing dynamic arcade feedback.
/// Fully stateless and safe for ScriptableObjects.
/// </summary>
[CreateAssetMenu(fileName = "RandomShuffledStagger", menuName = "Block Blast/Effects/Clear Stagger/Random Shuffled")]
public class RandomShuffledStaggerSO : ClearStaggerSO
{
    [Header("Randomization Settings")]
    [Tooltip("If true, picks discrete step indices (0, 1, 2... N-1). If false, picks continuous smooth float delays.")]
    public bool discreteSteps = true;

    public override float CalculateDelay(
        int indexInLine,
        int totalInLine,
        Vector3 cellWorldPos,
        Vector3 placementOrigin)
    {
        if (totalInLine <= 1 || stepDelay <= 0f) return 0f;

        if (discreteSteps)
        {
            int randomIndex = Random.Range(0, totalInLine);
            return randomIndex * stepDelay;
        }
        else
        {
            float randomFactor = Random.Range(0f, totalInLine - 1);
            return randomFactor * stepDelay;
        }
    }
}
