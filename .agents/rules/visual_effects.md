---
trigger: always_on
description: Visual feedback and strategy pattern guidelines for AnimationSO, StaggerSO, and View delegation.
---

# Block Blast - Visual Feedback & Strategy Pattern Guidelines

The project leverages ScriptableObject Strategy Patterns for plug-and-play visual feedback (Placement, Pre-Clear, and Clear effects). To preserve clean architectural boundaries and prevent View bloat, responsibilities are strictly divided among four distinct layers:

```
[ Scene GridView ] (Humble Scene Holder & Dispatcher)
       │
       ├──> [ ClearStaggerSO ] (Wave Orchestrator: Timing & Rhythm)
       │         └── Spawns Prop via IPoolService & Delegates to Prop Strategy
       │         └── Plays Step Particle / Step Audio
       │
       ├──> [ ClearPropBase ] (Prop Choreographer: Movement, Spin, Shake & VFX)
       │         └── Pure Prefab: Meshes + Particles only, NO custom MonoBehaviours
       │         └── Drives DOTween Sequence, Finite Spin, Alpha Fade & Particle Triggers
       │
       └──> [ ClearAnimationSO ] (Cell & Theme Specialist: Mesh Morphing & Debris)
                 └── Performs Block Squash / Burst / Shatter Tweens
                 └── Spawns 3-Tier Theme Debris (Burst, Dynamics, Shards)
                 └── Provides Theme Line Prop Strategy Reference
```

---

## 1. Animation Strategy (`ClearAnimationSO`, `PreClearAnimationSO`, `PlacementAnimationSO`)
*   **Role**: Theme & Cell Specialist (**WHAT** happens to the cell mesh and what theme assets exist).
*   **Core Responsibilities**:
    1.  **Mesh Morphing**: Animates the cell `Transform` directly using DOTween (e.g., scale punch, squeeze, jump, twist, dissolve).
    2.  **Theme VFX Layers**: Encapsulates 3-tier thematic particle effects (`layer1_BurstPrefab`, `layer2_DynamicsPrefab`, `layer3_DebrisPrefab`) and shader feedback (e.g., flash color).
    3.  **Thematic Prop Provider**: Holds references to theme-specific props (`ClearPropBase`, e.g. SawBlade for Wood, Pickaxe for Ice, Drill for Stone) and supports single or randomized prop selection.
*   **Strict Prohibitions**:
    *   MUST NOT calculate multi-cell wave propagation timings, delays, or line sweep trajectories.
    *   MUST NOT instantiate or control line sweep props across rows/columns.
    *   MUST NOT query or iterate the 8x8 grid matrix.

---

## 2. Stagger Strategy (`ClearStaggerSO`, `SequentialForwardStaggerSO`, etc.)
*   **Role**: Wave & Rhythm Orchestrator (**HOW & WHEN** the visual wave propagates across time and space).
*   **Core Responsibilities**:
    1.  **Temporal Rhythm**: Calculates individual cell delays (`CalculateDelay`) based on its algorithmic wave pattern (Sequential, Center-Outward, Checkerboard, Random, Instant).
    2.  **Prop & Wave Orchestration (`Play`)**:
        *   Implements the full wave orchestration method:
            ```csharp
            public virtual void Play(
                List<ClearCellItem> lineCells, 
                ClearAnimationSO clearAnimation, 
                IPoolService poolService,
                Action<Vector2Int> onCellExploded)
            ```
        *   **Delegated Prop Spawning**: Spawns the prop via `IPoolService`, calculates line endpoints and direction (`dir`), and delegates the entire motion choreography to `propData.AnimatePropSequence(prop, firstPos, lastPos, dir, sweepDuration, leadOffset, poolService)`.
        *   **Cell Explosion Dispatching**: Iterates through `lineCells`, calculates individual delays, and triggers `clearAnimation.Play(...)` for each cell. On explosion, spawns step-specific wave VFX (`stepParticlePrefabs`), restores canonical transform, returns block to pool, and invokes `onCellExploded(gridPos)` to release the cell in `GridModel`.
*   **Strict Prohibitions**:
    *   MUST NOT store runtime `Tween`, `Transform`, or `GameObject` references in instance fields (strictly adheres to `dotween.md` Stateless SO Rule).
    *   MUST NOT assume all clears have props; default implementation in base class MUST be a safe virtual no-op for prop sweeps.

---

