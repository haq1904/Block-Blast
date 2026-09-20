using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewPlacementPSEffect", menuName = "Block Blast/Effects/Grid/Placement/Placement PS Effect")]
public class PlacementPSEffectSO : ScriptableObject
{
    [Header("Particle Prefabs")]
    [Tooltip("Particle system prefab for the smoke puff emitted along 1-unit exposed outer edges.")]
    public GameObject smokePuffPrefab;

    [Tooltip("Particle system prefab for the debris / splinters flying from cell centers.")]
    public GameObject debrisPrefab;

    /// <summary>
    /// Executes the placement particle effects.
    /// Smoke puff is spawned along exposed edges facing outward.
    /// Debris is spawned at each cell center.
    /// </summary>
    public virtual void Play(
        List<PlacementEdgeData> exposedEdges,
        List<Vector3> cellWorldPositions,
        IPoolService poolService)
    {
        if (poolService == null) return;

        // 1. Emit Smoke Puff along each exposed outer edge (1 unit length)
        if (exposedEdges != null && smokePuffPrefab != null)
        {
            for (int i = 0; i < exposedEdges.Count; i++)
            {
                poolService.SpawnObject(
                    smokePuffPrefab,
                    exposedEdges[i].worldPosition,
                    exposedEdges[i].rotation,
                    PoolType.ParticleSystem);
            }
        }

        // 2. Emit Debris at each placed cell's center
        if (cellWorldPositions != null && debrisPrefab != null)
        {
            for (int i = 0; i < cellWorldPositions.Count; i++)
            {
                poolService.SpawnObject(
                    debrisPrefab,
                    cellWorldPositions[i],
                    Quaternion.identity,
                    PoolType.ParticleSystem);
            }
        }
    }
}
