"""Tests for Engine."""

from __future__ import annotations

import pytest

from python_runtime.components import Engine


def test_idle_torque_at_t_zero():
    e = Engine("e")
    e.parameters["torque_max"] = 100.0
    e.parameters["idle_ratio"] = 0.1
    e.update(0.0, 0.001, None)
    assert e.outputs["torque"] == pytest.approx(10.0)


def test_torque_reaches_max_after_ramp():
    e = Engine("e")
    e.parameters["torque_max"] = 100.0
    e.parameters["ramp_time"] = 0.1
    e.parameters["idle_ratio"] = 0.0
    e.update(0.1, 0.001, None)
    assert e.outputs["torque"] == pytest.approx(100.0)


def test_torque_plateaus_at_max():
    e = Engine("e")
    e.parameters["torque_max"] = 100.0
    e.parameters["ramp_time"] = 0.1
    e.update(10.0, 0.001, None)
    assert e.outputs["torque"] == pytest.approx(100.0)


def test_linear_ramp_midpoint():
    e = Engine("e")
    e.parameters["torque_max"] = 100.0
    e.parameters["ramp_time"] = 0.2
    e.parameters["idle_ratio"] = 0.0
    e.update(0.1, 0.001, None)
    assert e.outputs["torque"] == pytest.approx(50.0)


def test_zero_ramp_time_immediate_max():
    e = Engine("e")
    e.parameters["torque_max"] = 100.0
    e.parameters["ramp_time"] = 0.0
    e.parameters["idle_ratio"] = 0.0
    e.update(0.0, 0.001, None)
    assert e.outputs["torque"] == pytest.approx(100.0)


def test_reset_clears_outputs():
    e = Engine("e")
    e.update(0.5, 0.001, None)
    assert e.outputs["torque"] != 0.0
    e.reset()
    assert e.outputs["torque"] == 0.0
