using UnityEngine;

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

    [Header("Shared Pre-Clear Effect")]
    [Tooltip("Dynamic pre-clear effect executed when lines are pending explosion.")]
    public PreClearEffectSO preClearEffect;

    [Header("Placement Feedback")]
    [Tooltip("Custom particle system effects spawned at cell positions and exposed edges when blocks of this type are placed.")]
    public PlacementPSEffectSO placementPSEffect;

    [Tooltip("Custom placement impact animation executed on each cell when placed on the board.")]
    public PlacementEffectSO placementEffect;

    [Tooltip("Sound played when placing blocks.")]
    public SoundFXType placeSound = SoundFXType.BlockPlace;

    [Header("Clear Feedback")]
    [Tooltip("Dynamic animation & rhythm executed on each cell right before it shatters.")]
    public ClearAnimationEffectSO clearAnimationEffect;

    [Tooltip("Particle VFX spawned when lines of this block type are cleared.")]
    public GameObject clearVFXPrefab;

    [Tooltip("Special celebration particle VFX spawned when multiple lines/combos are cleared.")]
    public GameObject comboClearVFXPrefab;

    [Tooltip("Sound played when clearing lines.")]
    public SoundFXType clearSound = SoundFXType.LineClear;

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
