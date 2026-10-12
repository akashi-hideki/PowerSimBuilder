"""Unit tests for DigitalInput."""

from python_runtime.components import DigitalInput


def _new(**params):
    c = DigitalInput("din1")
    c.parameters.update(params)
    return c


def test_initial_state_zero():
    c = _new()
    c.update(0.0, 0.001, None)
    assert c.outputs["state"] == 0
    assert c.outputs["edge_rising"] is False
    assert c.outputs["edge_falling"] is False


def test_rising_edge_no_debounce():
    c = _new()
    c.inputs["state"] = 0
    c.update(0.0, 0.001, None)
    assert c.outputs["state"] == 0

    c.inputs["state"] = 1
    c.update(0.1, 0.001, None)
    assert c.outputs["state"] == 1
    assert c.outputs["edge_rising"] is True
    assert c.outputs["edge_falling"] is False


def test_rising_edge_is_one_shot():
    c = _new()
    c.inputs["state"] = 1
    c.update(0.1, 0.001, None)
    assert c.outputs["edge_rising"] is True

    c.update(0.2, 0.001, None)
    assert c.outputs["edge_rising"] is False
    assert c.outputs["state"] == 1


def test_falling_edge_no_debounce():
    c = _new(initial=1)
    c.reset()
    c.inputs["state"] = 1
    c.update(0.0, 0.001, None)

    c.inputs["state"] = 0
    c.update(0.1, 0.001, None)
    assert c.outputs["state"] == 0
    assert c.outputs["edge_falling"] is True


def test_debounce_filters_short_pulse():
    c = _new(debounce_ms=10.0)
    c.inputs["state"] = 0
    c.update(0.0, 0.001, None)

    c.inputs["state"] = 1
    c.update(0.001, 0.001, None)
    c.inputs["state"] = 0
    c.update(0.002, 0.001, None)

    assert c.outputs["state"] == 0


def test_debounce_accepts_long_pulse():
    c = _new(debounce_ms=10.0)
    c.inputs["state"] = 0
    c.update(0.0, 0.001, None)

    c.inputs["state"] = 1
    c.update(0.001, 0.001, None)
    c.update(0.006, 0.001, None)
    assert c.outputs["state"] == 0

    c.update(0.012, 0.001, None)
    assert c.outputs["state"] == 1
    assert c.outputs["edge_rising"] is True
