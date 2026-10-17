#!/usr/bin/env python3
"""
UI Asset & Component Manifest Validator

Validates individual UI component PNGs and their manifest definitions
using Draft 2020-12 JSON Schema validation, semantic rules, image dimensions,
RGBA mode, transparency, optical center, grid placements, 9-slice bounds,
and assembled spritesheet integrity.

Usage:
    python validate_ui_assets.py --manifest <manifest.json> --asset-root <dir>
                                 [--sheet-root <assembled_dir>]
                                 [--schema <schema.json>]
                                 [--report <report.json>]
"""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

# Dependency imports: non-fatal at module import time to keep library usage side-effect-free.
try:
    from PIL import Image
except ImportError:
    Image = None

try:
    from jsonschema import Draft202012Validator
except ImportError:
    Draft202012Validator = None


def require_image_dependencies() -> None:
    """Check that required third-party libraries (Pillow, jsonschema) are importable."""
    missing = []
    if Image is None:
        missing.append("Pillow")
    if Draft202012Validator is None:
        missing.append("jsonschema")
    if missing:
        raise ImportError(
            f"Required dependencies missing: {', '.join(missing)}. "
            f"Please install them via 'pip install -r Tools/UIAssets/requirements.txt'."
        )


def is_power_of_two(n: int) -> bool:
    """Check if integer is a positive power of two."""
    return n > 0 and (n & (n - 1)) == 0


def is_path_safe(base_dir: Path, target_path: Path) -> bool:
    """Verify target_path is contained within base_dir to prevent path traversal."""
    try:
        resolved_base = base_dir.resolve()
        resolved_target = target_path.resolve()
        return resolved_target.is_relative_to(resolved_base)
    except Exception:
        return False


def get_default_schema_path() -> Path:
    """Resolve default component manifest schema path relative to repository root."""
    return (
        Path(__file__).resolve().parent.parent.parent
        / ".agents"
        / "skills"
        / "cozy-ui-asset-workflow"
        / "references"
        / "component_manifest.schema.json"
    )


def load_manifest(manifest_path: Path) -> dict:
    """Load and parse manifest JSON file."""
    with open(manifest_path, "r", encoding="utf-8") as f:
        return json.load(f)


def validate_manifest_schema(manifest: dict, schema_path: Path = None) -> list[str]:
    """Validate manifest against the Draft 2020-12 JSON Schema."""
    if Draft202012Validator is None:
        raise RuntimeError("jsonschema is required for schema validation but is not installed.")

    if schema_path is None:
        schema_path = get_default_schema_path()

    if not schema_path.is_file():
        return [f"Schema file not found at '{schema_path}'."]

    try:
        with open(schema_path, "r", encoding="utf-8") as f:
            schema = json.load(f)
    except Exception as e:
        return [f"Failed to read or parse schema file '{schema_path}': {e}"]

    try:
        Draft202012Validator.check_schema(schema)
    except Exception as e:
        return [f"Invalid JSON Schema definition in '{schema_path}': {e}"]

    validator = Draft202012Validator(schema)
    errors = []
    for err in sorted(validator.iter_errors(manifest), key=lambda e: [str(p) for p in e.path]):
        json_path = "$." + ".".join(str(p) for p in err.path) if err.path else "$"
        errors.append(f"Schema violation at {json_path}: {err.message}")

    return errors


