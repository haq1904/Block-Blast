# Detailed UI Asset Generation Workflow

Comprehensive operational guide for transforming visual references into modular 2D UI bitmap asset packs and spritesheets for Unity.

---

## 1. Phase 1: Intake & Reference Audit

### 1.1 Reference Inventory
Scan the designated reference directory (`Assets/DesignReference/...` or user-specified folder). Record every file in an inventory table:
- **File Name**: Relative path.
- **Format**: PNG, JPG, or GIF.
- **Resolution**: Width x Height in pixels.
- **Primary Role**: Style, Geometry, Component Coverage, Motion/Modularity, or Negative.

### 1.2 Inspecting Animated GIFs
When GIF references are present:
1. Extract representative keyframes (e.g., initial state, peak action, settle state) to inspect UI transitions.
2. Determine whether the animation represents:
   - **Procedural / Code Animation**: Scale punches, bounce tweens, or fade transitions that DOTween handles at runtime (only static base sprites needed).
   - **Multi-state Modularity**: Toggle switches, checkbox states, or tab selection indicators (separate static state sprites needed).
   - **Frame-by-frame Effect**: Particle bursts or loading spinners (frame sequence needed).
3. **Rule**: Never turn entire UI screen GIFs into full-screen flipbook textures. Decompose them into individual modular widgets.

### 1.3 Visual Fingerprinting
Analyze the reference images to extract core visual tokens:
- **Color Palette**: Dominant background tints, panel fills, primary/secondary action colors, border strokes, and highlight/shadow tones in RGBA hex format.
- **Stroke & Border Grammar**: Outer stroke thickness (e.g., 3-4px), corner radius (e.g., 16px soft round vs 4px crisp), inner bevel or border insets.
- **Depth & Lighting**: Top-down directional light, bottom-edge drop shadow offset (e.g., +4px Y offset), subtle top-edge inner highlight.
- **Surface Texture**: Matte paper, parchment texture, wood grain, frosted glass, or clean flat graphic styling.

### 1.4 Reuse Audit & Component Taxonomy
Categorize needed components to prevent asset bloat:
- **9-Sliceable Containers**: Cards, dialog panels, tooltip speech bubbles, progress bar tracks. Design them with expandable center zones to avoid baking multiple fixed sizes.
- **Fixed-Aspect Widgets**: Circle buttons, square icon buttons, counter icons, star ratings, toggle knobs.
- **Derived States**: Determine which states can be achieved via runtime color tint or transform vs those requiring dedicated sprites (e.g., pressed down-state with compressed shadow).

---

## 2. Gate 1: Sheet Layout Strategy Decision

Evaluate whether the UI pack should use a single atlas or category-specific sheets:

1. **Single Master Sheet**:
   - Suitable when total UI components fit comfortably within a single 2048x2048 texture atlas (or 1024x1024 for lightweight casual games).
   - May reduce draw calls when multiple UI types appear on screen simultaneously (verify with Unity Frame Debugger).
2. **Multi-Sheet by Category**:
   - Suitable when components exceed 2048x2048 or belong to distinct scenes/flows (e.g., `UI_Common`, `UI_Gameplay`, `UI_Modals`, `UI_Store`).
   - May allow unloading memory when players leave specific menus, subject to asset bundle/scene lifecycle verification in Unity Profiler.
3. **Execution Rule**: If an existing implementation plan or design spec has already approved the strategy, proceed directly. Only request decision if unconstrained.

---

## 3. Phase 2: Style Spec & Component Manifest

### 3.1 Drafting the Style Spec
Fill in `references/style_spec_template.md` with:
- Formal token names (`color.panel.fill`, `color.button.primary`, `stroke.outer.width`).
- Standard positive prompt prefixes: Style descriptors, lighting rules, rendering engine instructions.
- Standard negative prompt suffixes: Artifacts, gradients that break 9-slice, low contrast, text labels.
- Component catalog with intended dimensions and target sheet IDs.

### 3.2 Defining the Component Manifest (`manifest.json`)
Construct the JSON manifest strictly adhering to `references/component_manifest.schema.json`:
- **version**: `"1.0.0"`.
- **sheets**: Dictionary of sheet definitions (`dimensions`, `grid.cell_size`, `grid.columns`, `grid.rows`, `padding`).
- **components**: Array of component objects specifying `id`, `sheet_id`, `category`, `source_file`, `placement` (row, col, col_span, row_span), `dimensions` (width, height), `padding`, `pivot`, and named `nine_slice` borders (`{"left", "bottom", "right", "top"}`).

---

## 4. Gate 2: Spec & Manifest Approval

Present the completed Style Spec and Component Manifest to the user:
- Highlight key design tokens and component inventory count.
- Point to the manifest file for layout inspection.
- **STOP** and wait for user approval before issuing image generation calls.

---

