# Block Blast - Shader & Material Optimization Guidelines

Performance and memory efficiency are paramount, especially on mobile devices. Mismanagement of shaders and materials can cause severe memory leaks, garbage collection (GC) spikes, and broken draw call batching (SRP Batcher / GPU Instancing). You MUST strictly adhere to the following rules:

---

## 1. Strict Prohibition of Material Cloning (`renderer.material` Ban)
*   **Prohibition**: Accessing `renderer.material` in runtime code (e.g., animations, color changes, hover highlights, tweens, visual feedback) is **STRICTLY PROHIBITED**.
    ```csharp
    // STRICTLY PROHIBITED - Instantiates a leaked Material copy in RAM:
    renderer.material.color = Color.white;
    renderer.material.SetColor("_EmissionColor", Color.yellow);
    ```
*   **Rationale**: Calling `renderer.material` creates an instantiated clone `Material (Instance)` in memory. These copies are never automatically garbage collected, resulting in progressive memory leaks and permanently breaking URP SRP Batching.
*   **Mandatory Standard**: ALWAYS use **`MaterialPropertyBlock`** to alter material properties at runtime without cloning.

---

## 2. MaterialPropertyBlock (MPB) Standard Workflow

### A. Static Shader ID Caching
*   Never use string property names inside update loops or tweens. Always cache `Shader.PropertyToID`:
    ```csharp
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
    ```

### B. Shared / Reusable MPB Allocation
*   Do NOT instantiate `new MaterialPropertyBlock()` every frame or within high-frequency callbacks.
*   Reuse a single cached or static instance:
    ```csharp
    private static MaterialPropertyBlock mpbCache;
    private static MaterialPropertyBlock MPB => mpbCache ??= new MaterialPropertyBlock();
    ```

### C. Standard Application Pattern
```csharp
MPB.Clear();
renderer.GetPropertyBlock(MPB);
MPB.SetColor(EmissionColorId, emissionHdrColor);
MPB.SetColor(BaseColorId, tintColor);
renderer.SetPropertyBlock(MPB);
```

### D. Mandatory Cleanup for Object Pooling
*   When a GameObject or Transform is returned to the Object Pool (`IPoolService.ReturnObjectToPool`), disabled, or its animation is cancelled:
    1. Reset the `MaterialPropertyBlock` back to clean state:
       ```csharp
       renderer.SetPropertyBlock(null);
       ```
    2. Never leave an active property block with overrides lingering on a recycled or disabled object.

---

## 3. SRP Batcher & GPU Instancing Compatibility

### A. URP SRP Batcher Compatibility
*   Any custom shader (Shader Graph or HLSL) written for the project MUST be 100% compatible with the **URP SRP Batcher**:
    *   All material properties must be declared inside a `UnityPerMaterial` constant buffer (`CBUFFER_START(UnityPerMaterial) ... CBUFFER_END`).
    *   Avoid using non-SRP-batchable passes or custom global properties that break the batch.

### B. Runtime Keyword Ban
*   Do NOT toggle shader keywords dynamically at runtime (`material.EnableKeyword` / `material.DisableKeyword` or `Shader.EnableKeyword`).
*   Toggling keywords forces shader variant recompilation/switching, causes GPU pipeline stalls, and breaks batching across instances.
*   Use numerical floats/sliders (e.g., `_FlashAmount: 0..1`, `_EmissionIntensity: 0..N`) to modulate shader features smoothly rather than binary keyword branching.

---

## 4. Master Shader Architecture

*   **Avoid Micro-Shader Proliferation**: Do not create dozens of fragmented, one-off shaders for individual block types or subtle visual tweaks.
*   **Unified Master Shader**: Utilize a unified "Master Shader" for gameplay blocks and board elements with standard channels:
    *   `_BaseMap` / `_BaseColor`: Texture atlas or tint color.
    *   `_EmissionColor` / `_EmissionIntensity`: Glow, pulses, and electrical overload feedback.
    *   `_FlashColor` / `_FlashAmount`: Instantaneous hit or clear flash feedback.
    *   `_RimColor` / `_RimIntensity`: Aerodynamic / outline fresnel glow.
    *   `_DissolveAmount` / `_DissolveMap`: Dissolve/twist disintegrations.
*   By sharing a single Master Shader across all blocks, the entire 8x8 grid can be rendered within a **single SRP Batch**, yielding maximum 60/120 FPS performance on mobile hardware.

---

## 5. Texture Atlasing & Mobile Overdraw Guard

*   **Texture Atlasing**: Whenever possible, block models should map their UVs to unified color palettes or atlases (e.g., `palette.png`) to maximize draw call batching and minimize texture memory bandwidth.
*   **Debris Mesh Alignment**: Specialized 3D debris meshes (`Mesh_MicroDebris_*`) must use dedicated debris materials matching their specific UV layouts—never force-map full block atlas materials onto unmatched fragment geometries.
*   **Transparent Overdraw Limit**: Keep particle system transparent quads bounded and trimmed. Avoid massive overlapping fullscreen particle clouds that cause fillrate bottlenecking on mobile GPUs.