def validate_manifest_semantics(manifest: dict) -> list[str]:
    """Perform cross-field semantic validation on manifest data."""
    errors = []
    sheets = manifest.get("sheets", {})
    components = manifest.get("components", [])

    # 1. Sheet semantic equations and constraints
    for sheet_id, sheet_info in sheets.items():
        if not isinstance(sheet_info, dict):
            continue

        dims = sheet_info.get("dimensions", [0, 0])
        grid = sheet_info.get("grid", {})
        padding = sheet_info.get("padding", 0)
        max_size = sheet_info.get("max_size")
        req_pot = sheet_info.get("require_power_of_two", False)

        cell_size = grid.get("cell_size", [0, 0])
        cols = grid.get("columns", 0)
        rows = grid.get("rows", 0)

        # Semantic grid equation: sheet_dim = 2 * padding + count * cell_dim
        expected_w = 2 * padding + cols * cell_size[0]
        expected_h = 2 * padding + rows * cell_size[1]

        if dims[0] != expected_w:
            errors.append(
                f"Sheet '{sheet_id}' width mismatch: declared {dims[0]}px != "
                f"expected {expected_w}px (2 * {padding} padding + {cols} * {cell_size[0]} cell_size)."
            )
        if dims[1] != expected_h:
            errors.append(
                f"Sheet '{sheet_id}' height mismatch: declared {dims[1]}px != "
                f"expected {expected_h}px (2 * {padding} padding + {rows} * {cell_size[1]} cell_size)."
            )

        if max_size is not None:
            if dims[0] > max_size or dims[1] > max_size:
                errors.append(
                    f"Sheet '{sheet_id}' dimensions ({dims[0]}x{dims[1]}) exceed max_size of {max_size}px."
                )

        if req_pot:
            if not is_power_of_two(dims[0]) or not is_power_of_two(dims[1]):
                errors.append(
                    f"Sheet '{sheet_id}' dimensions ({dims[0]}x{dims[1]}) violate power-of-two requirement."
                )

    # 2. Component references, unique IDs, grid boundary and placement fit
    seen_ids = set()
    sheet_grid_occupancy: dict[str, dict[tuple[int, int], str]] = {s_id: {} for s_id in sheets}

    for comp in components:
        if not isinstance(comp, dict):
            continue

        cid = comp.get("id")
        if cid in seen_ids:
            errors.append(f"Duplicate component ID detected: '{cid}'.")
        else:
            seen_ids.add(cid)

        target_sheet = comp.get("sheet_id")
        if target_sheet not in sheets:
            errors.append(f"Component '{cid}' references undefined sheet_id '{target_sheet}'.")
            continue

        sheet_info = sheets[target_sheet]
        grid = sheet_info.get("grid", {})
        cell_size = grid.get("cell_size", [1, 1])
        sheet_cols = grid.get("columns", 1)
        sheet_rows = grid.get("rows", 1)

        placement = comp.get("placement", {})
        col = placement.get("col", 0)
        row = placement.get("row", 0)
        col_span = placement.get("col_span", 1)
        row_span = placement.get("row_span", 1)

        dims = comp.get("dimensions", [0, 0])
        allocated_w = col_span * cell_size[0]
        allocated_h = row_span * cell_size[1]

        # Component dimensions must fit within allocated cell span
        if dims[0] > allocated_w or dims[1] > allocated_h:
            errors.append(
                f"Component '{cid}' dimensions ({dims[0]}x{dims[1]}) exceed its allocated grid span "
                f"({allocated_w}x{allocated_h} across {col_span}x{row_span} cells)."
            )

        # Boundary checks
        if col + col_span > sheet_cols or row + row_span > sheet_rows:
            errors.append(
                f"Component '{cid}' out of bounds on sheet '{target_sheet}': "
                f"col {col}+{col_span} > {sheet_cols} or row {row}+{row_span} > {sheet_rows}."
            )
            continue

        # Overlap checks
        for r in range(row, row + row_span):
            for c in range(col, col + col_span):
                coord = (c, r)
                if coord in sheet_grid_occupancy[target_sheet]:
                    colliding_id = sheet_grid_occupancy[target_sheet][coord]
                    errors.append(
                        f"Grid cell collision on sheet '{target_sheet}' at (col={c}, row={r}): "
                        f"'{cid}' collides with '{colliding_id}'."
                    )
                else:
                    sheet_grid_occupancy[target_sheet][coord] = cid

        # 9-slice semantic check
        nine_slice = comp.get("nine_slice")
        if isinstance(nine_slice, dict):
            nl = nine_slice.get("left", 0)
            nb = nine_slice.get("bottom", 0)
            nr = nine_slice.get("right", 0)
            nt = nine_slice.get("top", 0)
            if nl + nr >= dims[0]:
                errors.append(
                    f"Component '{cid}' nine_slice horizontal insets ({nl} + {nr} = {nl + nr}) "
                    f"must be strictly less than width ({dims[0]})."
                )
            if nb + nt >= dims[1]:
                errors.append(
                    f"Component '{cid}' nine_slice vertical insets ({nb} + {nt} = {nb + nt}) "
                    f"must be strictly less than height ({dims[1]})."
                )

    return errors


