#!/usr/bin/env python
"""Validate and auto-fix i18n JSON files (Resources/i18n/en.json, zh.json).

Fixable issues:
  - Missing comma between two entries
  - Trailing comma before closing brace

Reported (not fixable):
  - Malformed JSON beyond the above patterns
  - Key parity mismatch between en.json and zh.json

Usage:
    python tools/i18n_check.py
    python tools/i18n_check.py --fix
    python tools/i18n_check.py --fix --quiet
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parent.parent
I18N_DIR = REPO_ROOT / "Resources" / "i18n"
TARGETS = (I18N_DIR / "en.json", I18N_DIR / "zh.json")

VALUE_LINE = re.compile(r'^\s*"[^"]+"\s*:\s*"(?:[^"\\]|\\.)*"\s*(,?)\s*$')
KEY_LINE = re.compile(r'^\s*"[^"]+"\s*:')


def detect_newline(path: Path) -> str:
    raw = path.read_bytes()
    return "\r\n" if b"\r\n" in raw else "\n"


def parse_error(path: Path) -> tuple[int, int, str] | None:
    try:
        json.loads(path.read_text(encoding="utf-8"))
        return None
    except json.JSONDecodeError as e:
        return e.lineno, e.colno, e.msg


def fix_commas(path: Path) -> int:
    """Fix missing/trailing commas in place. Returns count of lines changed."""
    nl = detect_newline(path)
    raw = path.read_text(encoding="utf-8").replace("\r\n", "\n")
    lines = raw.splitlines()
    fixed = 0
    for i in range(len(lines)):
        m = VALUE_LINE.match(lines[i])
        if not m:
            continue
        has_comma = m.group(1) == ","
        nxt = ""
        for j in range(i + 1, len(lines)):
            if lines[j].strip():
                nxt = lines[j].strip()
                break
        if not nxt:
            continue
        if nxt.startswith("}"):
            if has_comma:
                lines[i] = lines[i].rstrip().rstrip(",")
                fixed += 1
        elif KEY_LINE.match(nxt):
            if not has_comma:
                lines[i] = lines[i].rstrip() + ","
                fixed += 1
    if fixed:
        out = "\n".join(lines)
        if nl == "\r\n":
            out = out.replace("\n", "\r\n")
        if not out.endswith(nl):
            out += nl
        path.write_text(out, encoding="utf-8", newline="")
    return fixed


def load_keys(path: Path) -> set[str] | None:
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
        if isinstance(data, dict):
            return set(data.keys())
    except (json.JSONDecodeError, OSError):
        pass
    return None


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    parser.add_argument("--fix", action="store_true", help="Auto-fix comma issues")
    parser.add_argument("--quiet", action="store_true")
    args = parser.parse_args()

    rc = 0
    for path in TARGETS:
        rel = path.relative_to(REPO_ROOT) if path.is_relative_to(REPO_ROOT) else path
        if not path.exists():
            print(f"[ERROR] {rel}: file not found", file=sys.stderr)
            rc = 1
            continue
        err = parse_error(path)
        if err:
            if args.fix:
                n = fix_commas(path)
                if n > 0 and not args.quiet:
                    print(f"[FIX] {rel}: {n} comma(s) adjusted")
                err = parse_error(path)
            if err:
                lineno, colno, msg = err
                print(f"[ERROR] {rel}:{lineno}:{colno}: {msg}", file=sys.stderr)
                rc = 1
            elif not args.quiet:
                print(f"[OK] {rel}: valid after fix")
        elif not args.quiet:
            print(f"[OK] {rel}: valid")

    if rc == 0:
        en = load_keys(TARGETS[0])
        zh = load_keys(TARGETS[1])
        if en is not None and zh is not None:
            only_en = sorted(en - zh)
            only_zh = sorted(zh - en)
            if only_en:
                preview = ", ".join(only_en[:10])
                more = f" (+{len(only_en) - 10} more)" if len(only_en) > 10 else ""
                print(f"[ERROR] Keys only in en.json ({len(only_en)}): {preview}{more}", file=sys.stderr)
                rc = 1
            if only_zh:
                preview = ", ".join(only_zh[:10])
                more = f" (+{len(only_zh) - 10} more)" if len(only_zh) > 10 else ""
                print(f"[ERROR] Keys only in zh.json ({len(only_zh)}): {preview}{more}", file=sys.stderr)
                rc = 1
            if not only_en and not only_zh and not args.quiet:
                print(f"[OK] Key parity: {len(en)} keys in both")

    return rc


if __name__ == "__main__":
    sys.exit(main())