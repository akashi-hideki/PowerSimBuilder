"""Verify generator_load scenario."""

from __future__ import annotations

import json
from pathlib import Path

from python_runtime import SimulationData
from python_runtime.components import Engine, Generator


def _load(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


_FACTORIES = {
    "Engine": Engine,
    "Generator": Generator,
}


def _run_scenario(scenarios_dir: Path):
    cfg = _load(scenarios_dir / "generator_load" / "system.json")
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


def test_generator_reaches_expected_output(scenarios_dir: Path):
    comps = _run_scenario(scenarios_dir)
    engine = comps["engine"]
    gen = comps["gen"]

    torque = float(engine.outputs["torque"])
    v = float(gen.outputs["voltage"])
    i = float(gen.outputs["current"])
    p = float(gen.outputs["power"])

    assert torque > 19.0, f"engine torque {torque:.1f} too low"
    assert 175.0 < v < 185.0, f"generator voltage {v:.1f} out of [175, 185]"
    assert 17.0 < i < 19.0, f"generator current {i:.1f} out of [17, 19]"
    assert 3000.0 < p < 3500.0, f"generator power {p:.1f} out of [3000, 3500]"