def validate_component_assets(manifest: dict, asset_root: Path) -> tuple[list[str], list[dict]]:
    """Inspect raw component PNG image files on disk."""
    if Image is None:
        raise RuntimeError("Pillow is required for image validation but is not installed.")

    errors = []
    results = []

    for comp in manifest.get("components", []):
        cid = comp.get("id", "unknown")
        src_rel = comp.get("source_file")
        if not src_rel:
            err = f"Component '{cid}' missing source_file."
            errors.append(err)
            results.append({"id": cid, "status": "FAIL", "errors": [err]})
            continue

        src_path = asset_root / src_rel
        if not is_path_safe(asset_root, src_path):
            err = f"Component '{cid}' source path attempts traversal outside asset root: '{src_rel}'."
            errors.append(err)
            results.append({"id": cid, "status": "FAIL", "errors": [err]})
            continue

        if not src_path.is_file():
            err = f"Component '{cid}' source file not found: '{src_path}'."
            errors.append(err)
            results.append({"id": cid, "status": "FAIL", "errors": [err]})
            continue

        comp_errors = []
        try:
            with Image.open(src_path) as img:
                if img.format != "PNG":
                    comp_errors.append(f"Image format is '{img.format}', expected 'PNG'.")

                if img.mode != "RGBA":
                    comp_errors.append(
                        f"Image mode is '{img.mode}', strictly expected 'RGBA' (no automatic conversion allowed)."
                    )

                w, h = img.size
                decl_w, decl_h = comp.get("dimensions", [0, 0])
                if w != decl_w or h != decl_h:
                    comp_errors.append(
                        f"Image dimensions ({w}x{h}) do not match declared dimensions ({decl_w}x{decl_h})."
                    )

                if img.mode == "RGBA":
                    alpha = img.getchannel("A")
                    min_a, max_a = alpha.getextrema()
                    req_transparency = comp.get("requires_transparency", True)

                    if max_a == 0:
                        comp_errors.append("Image is completely transparent (all alpha pixels are 0).")
                    elif req_transparency:
                        if min_a == 255:
                            comp_errors.append(
                                "Image has no transparent pixels (completely opaque), but requires_transparency is true."
                            )
                        elif min_a > 0:
                            comp_errors.append(
                                f"Image has no fully transparent background pixels (min alpha is {min_a} > 0), "
                                f"but requires_transparency is true."
                            )

                    bbox = alpha.getbbox()
                    if bbox is not None:
                        min_x, min_y, max_x, max_y = bbox

                        # Optical center check using normalized coordinates
                        exp_cx, exp_cy = comp.get("expected_center", [0.5, 0.5])
                        tol_px = comp.get("center_tolerance_px", 2.5)

                        actual_cx_norm = (min_x + max_x) / (2.0 * w)
                        actual_cy_norm = (min_y + max_y) / (2.0 * h)

                        diff_x_px = abs(actual_cx_norm - exp_cx) * w
                        diff_y_px = abs(actual_cy_norm - exp_cy) * h

                        if diff_x_px > tol_px or diff_y_px > tol_px:
                            comp_errors.append(
                                f"Optical center offset ({diff_x_px:.2f}px, {diff_y_px:.2f}px) "
                                f"exceeds tolerance of {tol_px}px from expected [{exp_cx}, {exp_cy}]."
                            )

                        # Padding constraint check
                        req_padding = comp.get("padding", 0)
                        if req_padding > 0:
                            pad_l = min_x
                            pad_t = min_y
                            pad_r = w - max_x
                            pad_b = h - max_y
                            if any(p < req_padding for p in (pad_l, pad_t, pad_r, pad_b)):
                                comp_errors.append(
                                    f"Padding violated: expected >= {req_padding}px, "
                                    f"got L={pad_l}, T={pad_t}, R={pad_r}, B={pad_b}."
                                )

        except Exception as e:
            comp_errors.append(f"Failed to inspect image: {e}")

        if comp_errors:
            errors.extend([f"[{cid}] {e}" for e in comp_errors])
            results.append({"id": cid, "status": "FAIL", "errors": comp_errors})
        else:
            results.append({"id": cid, "status": "PASS", "errors": []})

    return errors, results


def canonicalize_transparent_rgb(image: Image.Image) -> Image.Image:
    """Normalize RGB channels to 0 at all pixels where alpha is 0.
    
    Prevents differences in hidden RGB values under fully transparent pixels from causing
    false positive canvas comparison mismatches, while leaving semi-transparent and opaque
    pixels completely untouched.
    """
    if Image is None:
        raise RuntimeError("Pillow is required for image operations but is not installed.")

    if image.mode != "RGBA":
        return image

    r, g, b, a = image.split()
    mask = a.point(lambda p: 255 if p > 0 else 0)
    black = Image.new("L", image.size, 0)
    r_clean = Image.composite(r, black, mask)
    g_clean = Image.composite(g, black, mask)
    b_clean = Image.composite(b, black, mask)
    return Image.merge("RGBA", (r_clean, g_clean, b_clean, a))


