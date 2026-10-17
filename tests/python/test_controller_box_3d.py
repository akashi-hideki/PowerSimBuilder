"""Tests for Task 3-D: F_ReadSensorsMovingAvg sensor overwrite fix.

Verify that thermistor_* (from physical models) are not overwritten by
F_ReadSensorsMovingAvg, while ir_value/steam_sensor (no physical model)
are still updated with pseudo-noise.
"""

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
def driver_input():
    from python_runtime.components import ControllerBox

    ctrl = ControllerBox("ctrl", generated_path=str(GENERATED))
    ctrl._ensure_runtime()
    return next(layer for layer in ctrl._runtime.layers if layer.layer_name == "DriverInput")


def test_read_sensors_does_not_overwrite_thermistors(driver_input):
    """Task 3-D: thermistor_* must NOT be changed by F_ReadSensorsMovingAvg."""
    driver_input.globals["thermistor_top"] = 250.0
    driver_input.globals["thermistor_bottom"] = 240.0
    driver_input.globals["thermistor_middle"] = 245.0
    driver_input.globals["g_system_tick"] = 1000

    driver_input.F_ReadSensorsMovingAvg()

    assert driver_input.globals["thermistor_top"] == 250.0
    assert driver_input.globals["thermistor_bottom"] == 240.0
    assert driver_input.globals["thermistor_middle"] == 245.0


def test_read_sensors_updates_ir_steam(driver_input):
    """ir_value and steam_sensor are still updated with pseudo-noise."""
    driver_input.globals["ir_value"] = 0
    driver_input.globals["steam_sensor"] = 0
    driver_input.globals["g_system_tick"] = 1

    driver_input.F_ReadSensorsMovingAvg()

    assert driver_input.globals["ir_value"] > 0
    assert driver_input.globals["steam_sensor"] > 0


def test_read_sensors_is_deterministic(driver_input):
    """Two calls with same g_system_tick yield same ir_value."""
    driver_input.globals["ir_value"] = 0
    driver_input.globals["g_system_tick"] = 42
    driver_input.F_ReadSensorsMovingAvg()
    first = driver_input.globals["ir_value"]

    driver_input.globals["ir_value"] = 0
    driver_input.globals["g_system_tick"] = 42
    driver_input.F_ReadSensorsMovingAvg()
    second = driver_input.globals["ir_value"]

    assert first == second


def test_thermistor_top_drives_overheat_detection():
    """End-to-end: thermistor_top from physics triggers MwOven overheat."""
    from python_runtime.components import ControllerBox

    ctrl = ControllerBox("ctrl", generated_path=str(GENERATED))
    ctrl._ensure_runtime()

    di = next(layer for layer in ctrl._runtime.layers if layer.layer_name == "DriverInput")

    ctrl._globals["thermistor_top"] = 300.0
    di.F_ReadSensorsMovingAvg()
    assert ctrl._globals["thermistor_top"] == 300.0

    oven = next(layer for layer in ctrl._runtime.layers if layer.layer_name == "MwOven")
    assert oven.F_CheckOvenOverheat() == 1
