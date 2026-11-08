"""Simulate a .psbproj containing ControllerBox + PID + OvenChamber.

Runs the same component classes the GUI generates, wires connections,
and prints quantitative metrics (ss, overshoot, settle, duty stats).
"""
from __future__ import annotations
import sys, zipfile, json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from python_runtime.components import (
    OvenChamber, PIDController, FirstOrderLag, ThermalMass, ControllerBox,
)

COMPONENT_MAP = {
    "OvenChamber": OvenChamber,
    "PIDController": PIDController,
    "FirstOrderLag": FirstOrderLag,
    "ThermalMass": ThermalMass,
}


def _get(d, *names, default=None):
    for n in names:
        if isinstance(d, dict) and n in d:
            return d[n]
    return default


def parse_conn(s):
    parts = s.split(".")
    if len(parts) == 2:
        return parts[0], parts[1]
    if len(parts) == 3:
        return parts[0], parts[2]
    return None, None


def build(proj):
    comps = {}
    for c in proj.get("components", []):
        inst = _get(c, "instanceName", default="")
        desc = c.get("descriptor", {})
        ctype = _get(desc, "type", "Type", default="")
        params = c.get("parameters") or {}
        if ctype == "ControllerBox":
            ig = _get(desc, "InputPorts", default=[])
            og = _get(desc, "OutputPorts", default=[])
            obj = ControllerBox(inst, input_globals=ig, output_globals=og)
        else:
            cls = COMPONENT_MAP.get(ctype)
            if cls is None:
                print(f"  [skip] unknown type: {ctype!r}")
                continue
            obj = cls(inst)
        for k, v in params.items():
            obj.parameters[k] = v
        comps[inst] = obj

    conns = []
    for c in proj.get("connections", []):
        src = _get(c, "from", "From", default="")
        dst = _get(c, "to", "To", default="")
        si, sp = parse_conn(src)
        di, dp = parse_conn(dst)
        if si in comps and di in comps:
            conns.append((si, sp, di, dp))
    return comps, conns


def run(proj, t_end=None):
    meta = proj.get("metadata", {})
    dt = float(meta.get("dt", 0.001))
    if t_end is None:
        t_end = float(meta.get("tEnd", 5.0))
    comps, conns = build(proj)
    n = int(round(t_end / dt))
    log = {name: [] for name in comps}
    for i in range(n + 1):
        t = i * dt
        for obj in comps.values():
            obj.update(t, dt, None)
        for si, sp, di, dp in conns:
            comps[di].inputs[dp] = comps[si].outputs[sp]
        for name, obj in comps.items():
            log[name].append(dict(obj.outputs))
    return comps, conns, log, dt, t_end


def metrics(values, target):
    n = len(values)
    tail = values[int(n * 0.9):]
    ss = sum(tail) / len(tail)
    peak = max(values)
    ovs = max(0.0, (peak - target) / abs(target) * 100.0) if target else 0.0
    band = 0.02 * abs(target) if target else 0.01
    settle = -1
    for i in range(n - 1, -1, -1):
        if abs(values[i] - target) > band:
            settle = i
            break
    return dict(ss=ss, err=target - ss, peak=peak, ovs=ovs, settle=settle)


def _extract_bundled_components(zf, proj, extract_dir):
    """If a ControllerBox generated_path points to a non-existent file but the
    same basename exists under components/ inside the ZIP, extract it to
    extract_dir and rewrite the parameter. Returns dict of rewrites for log.
    """
    rewrites = {}
    for comp in proj.get("components", []):
        desc = comp.get("descriptor", {})
        if desc.get("type") != "ControllerBox":
            continue
        params = desc.get("parameters", {})
        gp = params.get("generated_path") or ""
        if not gp:
            continue
        if Path(gp).is_file():
            continue
        basename = Path(gp).name
        zip_key = f"components/{basename}"
        try:
            data = zf.read(zip_key)
        except KeyError:
            continue
        extract_dir.mkdir(parents=True, exist_ok=True)
        out = extract_dir / basename
        out.write_bytes(data)
        params["generated_path"] = str(out.resolve())
        rewrites[basename] = str(out)
    return rewrites


def main():
    if len(sys.argv) < 2:
        print("usage: python tools/verify_statable_psbproj.py <file.psbproj>")
        sys.exit(2)
    p = sys.argv[1]
    zf = zipfile.ZipFile(p)
    proj = json.loads(zf.read("project.json"))
    # Trap #93: v181-style .psbproj bundles components/*.py inside the ZIP
    # but project.json's generated_path still points to generated/. On a fresh
    # CI checkout that path doesn't exist -> extract from ZIP to a temp dir.
    import tempfile as _tf
    _tmp = Path(_tf.mkdtemp(prefix="psb_v181_"))
    _rewrites = _extract_bundled_components(zf, proj, _tmp)
    for name, path in _rewrites.items():
        print(f"[extract] {name} -> {path} (from ZIP components/)")
    comps, conns, log, dt, t_end = run(proj)

    print(f"=== {Path(p).name} ===")
    print(f"  t_end={t_end}s dt={dt} components={list(comps.keys())}")
    print(f"  connections: {len(conns)}")
    for si, sp, di, dp in conns:
        print(f"    {si}.{sp} -> {di}.{dp}")

    # Find setpoint from PID
    setpoint = None
    for name, obj in comps.items():
        if "PID" in name or "Controller" in name and hasattr(obj, "parameters"):
            sp = obj.parameters.get("setpoint")
            if sp is not None and sp > 0:
                setpoint = float(sp)
                break

    print(f"\n  target (from PID setpoint): {setpoint}")
    print(f"  {'signal':<45}{'ss':>10}{'err':>9}{'peak':>10}{'ovs%':>8}{'settle(s)':>12}")
    print("  " + "-" * 94)

    # Temperature
    for name in comps:
        if "Oven" in name or "Thermal" in name or "FirstOrderLag" in name:
            port = "oven_current_temp" if "oven_current_temp" in comps[name].outputs else "temperature"
            if port not in comps[name].outputs:
                continue
            vals = [row.get(port, 0.0) for row in log[name]]
            tgt = setpoint or vals[-1]
            m = metrics(vals, tgt)
            settle_s = m["settle"] * dt if m["settle"] >= 0 else -1.0
            print(f"  {name}.{port:<35}{m['ss']:>10.3f}{m['err']:>9.3f}{m['peak']:>10.3f}"
                  f"{m['ovs']:>8.2f}{settle_s:>12.1f}")

    # Duty signals
    for name in comps:
        for port in ("heater_top_duty", "control"):
            if port in comps[name].outputs:
                vals = [row.get(port, 0.0) for row in log[name]]
                ss_duty = sum(vals[int(len(vals)*0.9):]) / max(1, len(vals) - int(len(vals)*0.9))
                print(f"  {name}.{port:<35}{ss_duty:>10.3f}{'':>9}{max(vals):>10.3f}")

    # Layer states (ControllerBox)
    for name, obj in comps.items():
        if isinstance(obj, ControllerBox):
            states = {l.layer_name: l.state for l in obj._runtime.layers}
            print(f"\n  {name} final states: {states}")


if __name__ == "__main__":
    main()