def compose_expected_sheet(sheet_id: str, manifest: dict, asset_root: Path) -> Image.Image:
    """Compose canonical expected sheet canvas using deterministic alpha compositing.
    
    Shared by both assemble_ui_sheets.py and validate_assembled_sheets to guarantee
    single-source composition semantics.
    """
    if Image is None:
        raise RuntimeError("Pillow is required for image operations but is not installed.")

    sheet_info = manifest["sheets"][sheet_id]
    sheet_w, sheet_h = sheet_info["dimensions"]
    grid = sheet_info["grid"]
    cell_w, cell_h = grid["cell_size"]
    padding = sheet_info.get("padding", 0)

    canvas = Image.new("RGBA", (sheet_w, sheet_h), (0, 0, 0, 0))

    comps = [c for c in manifest.get("components", []) if c.get("sheet_id") == sheet_id]
    sorted_comps = sorted(
        comps,
        key=lambda c: (
            c.get("placement", {}).get("row", 0),
            c.get("placement", {}).get("col", 0),
            c.get("id", ""),
        ),
    )

    for comp in sorted_comps:
        cid = comp["id"]
        src_path = asset_root / comp["source_file"]
        placement = comp["placement"]
        col = placement["col"]
        row = placement["row"]
        col_span = placement.get("col_span", 1)
        row_span = placement.get("row_span", 1)

        alloc_w = col_span * cell_w
        alloc_h = row_span * cell_h

        cell_x = padding + col * cell_w
        cell_y = padding + row * cell_h

        with Image.open(src_path) as comp_img:
            if comp_img.mode != "RGBA":
                raise ValueError(f"Component '{cid}' is in mode '{comp_img.mode}', strictly expected 'RGBA'.")

            offset_x = (alloc_w - comp_img.width) // 2
            offset_y = (alloc_h - comp_img.height) // 2
            dest_x = cell_x + offset_x
            dest_y = cell_y + offset_y

            overlay = Image.new("RGBA", (sheet_w, sheet_h), (0, 0, 0, 0))
            overlay.paste(comp_img, (dest_x, dest_y))
            canvas = Image.alpha_composite(canvas, overlay)

    return canvas


def validate_assembled_sheets(
    manifest: dict, asset_root: Path, sheet_root: Path
) -> tuple[list[str], list[dict]]:
    """Validate assembled spritesheets against source expectations by full-canvas comparison."""
    if Image is None:
        raise RuntimeError("Pillow is required for image validation but is not installed.")

    errors = []
    results = []
    sheets = manifest.get("sheets", {})

    for sheet_id, sheet_info in sheets.items():
        sheet_file = sheet_root / f"{sheet_id}.png"
        if not is_path_safe(sheet_root, sheet_file):
            err = f"Sheet '{sheet_id}' attempts path traversal outside sheet root."
            errors.append(err)
            results.append({"sheet_id": sheet_id, "status": "FAIL", "errors": [err]})
            continue

        if not sheet_file.is_file():
            err = f"Assembled sheet file not found: '{sheet_file}'."
            errors.append(err)
            results.append({"sheet_id": sheet_id, "status": "FAIL", "errors": [err]})
            continue

        sheet_errors = []
        try:
            with Image.open(sheet_file) as sheet_img:
                if sheet_img.format != "PNG":
                    sheet_errors.append(f"Sheet '{sheet_id}' is format '{sheet_img.format}', expected 'PNG'.")

                if sheet_img.mode != "RGBA":
                    sheet_errors.append(f"Sheet '{sheet_id}' mode is '{sheet_img.mode}', expected 'RGBA'.")

                decl_w, decl_h = sheet_info.get("dimensions", [0, 0])
                if sheet_img.size != (decl_w, decl_h):
                    sheet_errors.append(
                        f"Sheet '{sheet_id}' size is {sheet_img.size}, expected {decl_w}x{decl_h}."
                    )

                # Full-canvas canonical comparison
                expected_img = compose_expected_sheet(sheet_id, manifest, asset_root)
                actual_canonical = canonicalize_transparent_rgb(sheet_img)
                expected_canonical = canonicalize_transparent_rgb(expected_img)

                if actual_canonical.tobytes() != expected_canonical.tobytes():
                    sheet_errors.append(
                        f"Sheet '{sheet_id}' canvas mismatch: pixel data differs from expected composition."
                    )

        except Exception as e:
            sheet_errors.append(f"Failed to inspect assembled sheet: {e}")

        if sheet_errors:
            errors.extend([f"[Sheet {sheet_id}] {e}" for e in sheet_errors])
            results.append({"sheet_id": sheet_id, "status": "FAIL", "errors": sheet_errors})
        else:
            results.append({"sheet_id": sheet_id, "status": "PASS", "errors": []})

    return errors, results