## 3. Prop Strategy (`ClearPropBase`) & Pure-Prefab Architecture
*   **Role**: Prop Specialist & Choreographer (**HOW** the physical prop acts, animates, and interacts with the board).
*   **Core Responsibilities**:
    1.  **Pure Prefab Mandate**:
        *   Prop prefabs (`Axe.prefab`, `SawBlade.prefab`, `Hammer.prefab`) MUST remain **Pure Visual Assets**.
        *   They contain strictly Unity standard components (`Transform`, `MeshFilter`, `MeshRenderer`, `ParticleSystem`) and the generic safety timeout `AutoReturnToPool`.
        *   **Strict Prohibition**: Never create or attach custom MonoBehaviour scripts dedicated to individual props (e.g., NO `AxePropFeedback`, `SawBladeSpinner`, `HammerFeedback`).
    2.  **Stateless SO Choreographer**:
        *   All dynamic behaviors (translation, pitch/angle, continuous spinning, micro-shake, particle triggering, alpha fading) MUST be driven purely by `ClearPropBase` subclasses via DOTween sequences inside `AnimatePropSequence()`.
        *   SOs must NEVER store active runtime references (`Tween`, `Transform`, `GameObject`) in instance fields (strictly adheres to `dotween.md` Stateless SO Rule).
    3.  **Finite Lifecycles (No Infinite Loops)**:
        *   Props with continuous spinning (e.g. SawBlade) must calculate finite rotations over the prop's total lifetime (`totalDuration = entryDuration + sweepDuration + exitDuration`) instead of using unbounded `.SetLoops(-1)` loops.
        *   Micro-shake and particle feedback must be joined directly into the sequence with explicit phase durations (`sweepDuration`).
    4.  **Shader & Material Property Block Standards**:
        *   Fade in/out and transparency MUST use cached static `MaterialPropertyBlock` instances without cloning materials (strictly adheres to `shaders.md`).
        *   When `alpha >= 0.999f` or when returning to pool, reset `renderer.SetPropertyBlock(null)` to preserve URP SRP Batching.
    5.  **Reusable Base Helpers**:
        *   Common routines (e.g., `SetPropAlpha(GameObject prop, float alpha)` and `SetPropParticles(GameObject prop, bool play, Vector3? sprayDir)`) must reside in `ClearPropBase` for 100% subclass reuse.

---

## 4. View Layer (`GridView`) - The Humble Dispatcher
*   **Role**: Scene Holder & Passive Dispatcher.
*   **Core Responsibilities**:
    1.  **Scene Component Ownership**: Owns the actual scene instances (`visualGrid[col, row]`), transforms, and grid coordinates.
    2.  **Matrix Detachment**: Unbinds cleared blocks immediately from the active visual matrix (`visualGrid[col, row] = null`) to free the cells for gameplay logic.
    3.  **Pure Delegation**:
        *   Packages cleared line cells into `List<ClearCellItem>` and delegates the entire clear wave to Stagger via a single call:
            ```csharp
            stagger?.Play(lineCells, clearEffect, poolService, ReleaseCellOnGrid);
            ```
    4.  **Fail-Safe Cleanup (`OnDisable`)**: Maintains lifecycle tracking (`activeClearingBlocks`, `ClearActiveClearingBlocks`) to instantly kill tweens, restore canonical transforms, and return blocks to the pool if the scene is unloaded or reset mid-animation.
*   **Strict Prohibitions (Manual Wiring Ban)**:
    *   MUST NOT execute manual `for` loops to animate individual cell explosions inside View methods.
    *   MUST NOT manually instantiate or stitch together individual loose particle prefabs from `theme.clearVFX`, `stagger.stepParticlePrefabs`, and `clearAnimation` layers in View code.
    *   MUST NOT manually compute prop movement math, DOTween paths, or sweep velocities inside View methods.
    *   MUST NOT fire flat single-shot audio clips at line start when visual wave propagation spans across multiple delayed steps.

---

## 5. Multi-Line & Cross-Clear Safety Rules
1.  **Line Waypoints vs Real GameObjects**:
    *   *Waypoints*: When horizontal and vertical lines intersect, full waypoints (8 coordinates) MUST be provided for both lines so that prop sweeps (e.g. crossing saw blades) execute completely from border to border.
    *   *Real Cell Transforms*: The intersection block at `(col, row)` exists ONLY ONCE in the scene. It MUST be queued and exploded exactly once to prevent double-return errors in `IPoolService`.
2.  **Object Pool & DOTween Safety**:
    *   All spawned props MUST have `AutoReturnToPool` attached as a safety timeout.
    *   All tweens launched on props or cells MUST use `.SetTarget(obj)` and `.SetLink(obj, LinkBehaviour.KillOnDisable)` to eliminate orphan tweens and memory leaks.
