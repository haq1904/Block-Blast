#!/usr/bin/env python3
"""
UI Spritesheet Assembler

Deterministically composites individual UI component PNGs into multi-sheet
textures according to a component manifest JSON. Enforces preflight schema &
semantic validation, strict alpha compositing without conversion/cropping,
and atomic staging with rollback on overwrite failure.

Usage:
    python assemble_ui_sheets.py --manifest <manifest.json> --asset-root <dir> --output-root <dir> [--overwrite] [--schema <schema.json>]
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import shutil
import sys
import uuid
from pathlib import Path

# Dependency imports: non-fatal at module import time to keep library usage side-effect-free.
try:
    from PIL import Image
except ImportError:
    Image = None

# Import validation and composition functions from validator
try:
    from validate_ui_assets import (
        compose_expected_sheet,
        is_path_safe,
        load_manifest,
        require_image_dependencies,
        validate_assembled_sheets,
        validate_component_assets,
        validate_manifest_schema,
        validate_manifest_semantics,
    )
except ImportError:
    script_dir = Path(__file__).resolve().parent
    if str(script_dir) not in sys.path:
        sys.path.insert(0, str(script_dir))
    from validate_ui_assets import (
        compose_expected_sheet,
        is_path_safe,
        load_manifest,
        require_image_dependencies,
        validate_assembled_sheets,
        validate_component_assets,
        validate_manifest_schema,
        validate_manifest_semantics,
    )


def get_file_sha256(file_path: Path) -> str:
    """Compute SHA256 checksum of a file for determinism checks."""
    h = hashlib.sha256()
    with open(file_path, "rb") as f:
        while chunk := f.read(65536):
            h.update(chunk)
    return h.hexdigest()


def preflight(
    manifest: dict, asset_root: Path, output_root: Path, overwrite: bool, schema_path: Path = None
) -> list[str]:
    """Execute all schema, semantic, asset preflight checks before any writing."""
    errors = []

    # 1. Output file overwrite check
    sheets = manifest.get("sheets", {})
    if not overwrite:
        for sheet_id in sheets:
            dest_file = output_root / f"{sheet_id}.png"
            if dest_file.exists():
                errors.append(
                    f"Target sheet file already exists: '{dest_file}'. Pass --overwrite to allow replacing it."
                )

    # 2. Schema check
    schema_errors = validate_manifest_schema(manifest, schema_path)
    errors.extend(schema_errors)
    if schema_errors:
        return errors

    # 3. Semantics check
    semantic_errors = validate_manifest_semantics(manifest)
    errors.extend(semantic_errors)
    if semantic_errors:
        return errors

    # 4. Source asset check
    asset_errors, _ = validate_component_assets(manifest, asset_root)
    errors.extend(asset_errors)

    return errors


def publish_staged_sheets(staging_root: Path, output_root: Path, overwrite: bool) -> None:
    """Atomically publish sheets from staging directory to output root with rollback.

    Ensures all-or-nothing rollback for catchable process exceptions: if any sheet fails to publish,
    any newly published sheets are unlinked, and existing sheets that were replaced are restored
    from backup.
    """
    staged_files = sorted(staging_root.glob("*.png"), key=lambda p: p.name)
    if not staged_files:
        return

    output_root.mkdir(parents=True, exist_ok=True)
    backup_dir = staging_root / ".backup"
    backup_dir.mkdir(parents=True, exist_ok=True)

    existing_backups: dict[Path, Path] = {}
    newly_published: list[Path] = []
    replaced_published: list[Path] = []

    # 1. Back up all pre-existing target files before starting replacements
    for staged in staged_files:
        target = output_root / staged.name
        if target.exists():
            if not overwrite:
                raise FileExistsError(
                    f"Target sheet file already exists: '{target}'. Pass --overwrite to allow replacing it."
                )
            backup_path = backup_dir / f"{staged.name}.bak"
            shutil.copy2(target, backup_path)
            existing_backups[target] = backup_path

    # 2. Publish files using os.replace on the same filesystem
    publish_error = None
    for staged in staged_files:
        target = output_root / staged.name
        is_new = target not in existing_backups
        try:
            os.replace(staged, target)
            if is_new:
                newly_published.append(target)
            else:
                replaced_published.append(target)
        except Exception as e:
            publish_error = e
            break

    # 3. Handle rollback if any replacement failed
    if publish_error is not None:
        rollback_errors = []
        # Remove any newly published files
        for target in newly_published:
            try:
                if target.exists():
                    target.unlink()
            except Exception as re:
                rollback_errors.append(f"Failed to remove newly published file '{target}': {re}")

        # Restore pre-existing files from backup
        for target in replaced_published:
            backup_path = existing_backups.get(target)
            if backup_path and backup_path.exists():
                try:
                    os.replace(backup_path, target)
                except Exception as re:
                    rollback_errors.append(f"Failed to restore original file '{target}' from backup: {re}")

        # Also clean up remaining backups in backup_dir
        shutil.rmtree(backup_dir, ignore_errors=True)

        if rollback_errors:
            raise RuntimeError(
                f"Publish failed: {publish_error}. "
                f"Secondary rollback errors: {'; '.join(rollback_errors)}"
            ) from publish_error
        else:
            raise RuntimeError(
                f"Publish failed; rolled back cleanly to prior state: {publish_error}"
            ) from publish_error

    # 4. Clean up backups on complete success
    shutil.rmtree(backup_dir, ignore_errors=True)


def main():
    try:
        require_image_dependencies()
    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        sys.exit(2)

    parser = argparse.ArgumentParser(description="Assemble UI components into spritesheets deterministically.")
    parser.add_argument("--manifest", required=True, type=Path, help="Path to component manifest JSON")
    parser.add_argument("--asset-root", required=True, type=Path, help="Root directory containing source PNGs")
    parser.add_argument("--output-root", required=True, type=Path, help="Directory to save assembled sheet PNGs")
    parser.add_argument("--schema", type=Path, default=None, help="Optional path to custom JSON schema")
    parser.add_argument("--overwrite", action="store_true", help="Allow overwriting existing sheet files")

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

    # 1. Preflight validation (schema, semantics, source files, overwrite safety)
    preflight_errors = preflight(
        manifest, args.asset_root, args.output_root, args.overwrite, args.schema
    )
    if preflight_errors:
        print("[FAIL] Preflight validation failed before assembly:", file=sys.stderr)
        for err in preflight_errors:
            print(f"  - {err}", file=sys.stderr)
        sys.exit(1)

    sheets = manifest.get("sheets", {})

    # 2. Render all sheets into temporary staging directory on the same filesystem as output
    staging_id = uuid.uuid4().hex[:8]
    staging_dir = args.output_root / f".staging_{staging_id}"
    try:
        staging_dir.mkdir(parents=True, exist_ok=True)
    except Exception as e:
        print(f"Error: Could not create staging directory: {e}", file=sys.stderr)
        sys.exit(1)

    try:
        rendered_manifest = []
        for sheet_id in sorted(sheets.keys()):
            sheet_img = compose_expected_sheet(sheet_id, manifest, args.asset_root)
            staged_path = staging_dir / f"{sheet_id}.png"
            sheet_img.save(staged_path, "PNG", optimize=False)
            rendered_manifest.append(sheet_id)

        # 3. Re-validate staged sheets before publishing
        sheet_errors, _ = validate_assembled_sheets(manifest, args.asset_root, staging_dir)
        if sheet_errors:
            print("[FAIL] Assembled sheet validation failed during staging:", file=sys.stderr)
            for err in sheet_errors:
                print(f"  - {err}", file=sys.stderr)
            sys.exit(1)

        # 4. Atomic publish
        publish_staged_sheets(staging_dir, args.output_root, args.overwrite)

        # Print success summary with deterministic SHA256 checksums
        print(f"\n[PASS] Successfully assembled {len(rendered_manifest)} sheet(s):")
        for sheet_id in rendered_manifest:
            final_path = args.output_root / f"{sheet_id}.png"
            sha = get_file_sha256(final_path)
            dims = sheets[sheet_id]["dimensions"]
            print(f"  - {sheet_id}.png ({dims[0]}x{dims[1]}) -> SHA256: {sha}")

    except Exception as e:
        print(f"Error during sheet assembly: {e}", file=sys.stderr)
        sys.exit(1)
    finally:
        # Cleanup staging directory completely
        if staging_dir.exists():
            shutil.rmtree(staging_dir, ignore_errors=True)

    sys.exit(0)


if __name__ == "__main__":
    main()
