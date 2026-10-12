"""Unit tests for Timer."""
from python_runtime.components import Timer


def _new(**params):
    c = Timer("tmr1")
    c.parameters.update(params)
    return c


def test_delay_mode_fires_after_duration():
    c = _new(mode="delay", duration=0.5)
    c.inputs["trigger"] = True
    c.update(0.0, 0.1, None)
    assert c.outputs["done"] is False
    c.update(0.3, 0.1, None)
    assert c.outputs["done"] is False
    c.update(0.5, 0.1, None)
    assert c.outputs["done"] is True


def test_delay_mode_resets_when_trigger_low():
    c = _new(mode="delay", duration=0.5)
    c.inputs["trigger"] = True
    c.update(0.0, 0.1, None)
    c.update(0.4, 0.1, None)
    assert c.outputs["done"] is False

    c.inputs["trigger"] = False
    c.update(0.5, 0.1, None)
    assert c.outputs["done"] is False
    assert c.outputs["elapsed"] == 0.0


def test_oneshot_stays_on_for_duration():
    c = _new(mode="oneshot", duration=0.5)
    # rising edge
    c.inputs["trigger"] = True
    c.update(0.0, 0.1, None)
    assert c.outputs["done"] is True

    c.update(0.4, 0.1, None)
    assert c.outputs["done"] is True

    c.update(0.5, 0.1, None)
    assert c.outputs["done"] is False


def test_oneshot_ignores_long_trigger():
    c = _new(mode="oneshot", duration=0.3)
    c.inputs["trigger"] = True
    c.update(0.0, 0.1, None)
    c.update(0.4, 0.1, None)
    assert c.outputs["done"] is False
    # still True on trigger, but oneshot already finished
    c.update(0.5, 0.1, None)
    assert c.outputs["done"] is False


def test_interval_toggles():
    c = _new(mode="interval", duration=0.2)
    c.inputs["trigger"] = True
    c.update(0.0, 0.1, None)
    assert c.outputs["done"] is True
    c.update(0.1, 0.1, None)
    assert c.outputs["done"] is True
    c.update(0.2, 0.1, None)
    assert c.outputs["done"] is False
    c.update(0.4, 0.1, None)
    assert c.outputs["done"] is True


def test_reset_input_clears_state():
    c = _new(mode="delay", duration=0.5)
    c.inputs["trigger"] = True
    c.update(0.0, 0.1, None)
    c.inputs["reset"] = True
    c.update(0.1, 0.1, None)
    assert c.outputs["elapsed"] == 0.0
    assert c.outputs["done"] is False
