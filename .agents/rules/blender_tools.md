# Block Blast - Blender Tool & Addon Optimization Guidelines

This document defines performance standards, memory management, and architectural best practices for creating Blender Python addons and automation tools (`block_tools.py` and future tools) for the Block Blast project.

---

## 1. Core Performance & Data Access Hierarchy

Blender's Python API (`bpy`) has multiple ways to interact with mesh data. Choosing the wrong method leads to massive slowdowns ($10\times$ to $100\times$).

### A. The Golden Rule: Strictly Avoid `bpy.ops` in Loops
*   **The Problem**: Every `bpy.ops` call pushes an undo step, evaluates the entire dependency graph (`depsgraph`), updates the UI, and triggers viewport redraws. Calling `bpy.ops` in a loop across hundreds of faces/vertices causes severe freezing.
*   **The Rule**:
    *   **NEVER** call `bpy.ops` inside iterative loops (`for v in verts: bpy.ops.transform.translate(...)` is STRICTLY PROHIBITED).
    *   Use direct data manipulation via `bmesh` or `mesh.vertices/polygons` arrays.
    *   Reserve `bpy.ops` strictly for high-level one-shot operations (e.g., `bpy.ops.export_scene.fbx`, `bpy.ops.object.mode_set`).

### B. Access Pattern Decision Matrix

| Task / Use Case | Recommended Technique | Why / Performance |
| :--- | :--- | :--- |
| **Bulk coordinate reading/writing (10,000+ verts)** | `foreach_get` / `foreach_set` with NumPy or flat arrays | **Fastest ($100\times$)**: Direct C-memory copy, zero Python object allocation overhead. |
| **Interactive editing, Selections, UV/Color layers** | `bmesh` (`bmesh.from_edit_mesh` or `bmesh.new()`) | **High**: Fast topology navigation, clean custom layer access (`layers.float_color`, `layers.uv`). |
| **One-shot macro actions (Export, Shade Smooth)** | `bpy.ops` | Standard Blender high-level operators. |

---

## 2. Context & Mode Management (Edit Mode vs Object Mode)

Mode switching (`bpy.ops.object.mode_set`) forces Blender to commit mesh buffers, invalidate active `bmesh` pointers, and reconstruct scene data.

### A. Minimize Mode Switching
*   **Prohibited**: Do NOT toggle back and forth between `'OBJECT'` and `'EDIT'` within an operator workflow.
*   **Edit Mode Standard**:
    *   If the user is already in Edit Mode, use `bm = bmesh.from_edit_mesh(obj.data)`.
    *   Perform all face selection, group assignment, and loop color updates directly in `bm`.
    *   Call `bmesh.update_edit_mesh(obj.data)` once at the end.
    *   **Do NOT** force-switch to Object Mode unless adding/removing mesh color attributes or vertex groups that strictly require Object Mode.
*   **Headless / Background / Object Mode Standard**:
    *   Instantiate an isolated `bm = bmesh.new()`.
    *   Load via `bm.from_mesh(obj.data)`.
    *   Perform algorithmic queries and mutations.
    *   Commit via `bm.to_mesh(obj.data)` and immediately free memory via `bm.free()`.

---

## 3. Color Attributes & UV Architecture (Modern Blender Standard)

### A. `float_color` vs `color` Layer Mapping
*   In modern Blender (3.4+ / 4.x / 5.x), Color Attributes have two distinct data types:
    1.  `FLOAT_COLOR`: Used for modern linear HDR / Viewport shading. In BMesh, it lives exclusively in `bm.loops.layers.float_color`.
    2.  `BYTE_COLOR`: Legacy 8-bit sRGB vertex colors. In BMesh, it lives in `bm.loops.layers.color`.
*   **Strict Standard**:
    *   Always query both or prioritize `float_color` to prevent name collision bugs that spawn duplicate layers (`.001`, `.002`, `.003`):
        ```python
        def get_or_create_bmesh_color_layer(bm, name="BT_TIER_COLORS"):
            layer = bm.loops.layers.float_color.get(name)
            if layer is not None: return layer, 'FLOAT_COLOR'
            layer = bm.loops.layers.color.get(name)
            if layer is not None: return layer, 'BYTE_COLOR'
            return bm.loops.layers.float_color.new(name), 'FLOAT_COLOR'
        ```
    *   **Read-Only Warning**: `bm.loops.layers.color.active` is READ-ONLY in BMesh. Setting active color attribute must be done on `obj.data.color_attributes.active_color = target_ca`.

