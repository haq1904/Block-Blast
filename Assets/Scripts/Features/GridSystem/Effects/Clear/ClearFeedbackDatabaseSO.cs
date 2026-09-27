using UnityEngine;

/// <summary>
/// Universal shared library of generic clear animations and stagger rhythms.
/// Inherited across all block themes, allowing themes to easily mix generic clears with signature moves.
/// </summary>
[CreateAssetMenu(fileName = "ClearFeedbackDatabase", menuName = "Block Blast/Effects/Clear/Clear Feedback Database")]
public class ClearFeedbackDatabaseSO : ScriptableObject
{
    [Header("Generic Cell Animations (Universal)")]
    [Tooltip("Pool of universal cell explosion animations usable by any theme (SquashLaunch, Jelly, SwellPop, VortexTwist...).")]
    public ClearAnimationSO[] genericAnimations;

    [Header("Generic Stagger Wave Rhythms (Universal)")]
    [Tooltip("Pool of universal wave propagation rhythms usable by any theme (CenterOutward, Checkerboard, Instant...).")]
    public ClearStaggerSO[] genericStaggers;

    /// <summary>
    /// Randomly selects a generic cell animation from the pool.
    /// </summary>
    public ClearAnimationSO GetRandomGenericAnimation()
    {
        if (genericAnimations == null || genericAnimations.Length == 0) return null;
        int randomIndex = Random.Range(0, genericAnimations.Length);
        return genericAnimations[randomIndex];
    }

    /// <summary>
    /// Randomly selects a generic stagger wave rhythm from the pool.
    /// </summary>
    public ClearStaggerSO GetRandomGenericStagger()
    {
        if (genericStaggers == null || genericStaggers.Length == 0) return null;
        int randomIndex = Random.Range(0, genericStaggers.Length);
        return genericStaggers[randomIndex];
    }
}
