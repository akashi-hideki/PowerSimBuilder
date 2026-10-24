#!/usr/bin/env python3
"""Build FMI Cross Check catalog JSON via git ls-tree (no GitHub API).

Avoids the Contents API (slow, rate-limited) by cloning only tree metadata
(--filter=blob:none --no-checkout) and listing paths with git ls-tree.

Usage:
    python tools/build_cross_check_catalog.py
    python tools/build_cross_check_catalog.py --platform win64 --max-models 50
    python tools/build_cross_check_catalog.py --keep-clone
"""
from __future__ import annotations
import argparse
import json
import shutil
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

REPO_URL = "https://github.com/modelica/fmi-cross-check.git"
DEFAULT_CLONE_DIR = "tmp/fmi-cross-check-meta"
DEFAULT_OUTPUT = "Resources/generated_catalog.json"
DEFAULT_PLATFORMS = ["win64", "c-code"]


def ensure_clone(clone_dir: Path) -> Path:
    if clone_dir.exists() and (clone_dir / ".git").exists():
        # Verify the clone actually has a valid HEAD (detect interrupted clones)
        try:
            subprocess.run(
                ["git", "rev-parse", "HEAD"],
                cwd=str(clone_dir),
                capture_output=True,
                check=True,
                text=True,
            )
            # Also check the tree has content
            result = subprocess.run(
                ["git", "ls-tree", "-r", "HEAD", "--name-only"],
                cwd=str(clone_dir),
                capture_output=True,
                check=True,
                text=True,
            )
            if result.stdout.strip():
                print(f"[SKIP] clone exists: {clone_dir}")
                return clone_dir
            print(f"[WARN] clone is empty (interrupted), removing: {clone_dir}")
        except subprocess.CalledProcessError:
            print(f"[WARN] stale clone detected, removing: {clone_dir}")
        shutil.rmtree(clone_dir, ignore_errors=True)
    if clone_dir.exists():
        shutil.rmtree(clone_dir)
    clone_dir.parent.mkdir(parents=True, exist_ok=True)
    cmd = [
        "git", "clone",
        "--filter=blob:none",
        "--no-checkout",
        "--depth", "1",
        REPO_URL,
        str(clone_dir),
    ]
    print(f"[INFO] running: {' '.join(cmd)}")
    result = subprocess.run(cmd, capture_output=True, text=True)
    if result.returncode != 0:
        sys.stderr.write(result.stderr)
        sys.exit(2)
    return clone_dir


def list_paths(clone_dir: Path) -> list[str]:
    result = subprocess.run(
        ["git", "ls-tree", "-r", "HEAD", "--name-only"],
        cwd=str(clone_dir), capture_output=True, text=True, check=True,
    )
    return [line.strip() for line in result.stdout.splitlines() if line.strip()]


def _sort_key(p: str) -> tuple:
    """Prioritize FMI 2.0 over 1.0, win64 over c-code, then alphabetical.

    Path format: fmus/{fmi_ver}/{fmi_type}/{platform}/{tool}/{version}/{model}/{file}.fmu
    """
    parts = p.split("/")
    if len(parts) < 7:
        return (9, 9, 9, p)
    fmi_ver = parts[1]
    platform = parts[3]
    ver_rank = {"2.0": 0, "3.0": 1, "1.0": 2}.get(fmi_ver, 8)
    plat_rank = {"win64": 0, "linux64": 1, "darwin64": 2, "c-code": 3}.get(platform, 8)
    return (ver_rank, plat_rank, p)


def build_catalog(paths: list[str], platforms: list[str], max_models: int) -> list[dict]:
    models: list[dict] = []
    seen: set[tuple[str, str, str]] = set()
    platform_set = set(platforms)
    for p in sorted(paths, key=_sort_key):
        if not p.lower().endswith(".fmu"):
            continue
        parts = p.split("/")
        if len(parts) < 8 or parts[0] != "fmus":
            continue
        fmi_version, fmi_type, platform, tool_id, tool_version, model_name = parts[1:7]
        if platform_set and platform not in platform_set:
            continue
        key = (model_name, tool_id, tool_version)
        if key in seen:
            continue
        seen.add(key)
        models.append({
            "name": model_name,
            "url": f"https://raw.githubusercontent.com/modelica/fmi-cross-check/master/{p}",
            "sizeBytes": 0,
            "license": "Modelica Association (FMI Cross Check)",
            "description": f"FMI {fmi_version} / {fmi_type} / {platform} / {tool_id} {tool_version}",
        })
        if max_models and len(models) >= max_models:
            break
    return models


def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--platform", action="append", help="platform filter (default: win64,c-code)")
    ap.add_argument("--max-models", type=int, default=0)
    ap.add_argument("--output", default=DEFAULT_OUTPUT)
    ap.add_argument("--clone-dir", default=DEFAULT_CLONE_DIR)
    ap.add_argument("--keep-clone", action="store_true")
    args = ap.parse_args(argv)

    platforms = args.platform or DEFAULT_PLATFORMS
    clone_dir = Path(args.clone_dir)

    ensure_clone(clone_dir)
    paths = list_paths(clone_dir)
    print(f"[INFO] total paths: {len(paths)}")
    fmu_paths = [p for p in paths if p.lower().endswith(".fmu")]
    print(f"[INFO] .fmu paths: {len(fmu_paths)}")

    models = build_catalog(paths, platforms, args.max_models)
    if not models:
        print("[NG] no models matched filters", file=sys.stderr)
        return 1

    catalog = {
        "version": 1,
        "generated_at": datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "source": f"modelica/fmi-cross-check ({', '.join(platforms)}) [ls-tree]",
        "models": models,
    }
    out = Path(args.output)
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(catalog, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"[OK] wrote {len(models)} model(s) to {out}")

    if not args.keep_clone:
        shutil.rmtree(clone_dir, ignore_errors=True)
        print(f"[INFO] removed clone: {clone_dir}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
