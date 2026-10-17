# UI Asset Quality Assurance (QA) Checklist

Rigorous verification criteria to evaluate individual UI component bitmaps and assembled spritesheets before integration into Unity, distinguishing automated tool checks from manual visual inspections.

---

## 1. Visual & Style Fidelity [Manual / Visual Review]

- [ ] **Side-by-Side Scale Comparison**:
  - Inspected at **100% native resolution** on a high-DPI display: borders, inner bevels, and highlights are sharp and unblurred.
  - Inspected at **50% mobile device preview scale**: icons and text container boundaries remain legible and do not turn muddy.
- [ ] **Family Consistency**:
  - Stroke width is identical across all components in the same family (e.g., all buttons have 4px outer strokes).
  - Corner radiuses are consistent across similar scales (small buttons share 12px, cards share 20px).
  - Lighting direction is uniform across the entire pack (e.g., top-down with consistent shadow drop).
  - Color palette matches the defined style spec tokens exactly (no stray hue variations).
- [ ] **Originality & Content Neutrality**:
  - **Zero Baked Text**: Containers, cards, and buttons contain no baked-in labels, numbers, mock text, or letters.
  - **Zero Watermarks / Signatures**: No AI generator artifacts, logos, or signatures.
  - **Original Artworks**: Shapes and motifs are original game assets, free of third-party IP or trademarked logos (requires human review).

---

## 2. Alpha Transparency & Edge Hygiene [Automated + Visual]

- [ ] **Alpha Background Integrity [Automated via `validate_ui_assets.py`]**:
  - The image format is strictly 32-bit RGBA PNG.
  - Areas outside the component boundary have alpha value `0` (pure transparent).
  - For components requiring transparency, alpha channel contains both `0` and `>0` pixels (not completely opaque, not empty).
- [ ] **Fringe & Halo Inspection [Manual / Visual Review]**:
  - Component placed against a **pure white (`#FFFFFF`) background**: no dark, dirty, or ragged edge halos.
  - Component placed against a **pure dark (`#1A1A1A`) background**: no light/white matte bleed or semi-transparent fringe artifacts.
  - Anti-aliasing along the outer stroke is smooth and tapers cleanly into full transparency within 1-2 pixels.

---

## 3. Geometry, Centering & 9-Slice Slicing [Automated + Visual]

- [ ] **Bounding Box & Padding [Automated via `validate_ui_assets.py`]**:
  - Actual non-transparent pixels fit within the declared manifest dimensions (`width x height`).
  - Bounding box has appropriate transparent padding (default >= 2-4px) to prevent texture bleeding and mipmap edge leakage.
- [ ] **Optical Center & Pivots [Automated via `validate_ui_assets.py`]**:
  - Symmetrical widgets have their optical center within `center_tolerance_px` of `expected_center` (default `<= 2.5px`).
- [ ] **9-Slice Expansion Test [Automated Semantic Check + Visual Canvas Test]**:
  - Border insets `{left, bottom, right, top}` satisfy `left + right < width` and `bottom + top < height`.
  - Slicing test in horizontal expansion: vertical edges stretch or tile without distorting corner radiuses.
  - Slicing test in vertical expansion: horizontal edges stretch or tile without distorting corner radiuses.
  - Center fill region contains no diagonal lines, complex gradients, or focal patterns that break when stretched.

---

## 4. Spritesheet & Technical Texture Budget [Automated via `validate_ui_assets.py --sheet-root`]

- [ ] **Deterministic Sheet Validation**:
  - Sheet dimensions conform to declared dimensions (and power-of-two if `require_power_of_two: true`).
  - Maximum sheet size does not exceed declared `max_size` (if specified in manifest) for mobile memory optimization.
  - Components do not overlap each other within the sheet grid.
  - Two consecutive runs of `assemble_ui_sheets.py` produce identical pixel data and SHA256 hashes within the same recorded runtime and dependency environment.
- [ ] **Texture Compression Budget [Visual / Frame Debugger]**:
  - Sheet is designed to compress cleanly with ASTC 6x6 or Crunch compression without severe color banding on pastel gradients.

---

## 5. Unity Import Settings Sign-Off [Manual / Editor Turn]

When importing into Unity:
- [ ] `Texture Type` set to `Sprite (2D and UI)`.
- [ ] `Sprite Mode` set to `Multiple` for sheets, `Single` for standalone full-screen backgrounds.
- [ ] `Alpha Is Transparency` checked `True`.
- [ ] `Mesh Type` set to `Full Rect` to avoid unnecessary polygon calculation overhead for UI.
- [ ] `Generate Physics Shape` unchecked `False` (UI sprites do not require 2D colliders).
- [ ] `Wrap Mode` set to `Clamp`.
- [ ] 9-slice borders configured in Sprite Editor match manifest `nine_slice` `{left, bottom, right, top}` values.
