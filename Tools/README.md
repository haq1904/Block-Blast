# Block Blast Project Development Tools

This directory contains external automation, asset generation, and project maintenance scripts.

## Tools Directory Structure

- `Tools/generate_loading_cloud_sprites.ps1`: Generates the 2D cartoon cloud transition edge sprites for the loading screen.
- `Tools/Antigravity/`: Antigravity workflow runners and automation menus.
- `Tools/Audio/`: Audio processing and SFX generation tools.
- `Tools/Blender/`: Blender 3D modeling scripts, add-ons, and prop automation.
- `Tools/UIAssets/`: Deterministic UI component validation and multi-sheet spritesheet assembly tools.

---

## `generate_loading_cloud_sprites.ps1`

### Purpose
Procedurally renders high-definition (`2048 x 512`), antialiased 2D cartoon cloud edge sprites used by the loading transition UI:
1. `Assets/Art/UI/Loading/Clouds/loading_cloud_top_edge.png`
2. `Assets/Art/UI/Loading/Clouds/loading_cloud_bottom_edge.png`

### Prerequisites
- Windows PowerShell with standard .NET runtime (`System.Drawing`).
- No external packages or Python dependencies required.

### Invocation from Project Root

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\generate_loading_cloud_sprites.ps1
```

To also generate multi-resolution composite verification images:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\generate_loading_cloud_sprites.ps1 -Preview
```

### Parameters
- `-Preview`: Generates QA composite test images simulating 4 screen aspect ratios (`1080x1920`, `1080x2340`, `1536x2048`, `1920x1080`) in the temporary scratch directory.
- `-OutputDir <path>`: Destination directory for the generated cloud sprites (default: `Assets/Art/UI/Loading/Clouds`).
- `-PreviewDir <path>`: Destination directory for QA previews.

### Output Assets
- `Assets/Art/UI/Loading/Clouds/loading_cloud_top_edge.png`
- `Assets/Art/UI/Loading/Clouds/loading_cloud_bottom_edge.png`

### Verification Example
Run with `-Preview` and verify output file creation and dimensions:

```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\generate_loading_cloud_sprites.ps1 -Preview
Test-Path Assets/Art/UI/Loading/Clouds/loading_cloud_top_edge.png
Test-Path Assets/Art/UI/Loading/Clouds/loading_cloud_bottom_edge.png
```
