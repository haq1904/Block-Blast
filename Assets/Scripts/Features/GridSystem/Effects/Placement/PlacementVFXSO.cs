using System.Collections.Generic;
using UnityEngine;

public enum DebrisFloorBehavior
{
    Bounce,         // Particles bounce off the floor upon impact
    SettleOnFloor,  // Particles hit the floor and lie still without bouncing
    NoCollision     // Particles ignore floor collision
}

[CreateAssetMenu(fileName = "NewPlacementVFX", menuName = "Block Blast/Effects/Grid/Placement/Placement VFX")]
public class PlacementVFXSO : ScriptableObject
{
    [Header("Smoke Layer (Base Edge Puff)")]
    [Tooltip("Particle system prefab for the smoke puff emitted along 1-unit exposed outer bottom edges.")]
    public GameObject smokePuffPrefab;

    [Header("Debris Layer (Top Edge Fall)")]
    [Tooltip("Shared particle system prefab for edge debris / chips.")]
    public GameObject debrisPrefab;

    [Tooltip("Custom 3D Mesh for this block type's debris (e.g. Cube, Shard, Crystal).")]
    public Mesh debrisMesh;

    [Tooltip("Custom Material for this block type's debris.")]
    public Material debrisMaterial;

    [Header("Debris Motion & Physics")]
    [Tooltip("Height of the top edge above the grid base (Y = -1). Default is 1.0 for 1x1x1 blocks.")]
    public float blockHeight = 1.0f;

    [Tooltip("Downward gravity strength pulling debris down to the floor.")]
    [Range(0.5f, 10f)]
    public float debrisGravity = 3.5f;

    [Tooltip("Minimum and maximum initial outward speed from the top edge.")]
    public Vector2 debrisStartSpeed = new Vector2(0.8f, 1.8f);

    [Tooltip("Total lifetime in seconds before particles naturally shrink/fade and disappear.")]
    [Range(0.3f, 3.0f)]
    public float debrisLifetime = 1.0f;

    [Header("Debris Collision & Floor Behavior")]
    [Tooltip("Collision behavior when debris particles hit the floor.")]
    public DebrisFloorBehavior floorBehavior = DebrisFloorBehavior.Bounce;

    [Tooltip("Bounce elasticity when floorBehavior is Bounce (0 = no bounce, 1 = full bounce).")]
    [Range(0f, 1f)]
    public float bounceStrength = 0.4f;

    [Tooltip("Friction / energy loss upon collision (0 = slick, 1 = dead stop).")]
    [Range(0f, 1f)]
    public float floorDampen = 0.3f;

    /// <summary>
    /// Executes the placement particle effects.
    /// Smoke puff is spawned along exposed bottom edges facing outward.
    /// Debris is spawned along exposed top edges facing outward and falls to the ground.
    /// </summary>
    public virtual void Play(
        List<PlacementEdgeData> exposedEdges,
        List<Vector3> cellWorldPositions,
        IPoolService poolService)
    {
        if (poolService == null) return;

        // 1. Emit Smoke Puff along each exposed outer bottom edge
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

        // 2. Emit Debris along each exposed outer top edge
        if (exposedEdges != null && debrisPrefab != null)
        {
            Vector3 heightOffset = Vector3.up * blockHeight;
            for (int i = 0; i < exposedEdges.Count; i++)
            {
                Vector3 topEdgePos = exposedEdges[i].worldPosition + heightOffset;
                GameObject spawned = poolService.SpawnObject(
                    debrisPrefab,
                    topEdgePos,
                    exposedEdges[i].rotation,
                    PoolType.ParticleSystem);

                ConfigureDebrisInstance(spawned);
            }
        }
    }

    /// <summary>
    /// Configures the spawned debris ParticleSystem instance with this block type's custom visuals and physics.
    /// </summary>
    protected virtual void ConfigureDebrisInstance(GameObject instance)
    {
        if (instance == null) return;

        var ps = instance.GetComponent<ParticleSystem>();
        if (ps == null) return;

        // Override custom Mesh & Material on renderer
        var psRenderer = instance.GetComponent<ParticleSystemRenderer>();
        if (psRenderer != null)
        {
            if (debrisMesh != null) psRenderer.mesh = debrisMesh;
            if (debrisMaterial != null) psRenderer.sharedMaterial = debrisMaterial;
        }

        // Override main module properties
        var main = ps.main;
        main.gravityModifier = debrisGravity;
        main.startSpeed = new ParticleSystem.MinMaxCurve(debrisStartSpeed.x, debrisStartSpeed.y);
        main.startLifetime = debrisLifetime;

        // Override collision module properties
        var collision = ps.collision;
        if (floorBehavior == DebrisFloorBehavior.NoCollision)
        {
            collision.enabled = false;
        }
        else
        {
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;

            if (floorBehavior == DebrisFloorBehavior.Bounce)
            {
                collision.bounce = bounceStrength;
                collision.dampen = floorDampen;
            }
            else // SettleOnFloor
            {
                collision.bounce = 0f;
                collision.dampen = 1.0f;
            }
        }

        // Clear previous state and emit cleanly
        ps.Clear();
        ps.Play();
    }
}
