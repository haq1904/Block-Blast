# Block Blast - 3D Modeling & Blender Optimization Guidelines

Performance, visual consistency, and memory efficiency are paramount for mobile game development (60/120 FPS). All 3D assets created in Blender or sourced externally for the "Block Blast" project MUST strictly adhere to the following modeling, texturing, and export rules.

---

## 1. Polygon & Triangle Budget (Mesh Optimization)

Keep geometry strictly stylized, clean, and low-poly. Eliminate unnecessary edge loops and subdivisions that provide zero visual value from a top-down camera perspective.

### A. Triangle Budget per Asset Category

| Asset Category | Target Tris Budget | Hard Upper Limit | Examples |
| :--- | :---: | :---: | :--- |
| **Gameplay Cell Blocks** | 100 – 300 tris | 500 tris | `WoodCrate`, `JellyBlock`, `StoneBlock` |
| **Micro-Debris / Particles** | 12 – 40 tris | 60 tris | `Mesh_MicroDebris_Wood`, `JellyDrop` |
| **Spinning Tools & Props** | 200 – 500 tris | 800 tris | `SawBlade`, `DrillBit`, `RotatingGear` |
| **Machinery & Complex Props** | 600 – 1,000 tris | 1,500 tris | `CircularSawMachine`, `Chainsaw`, `Bomb` |

### B. Geometry & Topology Rules
1. **Zero N-Gons (Strict Ban)**:
   * Meshes MUST contain only **Quads and Triangles** before export.
   * Planar or complex faces must be triangulated (`Ctrl + T`) to avoid runtime rendering tears, shading artifacts, or non-planar warping.
2. **Hidden Face Culling (Delete Invisible Polygons)**:
   * Always delete faces that will never be visible to the player (e.g., the bottom face of blocks resting on the grid, interior cavity walls of sealed casings, underside surfaces of mounted machinery).
3. **Cylinder & Wheel Subdivisions**:
   * Cylinders, wheels, saw discs, and circular handles must use **12 to 16 vertices** (maximum 16).
   * **Strict Prohibition**: Never use the default 32 or 64 vertices for cylinders on mobile assets.
4. **Beveling Constraints**:
   * For stylized rounded edges, use a **single segment bevel** (1 segment with `Profile: 0.5`).
   * Never use 3–4 segment high-poly bevels on mobile gameplay assets.

---

## 2. UV Mapping & Color Palette Workflow (1 Draw Call Standard)

To achieve maximum draw call batching via the **URP SRP Batcher**, all models must share the global color palette atlas.

1. **Point-Collapsed UV Mapping**:
   * In Blender's UV Editor, collapse face UV islands down to a single zero-size point (`S` $\rightarrow$ `0` $\rightarrow$ `Enter`).
   * Place (`G`) the collapsed point into the appropriate solid color swatch on the project's color palette texture:
     `Assets/Art/LeapLand/tex/palette.png`
2. **Strict Single-Material Standard**:
   * Every model MUST use **exactly 1 Material slot** bound to `DefaultMat` (`Assets/Art/Blocks/DefaultMat.mat`).
   * Do NOT assign multiple materials to different sub-faces of a single prop (e.g., metal blade vs wooden handle). Both colors MUST be sampled from the same `palette.png` using UV coordinates.
3. **No Dedicated 1K/2K Textures**:
   * Do NOT create separate diffuse, normal, or specular map textures for individual props unless explicitly approved for specialized hero VFX.

---

## 3. Pivot Point (Origin) & Part Separation

Correct pivot placement is vital for procedural animations, DOTween rotations, and physical juice.

### A. Rotating / Spinning Components (Saw Blades, Wheels, Gears)
1. **Geometric Center Alignment**:
   * The Pivot (Origin) of any rotating part MUST be placed **exactly at the geometric center of its axle/cylinder**:
     * In Edit Mode: Select the center vertex or circular boundary loop $\rightarrow$ `Shift + S` $\rightarrow$ `Cursor to Selected`.
     * In Object Mode: Right-click $\rightarrow$ `Set Origin` $\rightarrow$ `Origin to 3D Cursor`.
   * **Rule**: When DOTween invokes `DORotate(new Vector3(0, 0, 360f), ...)`, the component must spin perfectly on-axis without wobbling.
