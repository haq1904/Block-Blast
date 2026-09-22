using UnityEngine;

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
    [Tooltip("If true, all cells in a single shape share the same variantId. If false, a shape mixes variants.")]
    public bool isMonochromePerShape = true;

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

    [Header("Clear Feedback")]
    [Tooltip("Dynamic animation executed on each cell right before it shatters.")]
    public ClearAnimationSO clearAnimation;

    [Tooltip("Selection mode: Single fixed stagger rhythm or Random from list.")]
    public ClearStaggerSelectionMode clearStaggerSelectionMode = ClearStaggerSelectionMode.Single;

    [Tooltip("Single clear stagger wave rhythm used when selection mode is Single (also acts as fallback).")]
    public ClearStaggerSO clearStagger;

    [Tooltip("Pool of clear stagger wave rhythms randomly chosen when selection mode is RandomFromList.")]
    public ClearStaggerSO[] clearStaggerPool;

    [Tooltip("Particle VFX spawned when lines of this block type are cleared.")]
    public GameObject clearVFX;

    [Tooltip("Sound played when clearing lines.")]
    public SoundFXType clearSound = SoundFXType.LineClear;

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
    /// Resolves the active clear stagger rhythm according to the configured selection mode.
    /// Falls back to clearStagger if the pool is empty or invalid.
    /// </summary>
    public ClearStaggerSO GetClearStagger()
    {
        if (clearStaggerSelectionMode == ClearStaggerSelectionMode.RandomFromList &&
            clearStaggerPool != null && clearStaggerPool.Length > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, clearStaggerPool.Length);
            if (clearStaggerPool[randomIndex] != null)
            {
                return clearStaggerPool[randomIndex];
            }
        }
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
