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

    [Header("Clear Feedback")]
    [Tooltip("Particle VFX spawned when lines of this block type are cleared.")]
    public GameObject clearVFXPrefab;
    public SoundFXType clearSound = SoundFXType.LineClear;
    public SoundFXType placeSound = SoundFXType.BlockPlace;

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
