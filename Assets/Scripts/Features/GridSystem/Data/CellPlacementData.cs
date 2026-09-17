using System;
using UnityEngine;

[Serializable]
public struct CellPlacementData
{
    public Vector2Int gridPos;
    public string blockTypeId;
    public int variantIndex;

    public CellPlacementData(Vector2Int gridPos, string blockTypeId, int variantIndex)
    {
        this.gridPos = gridPos;
        this.blockTypeId = blockTypeId;
        this.variantIndex = variantIndex;
    }
}