### B. Single Source of Truth & Duplicate Cleanup
*   Repeated FBX imports or layer additions create orphan duplicate layers (`BT_TIER_COLORS.001`, `.002`).
*   Always maintain a single canonical active attribute (`BT_TIER_COLORS`) and purge temporary or numbered duplicate layers during analysis/standardization.

### C. Color Space Matching (Linear vs sRGB)
*   FBX exporter automatically quantizes vertex colors into 8-bit sRGB `BYTE_COLOR`.
*   When reading back color attributes (`try_restore_tiers_from_colors`), algorithms MUST test against both Linear definitions and sRGB equivalents ($c^{2.2}$) with a robust tolerance threshold ($0.38$) to ensure $100\%$ accuracy across round-trip exports.

---

## 4. Computational Geometry & Algorithmic Optimization

### A. Minimum Area Bounding Box (2D Convex Hull)
*   **Problem**: Models imported from external packs are frequently rotated at arbitrary yaw angles (e.g., $15^\circ, 33.7^\circ, 45^\circ$). Axis-Aligned Bounding Boxes (AABB) along global X/Y get artificially inflated.
*   **Standard Algorithm**:
    1.  Project 3D vertices to 2D on the XY ground plane.
    2.  Compute 2D Convex Hull using Andrew's Monotone Chain algorithm ($O(N \log N)$).
    3.  Evaluate bounding box areas aligned with hull edges (Freeman & Shapira theorem).
    4.  Rotate the mesh by $-\theta$ to bring all 4 side walls into perfect orthogonality with world axes $X$ and $Y$.
    5.  Result: $0.00^\circ$ orientation error with $< 5\text{ms}$ execution time.

### B. Spatial Queries (KDTree / BVHTree)
*   For nearest-vertex queries or raycast snapping (e.g., snapping slats to backing), avoid $O(N \times M)$ nested loops.
*   Use Blender's built-in C-accelerated spatial structures:
    ```python
    from mathutils.kdtree import KDTree
    kd = KDTree(len(target_verts))
    for i, v in enumerate(target_verts): kd.insert(v.co, i)
    kd.balance()
    co, index, dist = kd.find(query_point)
    ```

---

## 5. UI Panel & Viewport Responsiveness

### A. Zero Heavy Computation in `draw()`
*   **Strict Rule**: Blender calls the `Panel.draw()` method on every mouse move, hover, and viewport redraw (dozens of times per second).
*   **Prohibition**: NEVER iterate over mesh vertices/faces, search islands, or calculate bounding boxes inside `draw()`.
*   **Standard**: Cache pre-computed values (e.g. `count_frame`, `count_raised`, dimensions) in `PropertyGroup` properties (`BlockToolsSettings`) during operator execution (`execute()`), and simply display the cached properties in `draw()`.

### B. Ergonomic Layout & Non-Destructive Workflows
*   Group controls into collapsible sub-boxes (`r.prop(settings, "fold_...", toggle=True)`).
*   Store baseline mesh geometry in non-destructive custom vertex layers (`BT_ORIGINAL_CO`, `BT_ORIGINAL_UV`) so users can reset or tweak procedural adjustments without permanently degrading the asset.

---

## 6. Registration & State Safety

### A. Clean Unregistration
*   Always unregister classes in `reversed(classes)` order to avoid dependency teardown errors.
*   Cleanly delete registered `bpy.types.Scene.<prop_name>` attributes in `unregister()`.

### B. Undo & Transaction Safety
*   All user-facing operators modifying scene or mesh state must declare `bl_options = {'REGISTER', 'UNDO'}`.
*   Wrap external file I/O or FBX operations in `try ... except` blocks with informative user feedback via `self.report({'ERROR'}, message)` instead of letting Python crash into the system console.

---

## 7. Mandatory Logic Sweep & Dead Code Elimination (Legacy Pruning)

Whenever developing a new feature or modifying an existing feature in Blender tools/addons:

*   **Comprehensive Logic Sweep**:
    *   You MUST scan the entire file/module and trace call paths to identify all outdated, bypassed, or superseded logic.
    *   Do NOT leave legacy fallback code paths or obsolete algorithms behind when introducing a modern, superior approach.
*   **Zero Dead Code Tolerance**:
    *   Instantly delete unused helper functions, obsolete operators, orphaned data structures, and unused dictionary lookups.
    *   Remove dead variables, unneeded loop iterations, and redundant state caches.
    *   Eliminate commented-out code blocks and stale comments that no longer match active implementation.
*   **Lean & Cohesive Architecture**:
    *   Keep the script consolidated, atomic, and clean. Every function, operator, and line of code must serve an active, verified purpose.
