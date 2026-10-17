"""Tests for ControllerBox component (Task 3-C-1/3)."""

from __future__ import annotations

from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
GENERATED = ROOT / "generated" / "controller_box_powersim.py"

pytestmark = pytest.mark.skipif(
    not GENERATED.is_file(),
    reason="generated/controller_box_powersim.py not found",
)


@pytest.fixture
def controller_box():
    from python_runtime.components import ControllerBox

    return ControllerBox("ctrl1", generated_path=str(GENERATED))


@pytest.fixture
def controller_box_auto():
    """ControllerBox without explicit path - uses auto-discovery."""
    from python_runtime.components import ControllerBox

    return ControllerBox("ctrl_auto")


def test_lazy_loading_defers_runtime(controller_box_auto):
    """Runtime is None until first access."""
    assert controller_box_auto._runtime is None
    states = controller_box_auto.get_layer_states()
    assert controller_box_auto._runtime is not None
    assert len(states) == 7


def test_auto_discovery(controller_box_auto):
    """Auto-discovery finds generated/controller_box_powersim.py."""
    states = controller_box_auto.get_layer_states()
    assert len(states) == 7


def test_layer_count(controller_box):
    states = controller_box.get_layer_states()
    assert len(states) == 7
    expected = {
        "DriverInput",
        "DriverOutput",
        "Application",
        "MwMicrowave",
        "MwGrill",
        "MwOven",
        "MwSteam",
    }
    assert set(states.keys()) == expected


def test_input_output_ports_present(controller_box):
    for name in (
        "thermistor_top",
        "thermistor_bottom",
        "thermistor_middle",
        "door_open",
        "mw_anode_current",
    ):
        assert name in controller_box.inputs, f"missing input port: {name}"
    for name in ("heater_top_duty", "fan_duty", "mag_on", "error_code"):
        assert name in controller_box.outputs, f"missing output port: {name}"


def test_update_propagates_globals_to_outputs(controller_box):
    """Setting a global after runtime load propagates on next update."""
    controller_box.update(0.001, 0.001, None)  # load runtime
    controller_box._globals["heater_top_duty"] = 42
    controller_box.update(0.001, 0.001, None)
    assert controller_box.outputs["heater_top_duty"] == 42


def test_update_ticks_state_machine(controller_box):
    controller_box.update(0.5, 0.001, None)
    assert controller_box._globals["g_system_tick"] == 500


def test_overheat_detection_rolefunction(controller_box):
    """Direct RoleFunction test: F_CheckOvenOverheat."""
    controller_box.update(0.001, 0.001, None)
    controller_box._globals["thermistor_top"] = 300.0
    oven = next(layer for layer in controller_box._runtime.layers if layer.layer_name == "MwOven")
    assert oven.F_CheckOvenOverheat() == 1


def test_reset_restores_ports(controller_box):
    controller_box.update(0.001, 0.001, None)
    controller_box.inputs["thermistor_top"] = 999.0
    controller_box.reset()
    assert "thermistor_top" in controller_box.inputs


def test_integration_with_simulation_runner():
    """ControllerBox works within SimulationRunner."""
    from python_runtime import SimulationRunner
    from python_runtime.components import ControllerBox

    runner = SimulationRunner(dt=0.01)
    ctrl = ControllerBox("ctrl", generated_path=str(GENERATED))
    runner.add_component(ctrl)

    # Run 10 ticks
    for i in range(10):
        runner.step(i * 0.01)

    # g_system_tick advances via integration (last step at t=0.09s => 90ms)
    assert ctrl._globals["g_system_tick"] == 90
    assert "heater_top_duty" in ctrl.outputs
