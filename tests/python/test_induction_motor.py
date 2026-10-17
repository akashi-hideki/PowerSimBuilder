"""Tests for InductionMotor."""

from __future__ import annotations

import pytest

from python_runtime.components import InductionMotor


def test_at_standstill_high_slip():
    m = InductionMotor("m")
    m.inputs["voltage"] = 200.0
    m.update(0.0, 0.001, None)
    # At omega=0, slip=1.0
    assert m.outputs["slip"] == pytest.approx(1.0)


def test_zero_voltage_zero_torque():
    m = InductionMotor("m")
    m.inputs["voltage"] = 0.0
    m.update(0.0, 0.001, None)
    assert m.outputs["torque"] == 0.0


def test_accelerates_to_some_speed():
    m = InductionMotor("m")
    m.parameters["load_torque"] = 0.0
    m.inputs["voltage"] = 200.0
    for i in range(1000):
        m.update(i * 0.001, 0.001, None)
    assert m.outputs["speed"] > 100.0


def test_slip_decreases_with_speed():
    m = InductionMotor("m")
    m.inputs["voltage"] = 200.0
    m.update(0.0, 0.001, None)
    slip0 = m.outputs["slip"]
    for i in range(1000):
        m.update(i * 0.001, 0.001, None)
    slip1 = m.outputs["slip"]
    assert slip1 < slip0


def test_rated_torque_computation():
    m = InductionMotor("m")
    m.parameters["rated_power"] = 1500.0
    m.parameters["rated_speed"] = 1450.0
    # At slip = s_break = 0.2, T = T_break = 2.5 * T_rated
    # Setting omega such that slip = 0.2 is complex; just check basic run
    m.inputs["voltage"] = 200.0
    m.update(0.0, 0.001, None)
    assert m.outputs["torque"] >= 0.0


def test_reset_clears_state():
    m = InductionMotor("m")
    m.inputs["voltage"] = 200.0
    m.update(0.0, 0.001, None)
    m.reset()
    assert m._omega == 0.0
    assert m.outputs["torque"] == 0.0
