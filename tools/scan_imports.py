#!/usr/bin/env python3
"""scan_imports.py — AST-based import scanner for PowerSimBuilder.

Classifies every top-level import found in the repository as:
  - stdlib   : Python standard library
  - local    : project modules (tools/, tests/, examples/, ...)
  - external : third-party packages (candidates for requirements.txt)

Usage:
    python tools/scan_imports.py                        # list external imports
    python tools/scan_imports.py -v                     # per-file breakdown
    python tools/scan_imports.py --check lxml matplotlib
    python tools/scan_imports.py --json                 # machine-readable

Exit code for --check:
    0 = every specified package is imported somewhere
    1 = at least one specified package is NOT imported

Examples:
    python tools/scan_imports.py --check lxml matplotlib
    # -> decide whether to keep lxml / matplotlib in requirements.txt
"""

from __future__ import annotations

import argparse
import ast
import json
import sys
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent

SKIP_DIRS = {
    ".git", ".venv", "venv", "env", "build", "dist",
    "__pycache__", "node_modules", "tmp", "obj", "bin",
    ".pytest_cache", ".mypy_cache", ".ruff_cache",
    ".vs", ".idea", "generated",
    "user_components", "user_physics_models",
}

# Bundled stdlib list for Python < 3.10 (sys.stdlib_module_names is 3.10+).
STDLIB_39 = frozenset("""
    __future__ _abc _ast _asyncio _bisect _blake2 _bootlocale _bz2 _codecs
    _codecs_cn _codecs_hk _codecs_iso2022 _codecs_jp _codecs_kr _codecs_tw
    _collections _collections_abc _compat_pickle _compression _contextvars
    _crypt _csv _ctypes _curses _curses_panel _datetime _dbm _decimal
    _elementtree _functools _gdbm _hashlib _heapq _imp _io _json _locale
    _lsprof _lzma _markupbase _md5 _msi _multibytecodec _multiprocessing
    _opcode _operator _osx_support _overlapped _pickle _posixshmem
    _posixsubprocess _py_abc _pydecimal _pyio _queue _random _sha1 _sha256
    _sha3 _sha512 _signal _sitebuiltins _socket _sqlite3 _sre _ssl _stat
    _statistics _string _strptime _struct _symtable _thread _threading_local
    _tkinter _tracemalloc _uuid _warnings _weakref _weakrefset _winapi
    _zoneinfo abc aifc argparse array ast asynchat asyncio asyncore atexit
    audioop base64 bdb binascii binhex bisect builtins bz2 cProfile calendar
    cgi cgitb chunk cmath cmd code codecs codeop collections colorsys
    compileall concurrent configparser contextlib contextvars copy copyreg
    crypt csv ctypes curses dataclasses datetime dbm decimal difflib dis
    distutils doctest dummy_threading email encodings ensurepip enum errno
    faulthandler fcntl filecmp fileinput fnmatch formatter fractions ftplib
    functools gc getopt getpass gettext glob graphlib grp gzip hashlib hmac
    html http idlelib imaplib imghdr imp importlib inspect io ipaddress
    itertools json keyword lib2to3 linecache locale logging lzma mailbox
    mailcap marshal math mimetypes mmap modulefinder msilib msvcrt
    multiprocessing netrc nis nntplib nt ntpath numbers operator optparse
    os ossaudiodev parser pathlib pdb pickle pickletools pipes pkgutil
    platform plistlib poplib posix posixpath pprint profile pstats pty pwd
    py_compile pyclbr pydoc pydoc_data pyexpat queue quopri random re
    readline reprlib resource rlcompleter runpy sched secrets select
    selectors shelve shlex shutil signal site smtpd smtplib sndhdr socket
    socketserver spwd sqlite3 sre_compile sre_constants sre_parse ssl stat
    statistics string stringprep struct subprocess sunau symbol symtable sys
    sysconfig syslog tabnanny tarfile telnetlib tempfile termios test
    textwrap this threading time timeit tkinter token tokenize trace
    traceback tracemalloc tty turtle turtledemo types typing unicodedata
    unittest urllib uu uuid venv warnings wave weakref webbrowser winreg
    winsound wsgiref xdrlib xml xmlrpc zipapp zipfile zipimport zlib zoneinfo
    """.split())


