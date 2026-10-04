using UnityEngine;
using UnityEngine.Serialization;

public enum PreClearSelectionMode
{
    Single,         // Uses single preClearAnimation
    RandomFromList  // Randomly picks from preClearAnimationPool
}

[CreateAssetMenu(fileName = "NewBlockType", menuName = "Block Blast/Block Type")]
public class BlockTypeSO : ScriptableObject
{
    [Header("Identity")]
    public string typeId = "wood_crate";
    public string displayName = "Wood Crate";

    [Header("3D Variants by Integer ID")]
    [Tooltip("List of 3D cell variants, each mapped to a unique integer variantId.")]
    public BlockVariantData[] variants;

    [Tooltip("Semi-transparent material applied at runtime to cell prefabs to generate shadow blocks.")]
    public Material shadowMaterial;

    [Header("Color Palette Rules")]
    [Range(0f, 1f)]
    [Tooltip("Probability (0.0 to 1.0) of mixing multiple variants within a single shape. 0 = 100% monochrome, 1 = 100% mixed variants, 0.5 = 50/50 balance.")]
    public float mixedVariantChance = 0f;

    [Tooltip("If true, each individual cell in a spawned shape is randomly rotated by 0, 90, 180, or 270 degrees around the Y axis.")]
    public bool randomCellRotationY = true;

    [Header("Pre-Clear Feedback")]
    [Tooltip("Selection mode: Single fixed animation or Random from list.")]
    public PreClearSelectionMode preClearSelectionMode = PreClearSelectionMode.Single;

    [Tooltip("Single pre-clear animation used when selection mode is Single (also acts as fallback).")]
    public PreClearAnimationSO preClearAnimation;

    [Tooltip("Pool of pre-clear animations randomly chosen when selection mode is RandomFromList.")]
    public PreClearAnimationSO[] preClearAnimationPool;

    [Header("Placement Feedback")]
    [Tooltip("Custom placement impact animation executed on each cell when placed on the board.")]
    public PlacementAnimationSO placementAnimation;

    [Tooltip("Custom particle system effects spawned at cell positions and exposed edges when blocks of this type are placed.")]
    public PlacementVFXSO placementVFX;

    [Tooltip("Sound played when placing blocks.")]
    public SoundFXType placeSound = SoundFXType.BlockPlace;

    [Header("=== 1. Signature Clear Combos (Theme Exclusive) ===")]
    [Tooltip("Cohesive signature combos combining cell animation, compatible wave staggers, and sweepers with custom prop prefabs. 100% synchrony guaranteed.")]
    public SignatureClearCombo[] signatureCombos;

    [Header("=== 2. Generic Clears Integration ===")]
    [Tooltip("If true, occasionally mixes in universal animations (Jelly, SquashLaunch...) with universal wave patterns.")]
    public bool allowGenericClears = true;

    [Range(0f, 1f)]
    [Tooltip("Probability of triggering a theme signature combo vs generic clear (0.7 = 70% signature, 30% generic).")]
    public float signatureChance = 0.7f;

    [Tooltip("Shared database of generic animations and staggers. Inherited across all themes.")]
    public ClearFeedbackDatabaseSO globalClearDatabase;

    [Header("=== 3. Fallbacks & Legacy ===")]
    [Tooltip("Single clear stagger wave rhythm used as fallback.")]
    public ClearStaggerSO clearStagger;

    [FormerlySerializedAs("clearAnimation")]
    [FormerlySerializedAs("signatureAnimation")]
    [Tooltip("Fallback clear animation if no combos are configured.")]
    public ClearAnimationSO fallbackClearAnimation;

    // Backwards-compatibility properties
    public ClearAnimationSO clearAnimation => fallbackClearAnimation;
    public ClearAnimationSO signatureAnimation => fallbackClearAnimation;

    [Tooltip("Particle VFX spawned when lines of this block type are cleared.")]
    public GameObject clearVFX;

    [Tooltip("Sound played when clearing lines.")]
    public SoundFXType clearSound = SoundFXType.LineClear;

    [Header("Explosion Audio Feedback")]
    [Tooltip("Custom audio clip played when individual cells of this block type explode. If null, falls back to explosionSoundType.")]
    public AudioClip explosionSound;

