using System;
using UnityEngine;

[Serializable]
public struct BlockVariantData
{
    [Tooltip("Unique integer ID for this color/model variant, e.g., 1, 2, 3.")]
    public int variantId;

    [Tooltip("The 3D 1x1x1 cell prefab associated with this variant.")]
    public GameObject prefab;

    public BlockVariantData(int variantId, GameObject prefab)
    {
        this.variantId = variantId;
        this.prefab = prefab;
    }
}