def collect_stdlib() -> tuple[frozenset, str]:
    builtin = getattr(sys, "stdlib_module_names", None)
    if builtin is not None:
        return frozenset(builtin), "sys.stdlib_module_names"
    return STDLIB_39, "bundled 3.9 list"


def detect_local_modules(root: Path) -> set[str]:
    """Top-level names that belong to this project."""
    local: set[str] = set()
    for p in root.glob("*.py"):
        local.add(p.stem)
    for p in root.iterdir():
        if not p.is_dir() or p.name in SKIP_DIRS:
            continue
        if (p / "__init__.py").exists() or any(p.glob("*.py")):
            local.add(p.name)
    return local


def iter_py_files(root: Path):
    for p in root.rglob("*.py"):
        if any(part in SKIP_DIRS for part in p.relative_to(root).parts):
            continue
        yield p


def scan(root: Path) -> dict[str, list[str]]:
    results: dict[str, list[str]] = defaultdict(list)
    for py in iter_py_files(root):
        try:
            tree = ast.parse(
                py.read_text(encoding="utf-8", errors="ignore"),
                filename=str(py),
            )
        except SyntaxError as e:
            print(f"WARN: skip {py} ({e})", file=sys.stderr)
            continue
        rel = py.relative_to(root).as_posix()
        seen: set[str] = set()
        for node in ast.walk(tree):
            if isinstance(node, ast.Import):
                for n in node.names:
                    seen.add(n.name.split(".")[0])
            elif isinstance(node, ast.ImportFrom):
                if node.level:  # relative import
                    continue
                if node.module:
                    seen.add(node.module.split(".")[0])
        for name in seen:
            results[name].append(rel)
    return dict(results)


def classify(name: str, stdlib: frozenset, local: set[str]) -> str:
    if name in stdlib:
        return "stdlib"
    if name in local:
        return "local"
    return "external"


def main() -> None:
    ap = argparse.ArgumentParser(
        description="Scan PowerSimBuilder for imports and classify them.",
    )
    ap.add_argument("--verbose", "-v", action="store_true", help="show per-file breakdown")
    ap.add_argument("--check", nargs="+", metavar="PKG",
                    help="report whether specific packages are imported (exit 1 if any missing)")
    ap.add_argument("--json", action="store_true", help="machine-readable output")
    ap.add_argument("--include-local", action="store_true",
                    help="also list project-local modules")
    args = ap.parse_args()

    stdlib, stdlib_src = collect_stdlib()
    local = detect_local_modules(ROOT)
    results = scan(ROOT)

    if args.check:
        missing = 0
        for pkg in args.check:
            hits = results.get(pkg, [])
            if hits:
                print(f"FOUND     : {pkg}  ({len(hits)} file(s))")
                for f in hits:
                    print(f"    {f}")
            else:
                print(f"NOT FOUND : {pkg}")
                missing += 1
        sys.exit(1 if missing else 0)

    classified: dict[str, list[tuple[str, list[str]]]] = {
        "stdlib": [], "local": [], "external": [],
    }
    for name, files in sorted(results.items()):
        classified[classify(name, stdlib, local)].append((name, files))

    if args.json:
        payload = {
            cat: [{"name": n, "files": fs} for n, fs in items]
            for cat, items in classified.items()
        }
        print(json.dumps(payload, indent=2, ensure_ascii=False))
        return

    n_files = sum(1 for _ in iter_py_files(ROOT))
    print(f"Repo       : {ROOT}")
    print(f"Python     : {sys.version.split()[0]}")
    print(f".py files  : {n_files}")
    print(f"stdlib src : {stdlib_src}")
    print(f"local mods : {', '.join(sorted(local)) or '(none)'}")

    def dump(title: str, items: list[tuple[str, list[str]]], verbose: bool) -> None:
        if not items:
            return
        print(f"\n{title} ({len(items)}):")
        for name, files in items:
            print(f"  {name}")
            if verbose:
                for f in files:
                    print(f"    <- {f}")

    dump("EXTERNAL (third-party / requirements candidates)", classified["external"], args.verbose)
    if args.include_local:
        dump("LOCAL (project)", classified["local"], args.verbose)
    if args.verbose:
        dump("STDLIB", classified["stdlib"], args.verbose)

    print("\nVerify a specific package:")
    print("  python tools/scan_imports.py --check lxml matplotlib")


if __name__ == "__main__":
    main()