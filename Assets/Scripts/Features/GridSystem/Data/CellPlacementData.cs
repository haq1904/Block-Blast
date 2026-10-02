using System;
using UnityEngine;

[Serializable]
public struct CellPlacementData
{
    public Vector2Int gridPos;
    public string blockTypeId;
    public int variantIndex;
    public int rotationY;

    public CellPlacementData(Vector2Int gridPos, string blockTypeId, int variantIndex, int rotationY = 0)
    {
        this.gridPos = gridPos;
        this.blockTypeId = blockTypeId;
        this.variantIndex = variantIndex;
        this.rotationY = rotationY;
    }
}
