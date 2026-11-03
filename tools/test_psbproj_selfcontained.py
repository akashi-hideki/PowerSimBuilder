#!/usr/bin/env python3
"""Verify a .psbproj is self-contained: extract to temp, run demo_main.py.

Usage:
    python tools/test_psbproj_selfcontained.py examples/demos/oven_control_statable_v181.psbproj

Exit code: 0 = simulation completed, non-zero = failure.
"""
from __future__ import annotations

import subprocess
import sys
import tempfile
import zipfile
from pathlib import Path


def main() -> int:
    if len(sys.argv) < 2:
        print("usage: test_psbproj_selfcontained.py <archive.psbproj>")
        return 2
    archive = Path(sys.argv[1])
    if not archive.is_file():
        print(f"ERROR: archive not found: {archive}")
        return 2

    extract = Path(tempfile.mkdtemp(prefix="psb_selfcont_"))
    print(f"extract: {extract}")

    with zipfile.ZipFile(archive) as z:
        for entry in z.namelist():
            if entry == "project.json":
                continue
            rel = entry
            if rel.startswith("components/") and rel.endswith("_powersim.py"):
                rel = "generated/" + rel[len("components/"):]
            target = extract / rel
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(z.read(entry))

    gen_dir = extract / "generated"
    print()
    print("generated/ contents:")
    if gen_dir.exists():
        for f in sorted(gen_dir.iterdir()):
            print(f"  {f.name} ({f.stat().st_size} bytes)")

    demo = gen_dir / "demo_main.py"
    if not demo.exists():
        print("ERROR: generated/demo_main.py not found")
        return 1

    print()
    print(f"running: {demo}")
    r = subprocess.run(
        [sys.executable, str(demo)], cwd=str(extract), capture_output=True, text=True
    )
    print(f"exit: {r.returncode}")
    if r.stdout:
        print("stdout tail:", r.stdout[-500:])
    if r.stderr:
        print("stderr tail:", r.stderr[-500:])
    return r.returncode


if __name__ == "__main__":
    sys.exit(main())
