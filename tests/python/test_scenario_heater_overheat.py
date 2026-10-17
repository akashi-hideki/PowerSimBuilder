"""Verify heater_overheat scenario (system.json driven)."""

from __future__ import annotations

import json
from pathlib import Path

from python_runtime import SimulationData
from python_runtime.components import HeaterBlock, OverCurrentDetector, ThreePhaseSine


def _load(path: Path) -> dict:
    return json.loads(path.read_text(encoding="utf-8"))


_FACTORIES = {
    "ThreePhaseSine": ThreePhaseSine,
    "HeaterBlock": HeaterBlock,
    "OverCurrentDetector": OverCurrentDetector,
}


def test_heater_overheats_and_ocd_trips(scenarios_dir: Path):
    cfg = _load(scenarios_dir / "heater_overheat" / "system.json")
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

    heater = components["heater"]
    ocd = components["ocd"]
    final_temp = float(heater.outputs["temperature"])
    tripped = float(ocd.outputs["tripped"])
    peak = float(ocd.outputs["peak"])

    assert final_temp > 50.0, f"heater temp {final_temp:.1f} too low (expected > 50)"
    assert tripped == 1.0, f"OCD did not trip (tripped={tripped})"
    assert peak > 5.0, f"OCD peak {peak:.2f} A too low (expected > 5)"
