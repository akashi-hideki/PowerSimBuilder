"""Unit tests for AnalogInput."""

import pytest

from python_runtime.components import AnalogInput


def _new(**params):
    c = AnalogInput("ain1")
    c.parameters.update(params)
    return c


def test_scaling_midpoint():
    c = _new(v_min=0.0, v_max=5.0)
    c.inputs["raw"] = 2.5
    c.update(0.0, 0.001, None)
    assert c.outputs["scaled"] == pytest.approx(0.5)


def test_scaling_vmin_vmax():
    c = _new(v_min=0.0, v_max=5.0)
    c.inputs["raw"] = 0.0
    c.update(0.0, 0.001, None)
    assert c.outputs["scaled"] == pytest.approx(0.0)

    c.inputs["raw"] = 5.0
    c.update(0.001, 0.001, None)
    assert c.outputs["scaled"] == pytest.approx(1.0)


def test_scaling_clamps_out_of_range():
    c = _new(v_min=0.0, v_max=5.0)
    c.inputs["raw"] = -1.0
    c.update(0.0, 0.001, None)
    assert c.outputs["scaled"] == 0.0

    c.inputs["raw"] = 10.0
    c.update(0.001, 0.001, None)
    assert c.outputs["scaled"] == 1.0


def test_custom_range():
    c = _new(v_min=1.0, v_max=3.0)
    c.inputs["raw"] = 2.0
    c.update(0.0, 0.001, None)
    assert c.outputs["scaled"] == pytest.approx(0.5)


def test_filter_passthrough_alpha_1():
    c = _new(v_min=0.0, v_max=5.0, filter_alpha=1.0)
    c.inputs["raw"] = 0.0
    c.update(0.0, 0.001, None)
    c.inputs["raw"] = 5.0
    c.update(0.001, 0.001, None)
    assert c.outputs["filtered"] == pytest.approx(1.0)


def test_filter_first_order_step_response():
    c = _new(v_min=0.0, v_max=5.0, filter_alpha=0.5)
    c.inputs["raw"] = 0.0
    c.update(0.0, 0.001, None)
    assert c.outputs["filtered"] == pytest.approx(0.0)

    c.inputs["raw"] = 5.0
    c.update(0.001, 0.001, None)
    assert c.outputs["filtered"] == pytest.approx(0.5)

    c.update(0.002, 0.001, None)
    assert c.outputs["filtered"] == pytest.approx(0.75)

    c.update(0.003, 0.001, None)
    assert c.outputs["filtered"] == pytest.approx(0.875)