2. **Hierarchical Part Separation**:
   * Props with moving parts (e.g., a Circular Saw) MUST be separated into distinct child objects under a root GameObject:
     ```
     CircularSaw_Machine (Root Empty: Position = 0, 0, 0)
        ├── Body (Stationary casing, handle, motor)
        └── Blade (Spinning blade with Origin at disc center)
     ```
   * Never combine stationary and moving elements into a single flattened mesh.

### B. Gameplay Blocks & Ground Props
* The Pivot MUST be located at the **bottom center**:
  * `X = 0`, `Z = 0`, `Y = 0` (Base plane contacting the board).
  * Mesh height extends upward from `Y = 0` to `Y = 1.0` (Unit bounding box: $1.0 \times 1.0 \times 1.0$).

---

## 4. Shading & Normals (Eliminating Dirty Shading Artifacts)

1. **Auto Smooth Standard**:
   * Never apply unconditional `Shade Smooth` over 90-degree planar angles without angle gating.
   * Always enable **`Shade Auto Smooth`** with an angle threshold between **$30^\circ$ and $45^\circ$** (or use the `Smooth by Angle` modifier in Blender 4.x).
2. **Marking Sharp Edges**:
   * For sharp mechanical cuts, teeth on saw blades, or fractured edges, mark them explicitly as **Sharp** in Edit Mode (`Ctrl + E` $\rightarrow$ `Mark Sharp`).
3. **Recalculate Normals**:
   * Prior to export, ensure all face normals point outward: Select all (`A`) in Edit Mode $\rightarrow$ `Shift + N` (`Recalculate Outside`).

---

## 5. Freeze Transforms & FBX Export Settings

Dirty transformations (unapplied rotations or inverted scaling) cause severe runtime coordinate glitches and desynchronized tweens in Unity.

### A. Mandatory Freeze Before Export
* In Blender Object Mode, select all components:
  * Press **`Ctrl + A`** $\rightarrow$ select **`All Transforms`** (or `Rotation & Scale`).
  * Ensure in the Transform panel: `Position = (0, 0, 0)`, `Rotation = (0, 0, 0)`, `Scale = (1.0, 1.0, 1.0)`.

### B. Unity-Standard FBX Export Preset
When exporting via `File` $\rightarrow$ `Export` $\rightarrow$ `FBX (.fbx)`:
1. **Include**:
   * ☑ **`Limit to: Selected Objects`**
   * `Object Types`: Enable only `Armature` and `Mesh` (disable Camera and Lamp).
2. **Transform**:
   * `Scale`: `1.0`
   * `Apply Scalings`: **`FBX Units Scale`** (or `FBX All`)
   * `Forward`: **`-Z Forward`**
   * `Up`: **`Y Up`**
   * ☑ **`Apply Transform`**
3. **Geometry**:
   * `Smoothing`: `Face` or `Normals Only`
   * ☑ **`Apply Modifiers`**
4. **Animation**:
   * Uncheck / Disable animation baking if exporting static meshes or props animated via DOTween.

---

## 6. Pre-Export Quality Checklist (5-Point Verification)

Before dragging any newly created or modified FBX into the Unity project, verify every item on this checklist:

- [ ] **Triangle Budget Check**: Is the total triangle count within the category limit ($< 500$ for blocks, $< 1,000$ for props)?
- [ ] **No N-Gons**: Are all faces strictly 3-sided (tris) or 4-sided (quads)?
- [ ] **UV Mapping Verified**: Are all UVs collapsed and mapped to valid color regions on `palette.png`?
- [ ] **Pivot Alignment Verified**: Is the Origin centered on the rotation axle for spinning parts, or at the bottom-center for blocks?
- [ ] **Transforms Baked**: Are Scale `(1, 1, 1)` and Rotation `(0, 0, 0)` applied cleanly?
