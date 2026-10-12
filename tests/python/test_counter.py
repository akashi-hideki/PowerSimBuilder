"""Unit tests for Counter."""

from python_runtime.components import Counter


def _new(**params):
    c = Counter("cnt1")
    c.parameters.update(params)
    return c


def _tick(c, t):
    c.inputs["clock"] = True
    c.update(t, 0.1, None)
    c.inputs["clock"] = False
    c.update(t + 0.01, 0.1, None)


def test_count_up():
    c = _new(max_count=5)
    c.reset()
    _tick(c, 0.0)
    assert c.outputs["count"] == 1
    _tick(c, 0.1)
    assert c.outputs["count"] == 2


def test_overflow_at_max():
    c = _new(max_count=2)
    c.reset()
    _tick(c, 0.0)
    _tick(c, 0.1)
    _tick(c, 0.2)
    assert c.outputs["count"] == 2
    assert c.outputs["overflow"] is True


def test_overflow_with_wrap():
    c = _new(max_count=2, min_count=0, wrap=True)
    c.reset()
    _tick(c, 0.0)
    _tick(c, 0.1)
    _tick(c, 0.2)
    assert c.outputs["count"] == 0
    assert c.outputs["overflow"] is False


def test_count_down():
    c = _new(direction="down", min_count=0, initial=2)
    c.reset()
    _tick(c, 0.0)
    assert c.outputs["count"] == 1
    _tick(c, 0.1)
    assert c.outputs["count"] == 0


def test_underflow():
    c = _new(direction="down", min_count=0, initial=0)
    c.reset()
    _tick(c, 0.0)
    assert c.outputs["count"] == 0
    assert c.outputs["underflow"] is True


def test_reset_input():
    c = _new(max_count=5, initial=0)
    c.reset()
    _tick(c, 0.0)
    _tick(c, 0.1)
    assert c.outputs["count"] == 2
    c.inputs["reset"] = True
    c.update(0.2, 0.1, None)
    c.inputs["reset"] = False
    assert c.outputs["count"] == 0


def test_no_count_without_rising_edge():
    c = _new()
    c.reset()
    c.inputs["clock"] = True
    c.update(0.0, 0.1, None)
    c.update(0.1, 0.1, None)  # still True, no rising edge
    assert c.outputs["count"] == 1
