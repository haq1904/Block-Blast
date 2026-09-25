using UnityEngine;

/// <summary>
/// Lightweight data payload representing a single cell in a cleared line passed from GridView to ClearStaggerSO.
/// Pure C# struct adhering to architecture.md (Humble View pass-through without matrix logic in View).
/// </summary>
public struct ClearCellItem
{
    public GameObject gameObject;
    public Transform transform;
    public Vector2Int gridPos;
    public Vector3 canonicalPos;
    public int indexInLine;
    public int totalInLine;
}
