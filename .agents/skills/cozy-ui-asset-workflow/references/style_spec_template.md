# UI Style Specification & Component Catalog Template

Template to document visual tokens, prompt contracts, and component inventories for a specific UI asset pack.

---

## 1. Pack Metadata & Strategy

- **Pack Name**: `[e.g., CozyStationeryUI]`
- **Target Visual Theme**: `[e.g., Cozy craft paper, warm pastel stationery, rounded tactile wooden blocks]`
- **Atlas Layout Strategy**: `[Single Master Sheet | Multi-sheet by Category]`
- **Target Sheets**: `[e.g., UI_Common, UI_Gameplay, UI_Modals]`
- **Base Grid Cell Size**: `[e.g., 64x64 px]`
- **Default Padding**: `[e.g., 4px]`
- **Approval State**: `[Draft | Pending Review | Approved]`

---

## 2. Style Tokens

### 2.1 Color Palette (RGBA)

| Token Name | Hex Code / RGBA | Role / Application | Example Component |
| --- | --- | --- | --- |
| `color.bg.cream` | `#FBF7EEFF` | Neutral card/panel background fill | Panel card, speech bubble |
| `color.bg.dark` | `#2D3238FF` | High-contrast header/dark container fill | Score board, dark modal |
| `color.primary.action` | `#4CAF50FF` | Primary action button fill | Play button, confirm button |
| `color.secondary.action` | `#42A5F5FF` | Secondary navigation action fill | Shop button, options button |
| `color.danger.action` | `#EF5350FF` | Destructive/warning action fill | Quit button, reset button |
| `color.accent.gold` | `#FFC107FF` | Rewards, achievements, stars | Star badge, reward ribbon |
| `color.stroke.dark` | `#3E2723FF` | Outer crisp outline border | All component boundaries |
| `color.shadow.bottom` | `#00000033` | Drop shadow and depth shelf | Button bottom shadow |

### 2.2 Stroke, Borders & Geometry

- **Outer Stroke Width**: `[e.g., 4px solid continuous]`
- **Inner Border Inset**: `[e.g., 2px lighter bevel]`
- **Corner Radiuses**:
  - Small Widgets (Badges, Pills, Buttons): `[e.g., 12px]`
  - Medium Containers (Cards, Counters): `[e.g., 20px]`
  - Large Windows (Modals, Dialogs): `[e.g., 28px]`
- **Circle Widgets**: `[Strict 1:1 aspect ratio with centered anchor]`

### 2.3 Depth, Lighting & Shadows

- **Light Source**: `[Top-down at 90 degrees with subtle 5 degree left bias]`
- **Button Depth Shelf**: `[e.g., 8px solid darker bottom layer for 3D tactile push feel]`
- **Pressed State Compression**: `[Shelf height reduced from 8px to 2px, face translated 6px down]`
- **Highlight Treatment**: `[1px subtle lighter horizontal rim on top inner edge]`

### 2.4 Surface Texture & Finish

- **Material Quality**: `[Matte parchment paper / lacquered wood / soft clay]`
- **Noise / Grain**: `[Subtle <2% procedural grain, no heavy photographic noise]`
- **Gradient Policy**: `[Subtle top-to-bottom vertical gradients only; strictly NO diagonal or radial gradients across 9-slice expansion zones]`

---

## 3. Prompt Contract

Standardized prompt templates applied to all generation calls to maintain style uniformity.

### 3.1 Base Positive Prefix
```plaintext
2D video game UI asset, [THEME_NAME] style, [MATERIAL_STYLE], clean crisp dark outline of [STROKE_WIDTH]px, [LIGHTING_STYLE], [COLOR_TONE_KEYWORDS], [FINISH_STYLE], isolated game asset, centered on clean transparent background, high resolution 32-bit RGBA, no background scenery.
```
*(Note: When calling image generation tools, ensure `transparent_background=true` is set).*

### 3.2 Subject Geometry Template
```plaintext
Subject: [COMPONENT_NAME], [COMPONENT_TYPE], dimensions [WIDTH]x[HEIGHT] pixels. [SPECIFIC_DETAILS_AND_ICONS]. Uniform rounded corners of [CORNER_RADIUS]px radius. Completely plain center area with no text, no numbers, no words, no letters, no logos.
```

### 3.3 Base Negative Suffix
```plaintext
text, typography, letters, words, numbers, labels, watermark, logo, blurry edges, fuzzy borders, noisy background, photographic realistic clutter, drop shadow bleeding off canvas, non-transparent background, artifacts, 3D perspective tilt, angled view, isometric view, gradient banding.
```

---

## 4. Component Catalog & Manifest Table

| Component ID | Category | Dimensions (WxH) | Named 9-Slice Insets `{left, bottom, right, top}` | State Family | Target Sheet |
| --- | --- | --- | --- | --- | --- |
| `btn_primary_normal` | Buttons | 240x80 | `{"left": 24, "bottom": 28, "right": 24, "top": 20}` | `btn_primary` | `UI_Common` |
| `btn_primary_pressed` | Buttons | 240x80 | `{"left": 24, "bottom": 22, "right": 24, "top": 20}` | `btn_primary` | `UI_Common` |
| `btn_primary_disabled`| Buttons | 240x80 | `{"left": 24, "bottom": 28, "right": 24, "top": 20}` | `btn_primary` | `UI_Common` |
| `btn_circle_shop` | Buttons | 96x96 | `None` (Fixed) | `btn_circle` | `UI_Common` |
| `card_container_cream`| Containers | 320x240 | `{"left": 32, "bottom": 32, "right": 32, "top": 32}` | `card_cream` | `UI_Common` |
| `counter_pill_coin` | Counters | 180x64 | `{"left": 40, "bottom": 16, "right": 24, "top": 16}` | `counter_pill`| `UI_Gameplay`|
| `modal_frame_dialog` | Modals | 480x360 | `{"left": 48, "bottom": 48, "right": 48, "top": 48}` | `modal_dialog`| `UI_Modals` |
| `toggle_switch_base` | Controls | 120x64 | `{"left": 32, "bottom": 16, "right": 32, "top": 16}` | `toggle_switch`| `UI_Common` |
| `toggle_switch_knob` | Controls | 56x56 | `None` (Fixed) | `toggle_switch`| `UI_Common` |

---

## 5. Approval Sign-off

- **Reviewed by Agent/Chat**: `[Chat Name]`
- **Approved by User**: `[Pending / Yes / No]`
- **Approval Date**: `[YYYY-MM-DD]`
- **Notes / Constraints**: `[e.g., Approved with condition that button height is strictly 80px]`
