using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Immutable snapshot representing an individual exploding cell along a clear wave timeline.
/// </summary>
public readonly struct CellWaypoint
{
    public readonly Vector2Int gridPos;
    public readonly Vector3 worldPos;
    public readonly float hitTime;        // Exact explosion timestamp (relative to wave start t=0)
    public readonly int indexInLine;
    public readonly int totalInLine;

    public CellWaypoint(Vector2Int gridPos, Vector3 worldPos, float hitTime, int indexInLine, int totalInLine)
    {
        this.gridPos = gridPos;
        this.worldPos = worldPos;
        this.hitTime = hitTime;
        this.indexInLine = indexInLine;
        this.totalInLine = totalInLine;
    }
}

/// <summary>
/// Data-driven musical score context assembled by ClearStaggerSO and passed to ClearSweeperBase.
/// Contains complete spatial and temporal waypoints of all exploding blocks in the clear wave.
/// </summary>
public class ClearTimelineContext
{
    public IReadOnlyList<CellWaypoint> waypoints;
    public Vector3 startPos;              // First exploding cell coordinate
    public Vector3 endPos;                // Last exploding cell coordinate
    public Vector3 direction;             // Primary line sweep direction
    public float totalSweepDuration;      // Duration from first to last explosion
    public float leadOffset;              // Sweeper hold delay (if sweeper moves too fast, wait for cell anticipation)
    public float firstBlockDelay;         // Cell hold delay (if sweeper arrives slowly, cell waits before squash)

    /// <summary>
    /// Specific 3D prop prefab resolved by the Theme combo to spawn for the sweeper.
    /// </summary>
    public GameObject overridePrefab;

    /// <summary>
    /// Active scene camera used for viewport calculations during exit sweeps.
    /// </summary>
    public Camera viewCamera;

    /// <summary>
    /// Shared world-space exit target resolved outside the viewport.
    /// </summary>
    public Vector3 visualExitPosition;

    public CellWaypoint FirstWaypoint => waypoints != null && waypoints.Count > 0 ? waypoints[0] : default;
    public CellWaypoint LastWaypoint => waypoints != null && waypoints.Count > 0 ? waypoints[waypoints.Count - 1] : default;
}
