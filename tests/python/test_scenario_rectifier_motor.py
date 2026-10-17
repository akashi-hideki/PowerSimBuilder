"""Verify rectifier_motor scenario."""

from __future__ import annotations

import json
from pathlib import Path

from python_runtime import SimulationData
from python_runtime.components import DiodeBridge, InductionMotor, PowerSource


def _load(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


_FACTORIES = {
    "PowerSource": PowerSource,
    "DiodeBridge": DiodeBridge,
    "InductionMotor": InductionMotor,
}


def _run_scenario(scenarios_dir: Path):
    cfg = _load(scenarios_dir / "rectifier_motor" / "system.json")
    dt = float(cfg["dt"])
    t_end = float(cfg["t_end"])

    components = {}
    for c in cfg["components"]:
        cls = _FACTORIES[c["descriptorType"]]
        inst = cls(c["instanceName"])
        inst.parameters.update(c.get("parameters", {}))
        components[c["instanceName"]] = inst

    sim = SimulationData(time_step=dt)
    n_steps = int(round(t_end / dt))
    for i in range(n_steps + 1):
        t = i * dt
        for comp in components.values():
            comp.update(t, dt, sim)
        for conn in cfg["connections"]:
            src_id, _, src_port = conn["from"].split(".")
            dst_id, _, dst_port = conn["to"].split(".")
            components[dst_id].inputs[dst_port] = components[src_id].outputs[src_port]

    return components


def test_motor_accelerates_from_rectified_ac(scenarios_dir: Path):
    comps = _run_scenario(scenarios_dir)
    motor = comps["motor"]

    speed = float(motor.outputs["speed"])
    slip = float(motor.outputs["slip"])

    assert speed > 100.0, f"motor speed {speed:.1f} rpm too low (expected > 100)"
    assert slip < 0.9, f"motor slip {slip:.2f} too high (expected < 0.9)"


def test_rectifier_produces_dc(scenarios_dir: Path):
    cfg = _load(scenarios_dir / "rectifier_motor" / "system.json")
    dt = float(cfg["dt"])
    t_end = float(cfg["t_end"])

    components = {}
    for c in cfg["components"]:
        cls = _FACTORIES[c["descriptorType"]]
        inst = cls(c["instanceName"])
        inst.parameters.update(c.get("parameters", {}))
        components[c["instanceName"]] = inst

    sim = SimulationData(time_step=dt)
    n_steps = int(round(t_end / dt))
    peak_dc = 0.0
    for i in range(n_steps + 1):
        t = i * dt
        for comp in components.values():
            comp.update(t, dt, sim)
        for conn in cfg["connections"]:
            src_id, _, src_port = conn["from"].split(".")
            dst_id, _, dst_port = conn["to"].split(".")
            components[dst_id].inputs[dst_port] = components[src_id].outputs[src_port]
        peak_dc = max(peak_dc, float(components["rect"].outputs["dc_voltage"]))

    # Peak AC = 200 * sqrt(2) = 282.84, minus 2*0.7 = 281.4
    assert peak_dc > 270.0, f"rectifier peak DC {peak_dc:.1f} too low (expected > 270)"
