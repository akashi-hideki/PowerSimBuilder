"""Inspect ControllerBox instances in a .psbproj and verify I/O wiring.

Usage:
    python tools/inspect_controller_box.py examples/demos/oven_control.psbproj
    python tools/inspect_controller_box.py examples/demos/*.psbproj
"""
from __future__ import annotations
import sys, zipfile, json, importlib.util
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))
from python_runtime.components.controller_box import ControllerBox


def _get(d, *names, default=None):
    for n in names:
        if isinstance(d, dict) and n in d:
            return d[n]
    return default


def inspect(path: str) -> int:
    p = Path(path)
    print(f"\n=== {p.name} ===")
    if not p.exists():
        print(f"  [ERROR] not found: {p}")
        return 1

    proj = json.loads(zipfile.ZipFile(p).read("project.json"))
    cbs = [
        c for c in proj.get("components", [])
        if _get(c.get("descriptor", {}), "type", "Type", default="") == "ControllerBox"
    ]
    if not cbs:
        print("  (no ControllerBox)")
        return 0

    print(f"  found {len(cbs)} ControllerBox instance(s)")
    errors = 0
    for c in cbs:
        inst = c.get("instanceName", "")
        params = c.get("parameters", {})
        gp = params.get("generated_path", "")
        ig = _get(c.get("descriptor", {}), "InputPorts", "inputPorts", default=[])
        og = _get(c.get("descriptor", {}), "OutputPorts", "outputPorts", default=[])
        ae = params.get("auto_events", "")

        print(f"\n  [{inst}]")
        print(f"    generated_path = {gp!r}")
        print(f"    input_globals  = {len(ig)} ({ig[:3]}...)" if len(ig) > 3 else f"    input_globals  = {ig}")
        print(f"    output_globals = {len(og)} ({og[:3]}...)" if len(og) > 3 else f"    output_globals = {og}")
        print(f"    auto_events    = {ae!r}")

        # 1) path resolution
        gpath = ControllerBox._resolve_generated_path(gp if gp else None)
        print(f"    -> resolved: {gpath}")
        if not gpath.is_file():
            print(f"    [NG] generated module not found")
            errors += 1
            continue
        print(f"    [OK] module exists ({gpath.stat().st_size} bytes)")

        # 2) load + build_runtime
        try:
            spec = importlib.util.spec_from_file_location(
                f"_diag_{inst}", str(gpath))
            mod = importlib.util.module_from_spec(spec)
            spec.loader.exec_module(mod)
        except Exception as e:
            print(f"    [NG] import failed: {e}")
            errors += 1
            continue
        if not hasattr(mod, "build_runtime"):
            print(f"    [NG] no build_runtime() in module")
            errors += 1
            continue
        try:
            rt = mod.build_runtime()
        except Exception as e:
            print(f"    [NG] build_runtime() failed: {e}")
            errors += 1
            continue
        print(f"    [OK] runtime built ({len(rt.layers)} layers, {len(rt.globals)} globals)")

        # 3) I/O coverage
        rt_globals = set(rt.globals.keys())
        missing_in = [g for g in ig if g not in rt_globals]
        missing_out = [g for g in og if g not in rt_globals]
        if missing_in:
            print(f"    [NG] input_globals missing in runtime: {missing_in}")
            errors += 1
        else:
            print(f"    [OK] all {len(ig)} input_globals present")
        if missing_out:
            print(f"    [NG] output_globals missing in runtime: {missing_out}")
            errors += 1
        else:
            print(f"    [OK] all {len(og)} output_globals present")

        # 4) layer states after one tick
        try:
            rt.tick(0.0, 0.01, None)
            states = {l.layer_name: l.state for l in rt.layers}
            print(f"    layer states (after 1 tick): {states}")
        except Exception as e:
            print(f"    [NG] tick failed: {e}")
            errors += 1

    return 0 if errors == 0 else 1


def main():
    if len(sys.argv) < 2:
        print("usage: python tools/inspect_controller_box.py <file.psbproj> [...]")
        sys.exit(2)
    rc = 0
    for path in sys.argv[1:]:
        rc |= inspect(path)
    sys.exit(rc)


if __name__ == "__main__":
    main()