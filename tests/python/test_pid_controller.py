"""Unit tests for PIDController."""

import pytest

from python_runtime.components import PIDController


def _new(**params):
    c = PIDController("pid1")
    c.parameters.update(params)
    return c


def test_proportional_only():
    c = _new(kp=2.0, ki=0.0, kd=0.0, setpoint=1.0)
    c.inputs["measured"] = 0.0
    c.update(0.0, 0.1, None)
    assert c.outputs["error"] == pytest.approx(1.0)
    assert c.outputs["control"] == pytest.approx(2.0)


def test_integral_accumulates():
    c = _new(kp=0.0, ki=1.0, kd=0.0, setpoint=1.0)
    c.inputs["measured"] = 0.0

    c.update(0.0, 0.1, None)
    assert c.outputs["i_term"] == pytest.approx(0.0)

    c.update(0.1, 0.1, None)
    assert c.outputs["i_term"] == pytest.approx(0.1)

    c.update(0.2, 0.1, None)
    assert c.outputs["i_term"] == pytest.approx(0.2)


def test_derivative_step_response():
    c = _new(kp=0.0, ki=0.0, kd=1.0, setpoint=1.0)
    c.inputs["measured"] = 0.0

    c.update(0.0, 0.1, None)
    assert c.outputs["d_term"] == pytest.approx(0.0)

    c.inputs["measured"] = 1.0
    c.update(0.1, 0.1, None)
    # error goes 1 -> 0, derivative = (0-1)/0.1 = -10
    assert c.outputs["d_term"] == pytest.approx(-10.0)


def test_output_clamping():
    c = _new(kp=10.0, out_min=-1.0, out_max=1.0, setpoint=1.0)
    c.inputs["measured"] = 0.0
    c.update(0.0, 0.1, None)
    assert c.outputs["control"] == 1.0
    assert c.outputs["saturated"] is True
    assert c.outputs["p_term"] == pytest.approx(10.0)


def test_no_saturation():
    c = _new(kp=0.5, out_min=-1.0, out_max=1.0, setpoint=1.0)
    c.inputs["measured"] = 0.0
    c.update(0.0, 0.1, None)
    assert c.outputs["control"] == pytest.approx(0.5)
    assert c.outputs["saturated"] is False


def test_anti_windup_blocks_integral():
    c = _new(kp=10.0, ki=1.0, out_min=-1.0, out_max=1.0, anti_windup=True, setpoint=1.0)
    c.inputs["measured"] = 0.0
    c.update(0.0, 0.1, None)
    # saturated, so integral not accumulated
    assert c.outputs["saturated"] is True
    c.update(0.1, 0.1, None)
    assert c.outputs["i_term"] == pytest.approx(0.0)


def test_setpoint_input_override():
    c = _new(kp=1.0, setpoint=0.0)
    c.inputs["setpoint"] = 2.0
    c.inputs["measured"] = 0.0
    c.update(0.0, 0.1, None)
    assert c.outputs["error"] == pytest.approx(2.0)
