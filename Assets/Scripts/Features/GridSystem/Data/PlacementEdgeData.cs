using UnityEngine;

/// <summary>
/// Structured geometric presentation data for an exposed perimeter edge of a placed block.
/// </summary>
public struct PlacementEdgeData
{
    [Tooltip("World position at the base perimeter edge (Y = -1.0f, offset 0.5f from cell center).")]
    public Vector3 worldPosition;

    [Tooltip("Outward-facing orientation normal to the exposed edge.")]
    public Quaternion rotation;

    public PlacementEdgeData(Vector3 worldPosition, Quaternion rotation)
    {
        this.worldPosition = worldPosition;
        this.rotation = rotation;
    }
}
