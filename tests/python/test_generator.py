"""Tests for Generator."""

from __future__ import annotations

import pytest

from python_runtime.components import Generator


def test_zero_torque_zero_output():
    g = Generator("g")
    g.inputs["torque"] = 0.0
    g.update(0.0, 0.001, None)
    assert g.outputs["voltage"] == 0.0
    assert g.outputs["current"] == 0.0
    assert g.outputs["power"] == 0.0


def test_voltage_proportional_to_torque():
    g = Generator("g")
    g.parameters["efficiency"] = 0.9
    g.parameters["voltage_per_torque"] = 10.0
    g.parameters["load_resistance"] = 10.0
    g.inputs["torque"] = 10.0
    g.update(0.0, 0.001, None)
    # V = 0.9 * 10 * 10 = 90 V
    assert g.outputs["voltage"] == pytest.approx(90.0)
    assert g.outputs["current"] == pytest.approx(9.0)
    assert g.outputs["power"] == pytest.approx(810.0)


def test_voltage_capped_at_rated():
    g = Generator("g")
    g.parameters["rated_voltage"] = 200.0
    g.inputs["torque"] = 100.0  # would be 900 V without cap
    g.update(0.0, 0.001, None)
    assert g.outputs["voltage"] == pytest.approx(200.0)


def test_power_capped_at_rated():
    g = Generator("g")
    g.parameters["rated_power"] = 500.0
    g.parameters["rated_voltage"] = 200.0
    g.parameters["load_resistance"] = 1.0
    g.inputs["torque"] = 100.0
    g.update(0.0, 0.001, None)
    assert g.outputs["power"] == pytest.approx(500.0)


def test_zero_resistance_current_zero():
    g = Generator("g")
    g.parameters["load_resistance"] = 0.0
    g.inputs["torque"] = 10.0
    g.update(0.0, 0.001, None)
    assert g.outputs["current"] == 0.0
    assert g.outputs["power"] == 0.0


def test_reset_clears_outputs():
    g = Generator("g")
    g.inputs["torque"] = 10.0
    g.update(0.0, 0.001, None)
    assert g.outputs["voltage"] > 0.0
    g.reset()
    assert g.outputs["voltage"] == 0.0
    assert g.inputs["torque"] == 0.0


def test_engine_drives_generator():
    from python_runtime.components import Engine

    e = Engine("e")
    g = Generator("g")
    e.parameters["torque_max"] = 20.0
    e.parameters["ramp_time"] = 0.1
    e.parameters["idle_ratio"] = 0.0
    for i in range(201):
        t = i * 0.001
        e.update(t, 0.001, None)
        g.inputs["torque"] = e.outputs["torque"]
        g.update(t, 0.001, None)
    # At t=0.2, engine torque = 20, V = 0.9 * 20 * 10 = 180
    assert g.outputs["voltage"] == pytest.approx(180.0)
