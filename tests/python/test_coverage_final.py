"""Final coverage completion tests."""

from __future__ import annotations

from python_runtime.component_base import Component
from python_runtime.components import AnalogInput, PIDController
from python_runtime.logger import plot
from python_runtime.simulation_data import SimulationData

# --- analog_input.reset() ---


def test_analog_input_reset():
    c = AnalogInput("ain")
    c.inputs["raw"] = 5.0
    c.update(0.0, 0.001, None)
    c.reset()
    assert c.inputs == {"raw": 0.0}
    assert c.outputs["filtered"] == 0.0
    assert c._filtered is None


# --- pid_controller.reset() ---


def test_pid_controller_reset():
    c = PIDController("pid")
    c.parameters["ki"] = 1.0
    c.parameters["setpoint"] = 1.0
    c.inputs["measured"] = 0.0
    c.update(0.0, 0.1, None)
    c.update(0.1, 0.1, None)
    assert c._integral != 0.0
    c.reset()
    assert c._integral == 0.0
    assert c._prev_error == 0.0
    assert c._initialized is False


# --- logger.plot: invalid key (not 3 parts) ---


def test_plot_invalid_key_is_skipped(tmp_path):
    sd = SimulationData()
    c = Component("c1", "Dummy")
    c.outputs["y"] = 1.0
    sd.log(0.0, {"c1": c})
    sd.log(0.1, {"c1": c})
    out = tmp_path / "invalid_key.png"
    # Include a malformed key that should be silently skipped
    plot(sd, keys=["c1.outputs.y", "invalid", "too.many.parts.here"], output_path=out)
    assert out.exists()


# --- logger.plot: show=True (uses Agg backend for safety) ---


def test_plot_show_true(tmp_path):
    import matplotlib

    matplotlib.use("Agg")

    sd = SimulationData()
    c = Component("c1", "Dummy")
    c.outputs["y"] = 1.0
    sd.log(0.0, {"c1": c})
    sd.log(0.1, {"c1": c})
    # show=True path; Agg backend does not open a window
    plot(sd, keys=["c1.outputs.y"], show=True)


# --- simulation_data.apply_stimulus: invalid path skipped ---


def test_apply_stimulus_invalid_path_skipped():
    sd = SimulationData()
    c = Component("c1", "Dummy")
    # Register a malformed path that should be skipped
    sd.stimulus["invalid"] = ("callable", lambda t: 1)
    sd.apply_stimulus({"c1": c}, 0.0)
    assert c.inputs == {}
