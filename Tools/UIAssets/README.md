# UI Assets Automation Tools

Deterministic Python tools for validating UI component PNGs against Draft 2020-12 JSON Schema & semantic rules, and assembling them into multi-sheet spritesheets for Unity.

---

## 1. Requirements & Dependencies

- **Python**: Version 3.10 or later.
- **Windows environment manager**: [`uv`](https://docs.astral.sh/uv/) is required by the PowerShell recipe so MSYS2 installations do not fall back to unsupported source builds.
- **Dependencies**: Listed in `Tools/UIAssets/requirements.txt`:
  ```txt
  Pillow>=10.0.0,<13.0.0
  jsonschema>=4.20.0,<5.0.0
  PyYAML>=6.0.0,<7.0.0
  ```

### Temporary Virtual Environment Setup
To avoid modifying your global Python environment and prevent creating virtual environments or bytecode inside the repository, create and use a temporary virtual environment located in the operating system's temp directory.

#### PowerShell (Windows)
```powershell
$uiAssetsTempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("ui_assets_venv_" + [Guid]::NewGuid().ToString("N"))
$uiAssetsVenv = Join-Path $uiAssetsTempRoot "venv"
uv venv --no-project --managed-python --python 3.12 $uiAssetsVenv

# Keep both layouts supported when uv's selected interpreter varies.
$uiAssetsPython = @(
    (Join-Path $uiAssetsVenv "Scripts\python.exe")
    (Join-Path $uiAssetsVenv "bin\python.exe")
) | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1

if (-not $uiAssetsPython) {
    throw "Could not locate the temporary virtual environment Python executable."
}

$env:PYTHONDONTWRITEBYTECODE = "1"
uv pip install --python $uiAssetsPython -r Tools/UIAssets/requirements.txt

# Run tools directly using the temporary python binary:
& $uiAssetsPython Tools/UIAssets/validate_ui_assets.py --help
& $uiAssetsPython Tools/UIAssets/assemble_ui_sheets.py --help
```

#### Bash (macOS / Linux)
```bash
ui_assets_temp_root=$(mktemp -d)
python3 -m venv "$ui_assets_temp_root/venv"
"$ui_assets_temp_root/venv/bin/python" -m pip install -r Tools/UIAssets/requirements.txt

# Run tools directly using the temporary python binary:
"$ui_assets_temp_root/venv/bin/python" Tools/UIAssets/validate_ui_assets.py --help
"$ui_assets_temp_root/venv/bin/python" Tools/UIAssets/assemble_ui_sheets.py --help
```

*(Note: The temporary virtual environment resides outside the project workspace and must never be committed to source control).*

---

## 2. Tools Overview

| Tool | Purpose | Key Guarantees |
| --- | --- | --- |
| `validate_ui_assets.py` | Validates Draft 2020-12 schema, grid equations, component dimensions, RGBA mode, transparency, optical center, named 9-slice, and assembled sheets. | Non-destructive, read-only. Generates structured JSON report. Exposes pure functions for import. |
| `assemble_ui_sheets.py` | Composites individual component PNGs onto multi-sheet transparent canvases using grid placements from the manifest. | Strict preflight validation, alpha-safe Porter-Duff compositing, atomic staging with rollback, overwrite protection. |

---

## 3. Schema & Contracts

- **Manifest Schema**: Located at `.agents/skills/cozy-ui-asset-workflow/references/component_manifest.schema.json`.
- **9-Slice Insets**: Defined as a named object `{"left": int, "bottom": int, "right": int, "top": int}`.
- **Grid Equation**: `sheet_width = 2 * padding + columns * cell_width` (and height equivalent). Padding acts as the grid origin.

---

## 4. Usage & CLI Contracts

### 4.1 `validate_ui_assets.py`

Validates manifest structure, individual raw PNGs, and optionally assembled spritesheets:

```bash
python Tools/UIAssets/validate_ui_assets.py \
  --manifest path/to/manifest.json \
  --asset-root path/to/raw_pngs/ \
  [--sheet-root path/to/assembled_sheets/] \
  [--schema path/to/component_manifest.schema.json] \
  [--report path/to/validation_report.json]
```

**Options**:
- `--manifest` *(required)*: Path to the component manifest JSON.
- `--asset-root` *(required)*: Root directory where individual component PNGs reside.
- `--sheet-root` *(optional)*: Path to directory containing assembled sheets for cross-verification.
- `--schema` *(optional)*: Override path to JSON Schema (defaults to `.agents/skills/cozy-ui-asset-workflow/references/component_manifest.schema.json`).
- `--report` *(optional)*: Path to write a JSON summary report.

**Exit Codes**:
- `0`: All schema, semantic, asset, and sheet validations passed.
- `1`: Validation failed (errors reported on stderr).
- `2`: Missing required dependencies (Pillow or jsonschema).

---

### 4.2 `assemble_ui_sheets.py`

Composites component PNGs onto transparent spritesheets atomically:

```bash
python Tools/UIAssets/assemble_ui_sheets.py \
  --manifest path/to/manifest.json \
  --asset-root path/to/raw_pngs/ \
  --output-root path/to/assembled_sheets/ \
  [--overwrite] \
  [--schema path/to/component_manifest.schema.json]
```

**Options**:
- `--manifest` *(required)*: Path to the component manifest JSON.
- `--asset-root` *(required)*: Root directory containing source PNGs.
- `--output-root` *(required)*: Destination directory for generated sheet PNGs.
- `--overwrite` *(optional)*: Allow replacing existing sheet files.
- `--schema` *(optional)*: Override path to custom JSON schema for preflight check.

**Exit Codes**:
- `0`: All sheets assembled and verified successfully.
- `1`: Assembly aborted due to preflight failure, collision, or existing files without `--overwrite`.
- `2`: Missing required dependencies (Pillow or jsonschema).

---

## 5. Safety, Transaction & Determinism Policies

1. **Path Traversal Protection**: Both tools resolve paths and reject any `source_file` or sheet path outside the designated root directories.
2. **Atomic Output Transactions**: `assemble_ui_sheets.py` renders sheets to a temporary staging folder `.staging_<id>` under the output directory (on the same filesystem). It re-validates all staged sheets before publishing. If any sheet fails or is corrupted, staging is deleted and no partial output is written.
3. **Rollback on Publish / Overwrite Failure**: Sheet files are published sequentially in sorted order via `os.replace`. If an exception occurs during publishing, any newly created sheets are removed, and previously existing sheets that were replaced are restored from temporary backups.
   *(Note: This rollback guarantees all-or-nothing consistency for catchable process-level exceptions. It does not provide multi-file transactional persistence against sudden hardware power loss, kill signals, or OS crashes during replacement).*
4. **Deterministic Output**: Component pasting order is deterministically sorted by row, column, and component ID. Outputs are guaranteed to be pixel-identical and SHA256-identical within the same recorded Python / Pillow runtime and dependency environment.
