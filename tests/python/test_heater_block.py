"""Tests for HeaterBlock."""

from __future__ import annotations

from python_runtime.components import HeaterBlock


def test_power_at_rated_voltage():
    ht = HeaterBlock("ht")
    ht.parameters["rated_power"] = 1000.0
    ht.parameters["rated_voltage"] = 200.0
    ht.inputs["voltage"] = 200.0
    ht.update(0.0, 0.01, None)
    assert abs(ht.outputs["power"] - 1000.0) < 1e-6


def test_power_scales_with_voltage_squared():
    ht = HeaterBlock("ht")
    ht.parameters["rated_power"] = 1000.0
    ht.parameters["rated_voltage"] = 200.0
    ht.inputs["voltage"] = 100.0  # half voltage
    ht.update(0.0, 0.01, None)
    # P = V^2 / R, R = 40, so 100^2/40 = 250
    assert abs(ht.outputs["power"] - 250.0) < 1e-6


def test_temperature_rises_over_time():
    ht = HeaterBlock("ht")
    ht.parameters["rated_power"] = 1000.0
    ht.parameters["rated_voltage"] = 200.0
    ht.parameters["thermal_capacity"] = 1000.0
    ht.parameters["thermal_resistance"] = 1.0
    ht.parameters["ambient_temperature"] = 25.0
    ht.parameters["initial_temperature"] = 25.0
    ht.inputs["voltage"] = 200.0

    T0 = ht.outputs["temperature"]
    for k in range(100):
        ht.update(k * 0.01, 0.01, None)
    T1 = ht.outputs["temperature"]
    assert T1 > T0


def test_zero_voltage_cools_to_ambient():
    ht = HeaterBlock("ht")
    ht.parameters["ambient_temperature"] = 25.0
    ht.outputs["temperature"] = 100.0
    ht.inputs["voltage"] = 0.0
    for k in range(1000):
        ht.update(k * 0.01, 0.01, None)
    assert ht.outputs["temperature"] < 100.0
    assert ht.outputs["temperature"] > 25.0
