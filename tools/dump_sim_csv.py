"""Run generated demo_main.py with SIM_DUMP_CSV to emit all signals as CSV.

Usage:
    python tools/dump_sim_csv.py generated/demo_main.py tmp/sim_signals.csv
    python tools/dump_sim_csv.py generated/demo_main.py tmp/sim_signals.csv --t-end 300
"""
from __future__ import annotations
import os, re, subprocess, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent

INJECT = '''
    # --- dump_sim_csv injection ---
    import os as _os
    _dump_path = _os.environ.get("SIM_DUMP_CSV")
    if _dump_path:
        import csv as _csv
        _keys = set()
        for _log in sim_data.logs:
            for _cid, _d in _log.get("components", {}).items():
                for _p in _d.get("outputs", {}):
                    _keys.add((_cid, _p))
        _keys = sorted(_keys)
        with open(_dump_path, "w", newline="", encoding="utf-8") as _f:
            _w = _csv.writer(_f)
            _w.writerow(["time"] + [f"{c}.{p}" for c, p in _keys])
            for _log in sim_data.logs:
                _w.writerow([_log["time"]] + [
                    _log.get("components", {}).get(c, {}).get("outputs", {}).get(p, "")
                    for c, p in _keys
                ])
        print(f"[dump_sim_csv] wrote {_dump_path} ({len(sim_data.logs)} rows)")
    # --- end injection ---
'''


def instrument(src: Path, dst: Path, t_end: float | None) -> None:
    code = src.read_text(encoding="utf-8")
    if t_end is not None:
        code = re.sub(
            r"^(\s*)t_end\s*=\s*[\d.]+",
            lambda m: f"{m.group(1)}t_end = {t_end}",
            code,
            flags=re.MULTILINE,
        )
    m = re.search(r"^(\s*)plot_signals\(", code, re.M)
    if not m:
        raise SystemExit("plot_signals( call not found in code")
    indent = m.group(1)
    injected = "\n".join(
        (indent + line if line.strip() else line)
        for line in INJECT.strip("\n").split("\n")
    )
    code = code[: m.start()] + injected + "\n\n" + code[m.start():]
    dst.parent.mkdir(parents=True, exist_ok=True)
    dst.write_text(code, encoding="utf-8")


def main() -> None:
    if len(sys.argv) < 2:
        print(__doc__)
        sys.exit(2)
    src = Path(sys.argv[1])
    out = Path(sys.argv[2]) if len(sys.argv) > 2 else ROOT / "tmp" / "sim_signals.csv"
    t_end = None
    if "--t-end" in sys.argv:
        t_end = float(sys.argv[sys.argv.index("--t-end") + 1])
    if not src.is_file():
        raise SystemExit(f"not found: {src}")
    tmp = ROOT / "tmp" / ("_dump_" + src.name)
    instrument(src, tmp, t_end)
    env = os.environ.copy()
    env["SIM_DUMP_CSV"] = str(out)
    env.setdefault("MPLBACKEND", "Agg")  # no GUI popup
    r = subprocess.run([sys.executable, str(tmp)], env=env, cwd=str(ROOT))
    sys.exit(r.returncode)


if __name__ == "__main__":
    main()