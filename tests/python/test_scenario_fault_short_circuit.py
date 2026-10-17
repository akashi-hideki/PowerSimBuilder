"""Verify fault_short_circuit scenario."""

from __future__ import annotations

import json
from pathlib import Path

from python_runtime import SimulationData
from python_runtime.components import Engine, FaultDevice, Generator


def _load(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


_FACTORIES = {
    "Engine": Engine,
    "Generator": Generator,
    "FaultDevice": FaultDevice,
}


def _run_scenario(scenarios_dir: Path):
    cfg = _load(scenarios_dir / "fault_short_circuit" / "system.json")
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


def test_fault_triggers_and_latches(scenarios_dir: Path):
    comps = _run_scenario(scenarios_dir)
    fault = comps["fault"]

    assert (
        bool(fault.outputs["short_circuit"]) is True
    ), "fault.short_circuit should be latched True at t_end"
    assert bool(fault.outputs["open_circuit"]) is False


def test_power_chain_unaffected(scenarios_dir: Path):
    comps = _run_scenario(scenarios_dir)
    engine = comps["engine"]
    gen = comps["gen"]

    torque = float(engine.outputs["torque"])
    v = float(gen.outputs["voltage"])

    assert torque > 19.0, f"engine torque {torque:.1f} affected by fault (expected ~20)"
    assert 175.0 < v < 185.0, f"generator voltage {v:.1f} affected by fault"
