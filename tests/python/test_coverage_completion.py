"""Additional tests to reach 100% coverage.

Targets missing lines identified by pytest --cov.
"""

from __future__ import annotations

from python_runtime.component_base import Component
from python_runtime.components import (
    AnalogInput,
    Counter,
    PIDController,
    Timer,
)
from python_runtime.logger import plot
from python_runtime.simulation_data import SimulationData
from python_runtime.simulation_runner import SimulationRunner

# --- analog_input.py: v_max == v_min, alpha<=0 / alpha>=1 on second step ---


def test_analog_input_zero_range():
    c = AnalogInput("ain")
    c.parameters["v_min"] = 2.5
    c.parameters["v_max"] = 2.5
    c.inputs["raw"] = 2.5
    c.update(0.0, 0.001, None)
    assert c.outputs["scaled"] == 0.0


def test_analog_input_alpha_zero_after_first_step():
    c = AnalogInput("ain")
    c.parameters["filter_alpha"] = 0.0
    c.inputs["raw"] = 5.0
    c.update(0.0, 0.001, None)
    c.inputs["raw"] = 0.0
    c.update(0.001, 0.001, None)
    assert c.outputs["filtered"] == 0.0


def test_analog_input_alpha_one_after_first_step():
    c = AnalogInput("ain")
    c.parameters["filter_alpha"] = 1.0
    c.inputs["raw"] = 5.0
    c.update(0.0, 0.001, None)
    c.inputs["raw"] = 0.0
    c.update(0.001, 0.001, None)
    assert c.outputs["filtered"] == 0.0


# --- counter.py: direction=down, wrap=True ---


def test_counter_down_wrap():
    c = Counter("cnt")
    c.parameters["direction"] = "down"
    c.parameters["min_count"] = 0
    c.parameters["max_count"] = 5
    c.parameters["initial"] = 0
    c.parameters["wrap"] = True
    c.reset()
    c.inputs["clock"] = True
    c.update(0.0, 0.1, None)
    c.inputs["clock"] = False
    c.update(0.01, 0.1, None)
    assert c.outputs["count"] == 5
    assert c.outputs["underflow"] is False


# --- pid_controller.py: anti_windup + saturation, and dt=0 ---


def test_pid_anti_windup_blocks_integral():
    c = PIDController("pid")
    c.parameters["kp"] = 10.0
    c.parameters["ki"] = 1.0
    c.parameters["out_min"] = -1.0
    c.parameters["out_max"] = 1.0
    c.parameters["anti_windup"] = True
    c.parameters["setpoint"] = 1.0
    c.inputs["measured"] = 0.0
    c.update(0.0, 0.1, None)
    assert c.outputs["saturated"] is True
    c.update(0.1, 0.1, None)
    assert c.outputs["i_term"] == 0.0


def test_pid_zero_dt():
    c = PIDController("pid")
    c.parameters["kp"] = 0.0
    c.parameters["ki"] = 0.0
    c.parameters["kd"] = 1.0
    c.parameters["setpoint"] = 1.0
    c.inputs["measured"] = 0.0
    c.update(0.0, 0.0, None)
    assert c.outputs["d_term"] == 0.0


# --- timer.py: interval mode reset, and toggling twice ---


def test_timer_interval_reset():
    c = Timer("tmr")
    c.parameters["mode"] = "interval"
    c.parameters["duration"] = 0.1
    c.inputs["trigger"] = True
    c.update(0.0, 0.1, None)
    c.inputs["trigger"] = False
    c.update(0.1, 0.1, None)
    assert c.outputs["done"] is False
    assert c.outputs["running"] is False
    assert c.outputs["elapsed"] == 0.0


def test_timer_interval_toggles_twice():
    c = Timer("tmr")
    c.parameters["mode"] = "interval"
    c.parameters["duration"] = 0.1
    c.inputs["trigger"] = True
    c.update(0.0, 0.1, None)
    c.update(0.1, 0.1, None)
    c.update(0.2, 0.1, None)
    assert c.outputs["done"] is True


# --- simulation_data.py: series with t before first entry ---


def test_get_stimulus_before_first_entry():
    sd = SimulationData()
    sd.set_stimulus("c.inputs.x", [(0.5, 10), (1.0, 20)])
    assert sd.get_stimulus_value("c.inputs.x", 0.0) is None
    assert sd.get_stimulus_value("c.inputs.x", 0.5) == 10
    assert sd.get_stimulus_value("c.inputs.x", 2.0) == 20


# --- simulation_runner.py: invalid connection paths ---


def test_propagate_invalid_path_length():
    r = SimulationRunner()
    c = Component("c1", "Dummy")
    r.add_component(c)
    r.add_connection("c1.outputs", "c1.inputs")
    r.step(0.0)


def test_propagate_wrong_sections():
    r = SimulationRunner()
    c = Component("c1", "Dummy")
    c.outputs["y"] = 1.0
    r.add_component(c)
    r.add_connection("c1.inputs.x", "c1.outputs.y")
    r.step(0.0)
    assert "x" not in c.inputs


def test_propagate_missing_dst_component():
    r = SimulationRunner()
    c = Component("c1", "Dummy")
    c.outputs["y"] = 1.0
    r.add_component(c)
    r.add_connection("c1.outputs.y", "ghost.inputs.x")
    r.step(0.0)


# --- logger.py: no numeric lines, default keys ---


def test_plot_no_numeric_lines(tmp_path):
    sd = SimulationData()
    c = Component("c1", "Dummy")
    c.outputs["y"] = "not a number"
    sd.log(0.0, {"c1": c})
    sd.log(0.1, {"c1": c})
    out = tmp_path / "no_lines.png"
    plot(sd, keys=["c1.outputs.y"], output_path=out)
    assert out.exists()


def test_plot_default_keys(tmp_path):
    sd = SimulationData()
    c = Component("c1", "Dummy")
    c.outputs["y"] = 1.0
    c.outputs["z"] = 2.0
    sd.log(0.0, {"c1": c})
    sd.log(0.1, {"c1": c})
    out = tmp_path / "auto.png"
    plot(sd, output_path=out)
    assert out.exists()
