"""Unit tests for DigitalOutput."""

from python_runtime.components import DigitalOutput


def _new(**params):
    c = DigitalOutput("dout1")
    c.parameters.update(params)
    return c


def test_command_zero_gives_state_zero():
    c = _new()
    c.inputs["command"] = 0
    c.update(0.0, 0.001, None)
    assert c.outputs["state"] == 0
    assert c.outputs["current_draw"] == 0.0


def test_command_one_gives_state_one_active_high():
    c = _new(active_low=False)
    c.inputs["command"] = 1
    c.update(0.0, 0.001, None)
    assert c.outputs["state"] == 1
    assert c.outputs["current_draw"] == 20.0


def test_active_low_inverts_physical_state():
    c = _new(active_low=True)
    c.inputs["command"] = 1
    c.update(0.0, 0.001, None)
    assert c.outputs["state"] == 0
    assert c.outputs["current_draw"] == 20.0


def test_active_low_command_zero():
    c = _new(active_low=True)
    c.inputs["command"] = 0
    c.update(0.0, 0.001, None)
    assert c.outputs["state"] == 1
    assert c.outputs["current_draw"] == 0.0


def test_custom_on_current():
    c = _new(on_current_ma=50.0)
    c.inputs["command"] = 1
    c.update(0.0, 0.001, None)
    assert c.outputs["current_draw"] == 50.0
