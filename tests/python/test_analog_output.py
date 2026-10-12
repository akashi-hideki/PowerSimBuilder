"""Unit tests for AnalogOutput."""

import pytest

from python_runtime.components import AnalogOutput


def _new(**params):
    c = AnalogOutput("aout1")
    c.parameters.update(params)
    return c


def test_zero_value():
    c = _new()
    c.inputs["value"] = 0.0
    c.update(0.0, 0.001, None)
    assert c.outputs["duty"] == 0.0
    assert c.outputs["voltage"] == 0.0
    assert c.outputs["active"] is False


def test_half_value():
    c = _new(v_ref=5.0)
    c.inputs["value"] = 0.5
    c.update(0.0, 0.001, None)
    assert c.outputs["duty"] == pytest.approx(0.5)
    assert c.outputs["voltage"] == pytest.approx(2.5)
    assert c.outputs["active"] is True


def test_full_value():
    c = _new(v_ref=3.3)
    c.inputs["value"] = 1.0
    c.update(0.0, 0.001, None)
    assert c.outputs["duty"] == pytest.approx(1.0)
    assert c.outputs["voltage"] == pytest.approx(3.3)


def test_clamps_negative():
    c = _new(v_ref=5.0)
    c.inputs["value"] = -0.5
    c.update(0.0, 0.001, None)
    assert c.outputs["duty"] == 0.0
    assert c.outputs["active"] is False


def test_clamps_above_one():
    c = _new(v_ref=5.0)
    c.inputs["value"] = 2.0
    c.update(0.0, 0.001, None)
    assert c.outputs["duty"] == 1.0
    assert c.outputs["voltage"] == pytest.approx(5.0)
