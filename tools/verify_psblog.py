"""Verify all sample .psblog files parse cleanly (regression check).

Usage:
    python tools/verify_psblog.py
"""
from __future__ import annotations

import sys
from pathlib import Path

from python_runtime.psblog import parse_psblog, parse_psblog_glob

SAMPLES = [
    "examples/logs/sample_minimal.psblog",
    "examples/logs/sample_pid_tuning.psblog",
    "examples/logs/sample_diagnostic.psblog",
    "examples/logs/sample_cooking_heater.psblog",
]

GLOB_SAMPLES = [
    "examples/logs/sample_rotated/log.psblog",
]


def main() -> int:
    root = Path(__file__).resolve().parent.parent
    failures = []

    for name in SAMPLES:
        p = root / name
        if not p.is_file():
            print(f"[FAIL] {name}: file not found")
            failures.append(name)
            continue
        try:
            trace = parse_psblog(p)
        except Exception as e:  # noqa: BLE001
            print(f"[FAIL] {name}: {type(e).__name__}: {e}")
            failures.append(name)
            continue
        if trace.header.get("schema") != "psblog/v1":
            print(f"[FAIL] {name}: schema != psblog/v1")
            failures.append(name)
            continue
        if trace.warnings > 0:
            print(f"[WARN] {name}: warnings={trace.warnings}")
        print(
            f"[OK] {name}: vars={len(trace.vars)} "
            f"rows={len(trace.rows)} warnings={trace.warnings}"
        )

    for name in GLOB_SAMPLES:
        p = root / name
        if not p.is_file():
            print(f"[FAIL] {name}: file not found (glob)")
            failures.append(name)
            continue
        try:
            trace = parse_psblog_glob(p)
        except Exception as e:  # noqa: BLE001
            print(f"[FAIL] {name}: {type(e).__name__}: {e} (glob)")
            failures.append(name)
            continue
        print(
            f"[OK] {name} (glob): files={len(trace.source_files)} "
            f"rows={len(trace.rows)} warnings={trace.warnings}"
        )

    if failures:
        print(f"\n{len(failures)} file(s) failed")
        return 1
    print(f"\nAll {len(SAMPLES) + len(GLOB_SAMPLES)} samples OK")
    return 0


if __name__ == "__main__":
    sys.exit(main())