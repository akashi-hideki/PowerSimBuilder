"""Tests for ThermalMass."""

from __future__ import annotations

import pytest

from python_runtime.components import ThermalMass


def test_initial_temperature():
    tm = ThermalMass("tm")
    tm.parameters["initial_temperature"] = 30.0
    tm.reset()
    assert tm.outputs["temperature"] == 30.0


def test_heat_flow_raises_temperature():
    tm = ThermalMass("tm")
    tm.parameters["initial_temperature"] = 25.0
    tm.parameters["ambient_temperature"] = 25.0
    tm.parameters["thermal_capacity"] = 1000.0
    tm.parameters["thermal_resistance"] = 1.0
    tm.inputs["heat_flow"] = 100.0

    T0 = tm.outputs["temperature"]
    tm.update(0.0, 1.0, None)
    T1 = tm.outputs["temperature"]
    assert T1 > T0


def test_steady_state():
    tm = ThermalMass("tm")
    tm.parameters["ambient_temperature"] = 25.0
    tm.parameters["thermal_resistance"] = 1.0
    tm.parameters["thermal_capacity"] = 1.0  # tau = 1 s, fast convergence
    tm.inputs["heat_flow"] = 10.0

    # 10 * tau = 10 s of simulation
    for k in range(1000):
        tm.update(k * 0.01, 0.01, None)

    # Steady state: T = T_amb + Q*R = 25 + 10*1 = 35
    assert abs(tm.outputs["temperature"] - 35.0) < 0.1
