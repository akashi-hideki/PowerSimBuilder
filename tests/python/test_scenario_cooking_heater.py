"""Verify cooking_heater scenario end-to-end."""

from __future__ import annotations

import importlib.util
from pathlib import Path

from python_runtime import SimulationData
from python_runtime.components import ControllerBox, Gain, HeaterBlock


def _load_module(path: Path, name: str):
    spec = importlib.util.spec_from_file_location(name, str(path))
    assert spec is not None and spec.loader is not None
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def test_runtime_has_seven_layers(scenarios_dir: Path):
    controller_py = scenarios_dir / "cooking_heater" / "controller_box_demo.py"
    assert controller_py.is_file(), controller_py
    mod = _load_module(controller_py, "cooking_heater_controller")
    assert hasattr(mod, "build_runtime"), "generated module has no build_runtime()"
    rt = mod.build_runtime()
    assert len(rt.layers) == 7, f"expected 7 layers, got {len(rt.layers)}"
    layer_names = [layer.layer_name for layer in rt.layers]
    for expected in ("DriverInput", "DriverOutput", "Application"):
        assert expected in layer_names, f"missing layer: {expected}"


def test_heater_warms_up(scenarios_dir: Path):
    controller_py = scenarios_dir / "cooking_heater" / "controller_box_demo.py"
    assert controller_py.is_file(), controller_py

    dt = 0.001
    t_end = 5.0
    sim = SimulationData(time_step=dt)

    box = ControllerBox("box", generated_path=str(controller_py))
    box.parameters["auto_events"] = "demo_oven"  # drive heater_top_duty

    gain = Gain("gain")
    gain.parameters["gain"] = 2.0  # V = duty(100) * gain(2) = 200 V
    gain.parameters["bias"] = 0.0

    heater = HeaterBlock("heater")
    heater.parameters["rated_power"] = 1000.0
    heater.parameters["rated_voltage"] = 200.0
    heater.parameters["thermal_resistance"] = 1.0
    heater.parameters["thermal_capacity"] = 100.0  # tau = R*C = 100 s
    heater.parameters["ambient_temperature"] = 25.0
    heater.parameters["initial_temperature"] = 25.0

    components = {"box": box, "gain": gain, "heater": heater}
    connections = [
        ("box", "heater_top_duty", "gain", "input"),
        ("gain", "output", "heater", "voltage"),
    ]

    n_steps = int(round(t_end / dt))
    for i in range(n_steps + 1):
        t = i * dt
        for comp in components.values():
            comp.update(t, dt, sim)
        for src_id, src_port, dst_id, dst_port in connections:
            components[dst_id].inputs[dst_port] = components[src_id].outputs[src_port]

    final_temp = float(heater.outputs["temperature"])
    assert (
        final_temp > 50.0
    ), f"heater only warmed to {final_temp:.1f} degC (expected > 50, from 25)"