## 5. Phase 3: Golden Anchors Loop

Before generating the entire pack, establish 2-3 **Golden Anchors** (e.g., Primary Action Button, Medium Card Container, Currency Counter Pill):

1. **Prompt Construction**:
   Combine the base style prompt, component-specific geometry prompt, and negative constraints. Include the actual visual reference image in the tool/subagent call.
2. **Generation**:
   Invoke `imagegen` (or subagent with visual generation capability) to produce RGBA PNG assets with transparent backgrounds.
3. **Visual Quality Review**:
   - Inspect anchor PNGs at 100% native resolution and 50% device scale.
   - Verify that outer borders are crisp, inner fills have intended texture without noise, and shadows do not bleed into transparent padding.
4. **Iterate**:
   Refine prompt tokens or mask controls until the anchors seamlessly match reference styling.
5. **Lock Anchors**:
   Record prompt templates used for the golden anchors as the benchmark for subsequent batches.

---

## 6. Phase 4: Bulk Generation by Family

Execute generation in sequential batches grouped strictly by component family:

```
Batch 1: Buttons (Primary, Secondary, Danger, Icon Circle, Icon Square)
   │
Batch 2: Cards & Containers (Dark, Light, Cream, Header Panel, Footer Bar)
   │
Batch 3: Badges, Pills & Counters (Status Badges, Coin/Gem/Heart Pills)
   │
Batch 4: Controls & Modals (Toggles, Sliders, Checkboxes, Dialog Frames)
   │
Batch 5: Derived Interactive States (Pressed, Hover, Disabled, Selected)
```

### Prompt Consistency Rules
- Keep the anchor's prefix and suffix identical across all prompts in a family.
- Only adjust the subject geometry, dimensions, and inner accent features.
- For interactive states (e.g., `button_primary_pressed`):
  - Maintain identical shape and outline.
  - Apply physical down-shift (e.g., shadow height reduced from 8px to 2px, button face shifted down by 4px).
  - Slightly increase saturation or lower brightness to simulate tactile depression.

---

## 7. Phase 5: Technical Validation & Deterministic Assembly

### 7.1 Asset & Manifest Validation
Run the validator tool against the raw component PNGs:
```powershell
python Tools/UIAssets/validate_ui_assets.py --manifest path/to/manifest.json --asset-root path/to/raw_pngs/ --report path/to/report.json
```
Verify that:
- Every declared component file exists on disk.
- PNGs are strictly in `RGBA` format (32-bit depth with alpha transparency).
- Component content fits within declared dimensions plus padding tolerance.
- Optical center is within specified tolerance (default <= 2px).
- Manifest grid placements do not exceed sheet bounds or overlap adjacent cells.

### 7.2 Deterministic Spritesheet Assembly
Run the sheet assembler tool:
```powershell
python Tools/UIAssets/assemble_ui_sheets.py --manifest path/to/manifest.json --asset-root path/to/raw_pngs/ --output-root path/to/output_sheets/ --overwrite
```
The assembler:
- Creates a transparent RGBA canvas matching sheet dimensions.
- Places each individual PNG at the exact grid row/column pixel coordinate defined in the manifest.
- Guarantees pixel-identical and SHA256-identical reproducible outputs within the same recorded runtime and dependency environment.
- Rejects any output write if source files are missing, invalid, or would cause overlap.

### 7.3 Sheet Re-Validation
Run the validator on the assembled sheets to confirm dimensions, power-of-two boundaries, and alpha hygiene.

---

## 8. Phase 6: Unity Import & Handoff

Only perform this phase if authorized by the user and the chat holds the Unity Editor turn:

1. **Copy Assets to Unity Directory**:
   Place assembled sheets and sprite atlases under `Assets/Art/UI/<PackName>/`.
2. **Apply Texture Import Settings**:
   - `Texture Type`: `Sprite (2D and UI)`
   - `Sprite Mode`: `Multiple`
   - `Pixels Per Unit (PPU)`: e.g., `100` (or project standard)
   - `Mesh Type`: `Full Rect`
   - `Alpha Is Transparency`: `True`
   - `Generate Physics Shape`: `False`
   - `Wrap Mode`: `Clamp`
   - `Filter Mode`: `Bilinear`
3. **Configure Sprite Slicing & 9-Slice Borders**:
   In Unity Sprite Editor (or via automated import script using manifest metadata):
   - Slice sprites using exact rect coordinates from the manifest.
   - Set 9-slice border insets (`L`, `B`, `R`, `T`) matching the manifest `nine_slice` `{left, bottom, right, top}` fields.
4. **Final Verification**:
   - Test 9-slice scaling in a sample UI Canvas: stretch horizontally and vertically to ensure corners do not distort and center repeats cleanly.
   - Capture side-by-side validation screenshot comparing runtime UI against initial reference.
