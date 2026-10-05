---
trigger: always_on
description: Mandatory optimization and validation for every asset created, duplicated, generated, or imported by agents.
---

# Block Blast - Asset Optimization Requirements

## 1. Mandatory Scope

Every asset an agent creates, duplicates, generates, or imports into this project MUST be reviewed and optimized for its intended use before the task is reported as complete. This includes meshes/models, textures/sprites, materials/shaders, prefabs, VFX, audio, animations, UI/fonts, scenes, ScriptableObjects/data, and accompanying scripts/tools.

Read this document before creating assets. Apply relevant specialized rules alongside it: [modeling](modeling.md), [shaders](shaders.md), [DOTween](dotween.md), [visual effects](visual_effects.md), [architecture](architecture.md), [Blender tools](blender_tools.md), and [workflow standards](standards.md).

Optimization MUST preserve the requested behavior, appearance, and audio quality. Do not simplify assets blindly, invent universal limits, or claim performance improvements without evidence. Do not optimize unrelated existing assets or shared project settings as a side effect of the task.

## 2. Required Workflow

1. **Before creation:** inspect existing assets for suitable reuse. Identify the asset type, intended platform, visible size/camera use, and expected number of simultaneous instances. If platform or usage is unknown, state the working assumption.
2. **Set appropriate budgets:** use existing project limits, including category-specific triangle budgets in `modeling.md`. If no applicable budget exists, document a proposed budget and its reasoning. Do not apply one triangle count, texture size, compression format, or particle count to every asset.
3. **Optimize source and import settings:** remove unnecessary data/components from the new asset while preserving required behavior, references, animation, shading, and visibility. Configure platform-specific import settings only within the authorized task scope.
4. **Validate the imported result:** inspect references and relevant import settings, then verify representative use. For frequently spawned objects or expensive effects, include expected concurrent instances. Scale verification effort to asset cost and risk; do not require a full benchmark for a trivial data asset.
5. **Report evidence:** briefly state the optimization performed, checks completed, measured results when available, and remaining verification limits. Mark relevant criteria as addressed or not applicable with a reason when useful. Missing tools, Editor access, or a target device must be reported; do not present unavailable validation as passed.

## 3. Requirements by Asset Type

| Asset type | Required review and optimization where applicable |
| --- | --- |
| Mesh/model | Follow existing category triangle limits; remove unnecessary geometry and material slots; verify UVs, pivots, normals, colliders, and unused imported animation/data. Preserve faces needed for animation, shadows, or other supported camera angles. |
| Texture/sprite | Match resolution to visible size and quality requirements; select suitable compression and platform overrides; enable alpha, mipmaps, and Read/Write only when needed; consider atlases for sprites commonly used together. |
| Material/shader | Reuse suitable materials/shaders; remove unnecessary passes and computation; avoid runtime material clones; follow `shaders.md`. Sharing a shader/material does not prove reduced draw calls or a target FPS. Measure relevant rendering behavior when needed. |
| Prefab/VFX | Remove unnecessary components/renderers/colliders; bound particle count and lifetime; review transparent overdraw, shadows, and lights; use reuse/pooling for frequently spawned objects when beneficial, with correct cleanup and state reset. |
| Audio | Choose mono/stereo, compression, sample rate, and load mode according to use. Consider latency, memory, and CPU tradeoffs; bound simultaneous playback where needed. |
| Animation/UI/font | Remove unnecessary tracks/keyframes without changing intended motion; follow tween lifecycle rules; avoid unnecessary layout rebuilds, raycast targets, and font glyph/atlas data. Choose Canvas/font configuration for actual usage. |
| Scene | Keep necessary objects/components and review lights, shadows, post-processing, Canvas layout, and dependencies against supported camera views and representative load. |
| ScriptableObject/data | Reuse suitable data; avoid unnecessary copies and dependencies; preserve valid references and separate configuration from runtime state according to existing rules. |
| Script/tool | Avoid unnecessary repeated work and allocations in frequent execution paths; cache/reuse where beneficial and release resources correctly. Follow architecture and Blender tool rules as applicable. |

Use only techniques that benefit the specific asset. Do not apply pooling, atlasing, mesh reduction, compression, or other techniques mechanically when they increase cost or compromise requested quality.

## 4. Shared Project and Reference Safety

- Work only on assets and dependencies assigned to the current task/chat.
- Coordinate before changing shared atlases, materials, prefabs, project settings, or import presets owned or used by another chat.
- Preserve existing GUIDs and references. Never delete `.meta` files to regenerate GUIDs as an optimization step.
- Control Unity through MCP only during the current chat's assigned Editor turn. Do not interrupt another chat's scene, compilation, or PlayMode work.
- Follow `standards.md` for validation: visual-only changes use appropriate inspection, compiler/Console checks when applicable, and visual verification; automated unit tests are reserved for Model/Controller or core logic changes.
- Verify nonvisual assets with the appropriate data/import/tool checks. Do not force PlayMode or a benchmark for assets that do not require them.

## 5. Completion and Performance Claims

Before reporting completion, review the applicable criteria and report any unmet budget, quality issue, or verification gap. If an applicable hard limit cannot be met without changing the requested result, explain the tradeoff and obtain a decision before exceeding it.

Distinguish configured settings, static inspection, Editor observations, and measurements on a target device. Report the measurement context for performance results, including representative load and device/platform when available. Never promise a specific FPS, draw-call count, or memory reduction solely because an optimization technique was applied.
