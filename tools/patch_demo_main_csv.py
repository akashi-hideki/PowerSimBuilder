"""Add PSB_DUMP_CSV support to generated/demo_main.py (Trap #88 temporary).

The patch is overwritten on the next "Create Simulation". A permanent fix
belongs in PythonSimulationMainBuilder (post-GO/NO-GO).

Usage:
    python tools/patch_demo_main_csv.py --check   # exit 0 if already patched, 1 if not
    python tools/patch_demo_main_csv.py --apply   # patch (idempotent)
    python tools/patch_demo_main_csv.py --path <demo_main.py> --apply
"""
import argparse
import sys
from pathlib import Path


INJECT = (
    "    # QA: dump CSV when PSB_DUMP_CSV is set (Trap #88: builder should emit this)\n"
    "    import os as _os\n"
    '    _csv_path = _os.environ.get("PSB_DUMP_CSV", "")\n'
    "    if _csv_path:\n"
    "        from python_runtime.logger import dump_csv as _dump_csv\n"
    "        _dump_csv(sim_data, _csv_path)\n"
    '        print(f"[CSV] {_csv_path}")\n'
)
ANCHOR = "    plot_signals(sim_data, plot_config)\n"


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--path", default="generated/demo_main.py")
    ap.add_argument("--check", action="store_true")
    ap.add_argument("--apply", action="store_true")
    args = ap.parse_args()

    p = Path(args.path)
    if not p.is_file():
        print(f"[ERR] not found: {p}", file=sys.stderr)
        return 2

    s = p.read_bytes().decode("utf-8").replace("\r\n", "\n")
    patched = "PSB_DUMP_CSV" in s and "dump_csv" in s

    if args.check:
        if patched:
            print("[OK] already patched")
            return 0
        print("[NEED] not patched")
        return 1

    if args.apply:
        if patched:
            print("[SKIP] already patched")
            return 0
        if ANCHOR not in s:
            print(f"[ERR] anchor not found in {p}: {ANCHOR!r}", file=sys.stderr)
            return 1
        s = s.replace(ANCHOR, INJECT + ANCHOR, 1)
        p.write_bytes(s.encode("utf-8"))
        print(f"[OK] patched {p}")
        return 0

    ap.print_usage()
    return 2


if __name__ == "__main__":
    sys.exit(main())