    [Tooltip("Volume multiplier for cell explosion sound.")]
    [Range(0f, 1f)] public float explosionSoundVolume = 0.8f;

    /// <summary>
    /// Resolves a coordinated 4-element tuple (ClearAnimationSO, ClearStaggerSO, ClearSweeperBase, GameObject) for line clearing.
    /// Guarantees that sweepers always run with compatible staggers and proper 3D prop prefabs.
    /// </summary>
    public void ResolveClearFeedback(
        out ClearAnimationSO chosenAnim, 
        out ClearStaggerSO chosenStagger, 
        out ClearSweeperBase chosenSweeper,
        out GameObject chosenPropPrefab)
    {
        bool hasCombos = signatureCombos != null && signatureCombos.Length > 0;
        bool playSignature = hasCombos && (!allowGenericClears || UnityEngine.Random.value < signatureChance);

        if (playSignature)
        {
            SignatureClearCombo combo = PickWeightedCombo();
            chosenAnim = combo != null ? combo.animation : fallbackClearAnimation;
            chosenStagger = combo != null ? combo.GetRandomStagger() : clearStagger;

            SweeperOption sweeperOpt = combo?.GetRandomSweeperOption();
            chosenSweeper = sweeperOpt?.sweeper;
            chosenPropPrefab = sweeperOpt?.GetRandomPrefab();
        }
        else
        {
            if (globalClearDatabase != null)
            {
                chosenAnim = globalClearDatabase.GetRandomGenericAnimation();
                chosenStagger = globalClearDatabase.GetRandomGenericStagger();
            }
            else
            {
                chosenAnim = fallbackClearAnimation;
                chosenStagger = clearStagger;
            }
            chosenSweeper = null;
            chosenPropPrefab = null;
        }

        // Safety fallback against nulls
        if (chosenAnim == null) chosenAnim = fallbackClearAnimation;
        if (chosenStagger == null) chosenStagger = clearStagger;
    }

    /// <summary>
    /// Backwards-compatibility overload without sweeper and prop parameters.
    /// </summary>
    public void ResolveClearFeedback(out ClearAnimationSO chosenAnim, out ClearStaggerSO chosenStagger)
    {
        ResolveClearFeedback(out chosenAnim, out chosenStagger, out _, out _);
    }

    private SignatureClearCombo PickWeightedCombo()
    {
        if (signatureCombos == null || signatureCombos.Length == 0) return null;
        if (signatureCombos.Length == 1) return signatureCombos[0];

        int totalWeight = 0;
        for (int i = 0; i < signatureCombos.Length; i++)
        {
            if (signatureCombos[i] != null)
            {
                totalWeight += Mathf.Max(1, signatureCombos[i].weight);
            }
        }

        if (totalWeight <= 0) return signatureCombos[0];

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int accumulated = 0;
        for (int i = 0; i < signatureCombos.Length; i++)
        {
            if (signatureCombos[i] != null)
            {
                accumulated += Mathf.Max(1, signatureCombos[i].weight);
                if (roll < accumulated)
                {
                    return signatureCombos[i];
                }
            }
        }
        return signatureCombos[0];
    }

    /// <summary>
    /// Resolves the active pre-clear animation according to the configured selection mode.
    /// Falls back to preClearAnimation if the pool is empty or invalid.
    /// </summary>
    public PreClearAnimationSO GetPreClearAnimation()
    {
        if (preClearSelectionMode == PreClearSelectionMode.RandomFromList && 
            preClearAnimationPool != null && preClearAnimationPool.Length > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, preClearAnimationPool.Length);
            if (preClearAnimationPool[randomIndex] != null)
            {
                return preClearAnimationPool[randomIndex];
            }
        }
        return preClearAnimation;
    }

    /// <summary>
    /// Resolves the fallback clear stagger rhythm.
    /// </summary>
    public ClearStaggerSO GetClearStagger()
    {
        return clearStagger;
    }

    public GameObject GetPrefab(int variantId)
    {
        if (variants == null) return null;
        for (int i = 0; i < variants.Length; i++)
        {
            if (variants[i].variantId == variantId)
            {
                return variants[i].prefab;
            }
        }
        return variants.Length > 0 ? variants[0].prefab : null;
    }
}