def main():
    try:
        require_image_dependencies()
    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        sys.exit(2)

    parser = argparse.ArgumentParser(description="Validate UI component assets and manifest.")
    parser.add_argument("--manifest", required=True, type=Path, help="Path to component manifest JSON")
    parser.add_argument("--asset-root", required=True, type=Path, help="Root directory containing component PNGs")
    parser.add_argument("--sheet-root", type=Path, default=None, help="Optional directory containing assembled sheets")
    parser.add_argument("--schema", type=Path, default=None, help="Optional path to custom JSON schema")
    parser.add_argument("--report", type=Path, default=None, help="Optional output path for JSON validation report")

    args = parser.parse_args()

    if not args.manifest.is_file():
        print(f"Error: Manifest file not found: '{args.manifest}'", file=sys.stderr)
        sys.exit(1)

    if not args.asset_root.is_dir():
        print(f"Error: Asset root directory not found: '{args.asset_root}'", file=sys.stderr)
        sys.exit(1)

    try:
        manifest = load_manifest(args.manifest)
    except Exception as e:
        print(f"Error: Failed to parse manifest JSON: {e}", file=sys.stderr)
        sys.exit(1)

    all_errors = []
    report_stages = {}

    # Stage 1: Schema Validation
    schema_errors = validate_manifest_schema(manifest, args.schema)
    all_errors.extend(schema_errors)
    report_stages["schema"] = {
        "status": "FAIL" if schema_errors else "PASS",
        "errors": schema_errors,
    }

    if schema_errors:
        print("Manifest schema validation failed:", file=sys.stderr)
        for err in schema_errors:
            print(f"  - {err}", file=sys.stderr)
        if args.report:
            try:
                args.report.parent.mkdir(parents=True, exist_ok=True)
                with open(args.report, "w", encoding="utf-8") as f:
                    json.dump({"status": "FAIL", "errors": all_errors, "stages": report_stages}, f, indent=2)
            except Exception as e:
                print(f"Error: Failed to write report file '{args.report}': {e}", file=sys.stderr)
                sys.exit(1)
        sys.exit(1)

    # Stage 2: Semantic Validation
    semantic_errors = validate_manifest_semantics(manifest)
    all_errors.extend(semantic_errors)
    report_stages["semantics"] = {
        "status": "FAIL" if semantic_errors else "PASS",
        "errors": semantic_errors,
    }

    # Stage 3: Component Asset Inspection
    component_errors, component_results = validate_component_assets(manifest, args.asset_root)
    all_errors.extend(component_errors)
    report_stages["components"] = {
        "status": "FAIL" if component_errors else "PASS",
        "total": len(component_results),
        "passed": sum(1 for c in component_results if c["status"] == "PASS"),
        "failed": sum(1 for c in component_results if c["status"] == "FAIL"),
        "results": component_results,
    }

    # Stage 4: Sheet Inspection (Optional)
    if args.sheet_root:
        if not args.sheet_root.is_dir():
            sheet_dir_err = f"Sheet root directory not found: '{args.sheet_root}'"
            all_errors.append(sheet_dir_err)
            report_stages["sheets"] = {"status": "FAIL", "errors": [sheet_dir_err]}
        else:
            sheet_errors, sheet_results = validate_assembled_sheets(manifest, args.asset_root, args.sheet_root)
            all_errors.extend(sheet_errors)
            report_stages["sheets"] = {
                "status": "FAIL" if sheet_errors else "PASS",
                "results": sheet_results,
            }

    # Write report if requested
    if args.report:
        try:
            args.report.parent.mkdir(parents=True, exist_ok=True)
            with open(args.report, "w", encoding="utf-8") as f:
                json.dump(
                    {
                        "pack_name": manifest.get("pack_name", "unknown"),
                        "status": "FAIL" if all_errors else "PASS",
                        "errors": all_errors,
                        "stages": report_stages,
                    },
                    f,
                    indent=2,
                )
            print(f"Validation report saved to '{args.report}'.")
        except Exception as e:
            print(f"Error: Failed to write report file '{args.report}': {e}", file=sys.stderr)
            sys.exit(1)

    if all_errors:
        print(f"\n[FAIL] Validation completed with {len(all_errors)} error(s):", file=sys.stderr)
        for err in all_errors[:20]:
            print(f"  - {err}", file=sys.stderr)
        if len(all_errors) > 20:
            print(f"  ... and {len(all_errors) - 20} more error(s).", file=sys.stderr)
        sys.exit(1)
    else:
        print(f"\n[PASS] Validation successful: manifest and all assets verified cleanly.")
        sys.exit(0)


if __name__ == "__main__":
    main()
