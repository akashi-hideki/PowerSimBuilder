"""verify_psbproj.py - Simulate .psbproj files directly in Python.

Accepts both connection formats:
  - "instance.port"            (2 parts, .psbproj schema v2)
  - "instance.section.port"    (3 parts, generated code style)
"""
from __future__ import annotations
import sys, zipfile, json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))

from python_runtime.components import OvenChamber, PIDController, FirstOrderLag


COMPONENT_MAP = {
    "OvenChamber": OvenChamber,
    "PIDController": PIDController,
    "FirstOrderLag": FirstOrderLag,
}


def _get(d, *names, default=None):
    for n in names:
        if isinstance(d, dict) and n in d:
            return d[n]
    return default


def parse_conn(s):
    """Return (instance, port). Accept 'inst.port' or 'inst.section.port'."""
    parts = s.split(".")
    if len(parts) == 2:
        return parts[0], parts[1]
    if len(parts) == 3:
        return parts[0], parts[2]
    return None, None


def load_psbproj(path):
    with zipfile.ZipFile(path) as zf:
        return json.loads(zf.read("project.json"))


def instantiate_and_run(proj, t_end=None):
    meta = proj.get("metadata", {})
    dt = float(meta.get("dt", 0.001))
    if t_end is None:
        t_end = float(meta.get("tEnd", 5.0))

    comps = {}
    for c in proj.get("components", []):
        inst = _get(c, "instanceName", default="")
        desc = c.get("descriptor", {})
        ctype = _get(desc, "type", "Type", default="")
        cls = COMPONENT_MAP.get(ctype)
        if cls is None:
            print(f"  [skip] unknown type: {ctype!r}")
            continue
        obj = cls(inst)
        for k, v in (c.get("parameters") or {}).items():
            obj.parameters[k] = v
        comps[inst] = obj

    conns = []
    for c in proj.get("connections", []):
        src = _get(c, "from", "From", default="")
        dst = _get(c, "to", "To", default="")
        if not src or not dst:
            continue
        s_inst, s_port = parse_conn(src)
        d_inst, d_port = parse_conn(dst)
        if s_inst is None or d_inst is None:
            print(f"  [bad conn] {src} -> {dst}")
            continue
        if s_inst not in comps or d_inst not in comps:
            print(f"  [missing comp] {src} -> {dst} (have: {list(comps.keys())})")
            continue
        conns.append((s_inst, s_port, d_inst, d_port))

    print(f"  parsed {len(conns)} connection(s):")
    for s_inst, s_port, d_inst, d_port in conns:
        print(f"    {s_inst}.{s_port} -> {d_inst}.{d_port}")

    n = int(round(t_end / dt))
    log = {name: [] for name in comps}
    for i in range(n + 1):
        t = i * dt
        for obj in comps.values():
            obj.update(t, dt, None)
        for s_inst, s_port, d_inst, d_port in conns:
            comps[d_inst].inputs[d_port] = comps[s_inst].outputs[s_port]
        for name, obj in comps.items():
            log[name].append(dict(obj.outputs))

    return comps, log, dt, t_end


def metrics(values, target):
    n = len(values)
    tail = values[int(n * 0.9):]
    ss = sum(tail) / len(tail)
    peak = max(values)
    ovs = max(0.0, (peak - target) / abs(target) * 100.0) if target != 0 else 0.0
    band = 0.02 * abs(target) if target != 0 else 0.01
    settle = -1
    for i in range(n - 1, -1, -1):
        if abs(values[i] - target) > band:
            settle = i
            break
    return dict(ss=ss, err=target - ss, peak=peak, ovs=ovs, settle=settle)


def run_case(label, path, outputs_of_interest, targets):
    print(f"\n=== {label} ===")
    print(f"file: {path}")
    proj = load_psbproj(path)
    comps, log, dt, t_end = instantiate_and_run(proj)
    print(f"  t_end={t_end}s dt={dt} components={list(comps.keys())}")
    print(f"  {'signal':<40}{'ss':>10}{'err':>9}{'peak':>10}{'ovs%':>8}{'settle(s)':>12}")
    print("  " + "-" * 88)
    for inst, port in outputs_of_interest:
        vals = [row.get(port, 0.0) for row in log[inst]]
        tgt = targets.get(f"{inst}.{port}", vals[-1])
        m = metrics(vals, tgt)
        settle_s = m["settle"] * dt if m["settle"] >= 0 else -1.0
        print(f"  {inst}.{port:<35}{m['ss']:>10.3f}{m['err']:>9.3f}{m['peak']:>10.3f}"
              f"{m['ovs']:>8.2f}{settle_s:>12.1f}")


def main():
    run_case(
        "oven_control.psbproj (PID + OvenChamber)",
        "examples/demos/oven_control.psbproj",
        [("OvenChamber", "oven_current_temp"), ("PID Controller", "control")],
        {"OvenChamber.oven_current_temp": 200.0},
    )
    run_case(
        "pid_closed_loop.psbproj (PID + FirstOrderLag)",
        "examples/demos/pid_closed_loop.psbproj",
        [("First Order Lag", "output"), ("PID Controller", "control")],
        {"First Order Lag.output": 1.0},
    )


if __name__ == "__main__":
    main()