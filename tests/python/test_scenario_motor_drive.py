"""Verify motor_drive scenario (system.json driven)."""

from __future__ import annotations

import json
from pathlib import Path

from python_runtime import SimulationData
from python_runtime.components import Gain, MechanicalInertia, PWM3Phase


def _load(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


_FACTORIES = {
    "PWM3Phase": PWM3Phase,
    "Gain": Gain,
    "MechanicalInertia": MechanicalInertia,
}


def test_motor_reaches_speed(scenarios_dir: Path):
    cfg = _load(scenarios_dir / "motor_drive" / "system.json")
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

    motor = components["motor"]
    speed = float(motor.outputs["speed"])
    rpm = float(motor.outputs["speed_rpm"])
    assert speed > 30.0, f"final speed {speed:.2f} rad/s too low (expected > 30)"
    assert rpm > 200.0, f"final rpm {rpm:.1f} too low (expected > 200)"
