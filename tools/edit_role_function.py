#!/usr/bin/env python3
"""edit_role_function.py - Safely replace the body of a role function
inside a StaTable-generated *_powersim.py.

Only the region between
    # STABLE_USER_CODE_START:<Layer>.<F_Func>
    # STABLE_USER_CODE_END:<Layer>.<F_Func>
is replaced. PowerSimBuilder preserves this region across re-import.

Usage:
    python tools/edit_role_function.py <powersim.py> <Layer> <F_Func> --body "code"
    python tools/edit_role_function.py <powersim.py> <Layer> <F_Func> <body_file>
    python tools/edit_role_function.py <powersim.py> <Layer> <F_Func> --show

Example:
    python tools/edit_role_function.py generated/CookingHeaterController_powersim.py MwOven F_ReadThermistors --body "return 0"
"""
from __future__ import annotations

import argparse
import ast
import sys
from pathlib import Path


def extract_body(text: str, layer: str, func: str) -> tuple[int, int] | None:
    start = f"# STABLE_USER_CODE_START:{layer}.{func}"
    end = f"# STABLE_USER_CODE_END:{layer}.{func}"
    si = text.find(start)
    if si < 0:
        return None
    ei = text.find(end)
    if ei < 0 or ei < si:
        return None
    nl = "\r\n" if "\r\n" in text else "\n"
    line_end = text.find(nl, si)
    if line_end < 0:
        return None
    line_end += len(nl)
    line_start = text.rfind(nl, 0, ei) + len(nl)
    return line_end, line_start


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("powersim")
    ap.add_argument("layer")
    ap.add_argument("func")
    ap.add_argument("body_file", nargs="?")
    ap.add_argument("--body")
    ap.add_argument("--show", action="store_true", help="print current body and exit")
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    path = Path(args.powersim)
    if not path.is_file():
        print(f"ERROR: {path} not found")
        return 2

    text = path.read_text(encoding="utf-8", newline="")
    span = extract_body(text, args.layer, args.func)
    if span is None:
        print(f"ERROR: markers not found for {args.layer}.{args.func}")
        return 3
    line_end, line_start = span
    current = text[line_end:line_start]

    if args.show:
        print(f"=== Current body of {args.layer}.{args.func} ===")
        print(current, end="")
        return 0

    if args.body:
        body = args.body
    elif args.body_file:
        body = Path(args.body_file).read_text(encoding="utf-8")
    else:
        print("ERROR: provide --body or body_file (or use --show)")
        return 2

    nl = "\r\n" if "\r\n" in text else "\n"
    body = body.rstrip("\r\n")
    lines = body.splitlines() or [""]
    indented = "".join(("        " + ln.lstrip() if ln.strip() else "") + nl for ln in lines)

    new_text = text[:line_end] + indented + text[line_start:]
    try:
        ast.parse(new_text)
    except SyntaxError as e:
        print(f"ERROR: syntax error in new file: {e}")
        return 4

    if args.dry_run:
        print("=== DRY RUN ===")
        print("--- old ---")
        print(current, end="")
        print("--- new ---")
        print(indented, end="")
        return 0

    path.write_text(new_text, encoding="utf-8", newline="")
    print(f"OK: {path.name} {args.layer}.{args.func} updated")
    return 0


if __name__ == "__main__":
    sys.exit(main())