"""Tests for Clamp."""

from __future__ import annotations

from python_runtime.components import Clamp


def test_within_range():
    c = Clamp("c")
    c.parameters["lower"] = 0.0
    c.parameters["upper"] = 10.0
    c.inputs["input"] = 5.0
    c.update(0.0, 0.001, None)
    assert c.outputs["output"] == 5.0
    assert c.outputs["active"] == 0.0


def test_upper_clamp():
    c = Clamp("c")
    c.parameters["lower"] = 0.0
    c.parameters["upper"] = 10.0
    c.inputs["input"] = 15.0
    c.update(0.0, 0.001, None)
    assert c.outputs["output"] == 10.0
    assert c.outputs["active"] == 1.0


def test_lower_clamp():
    c = Clamp("c")
    c.parameters["lower"] = 0.0
    c.parameters["upper"] = 10.0
    c.inputs["input"] = -5.0
    c.update(0.0, 0.001, None)
    assert c.outputs["output"] == 0.0
    assert c.outputs["active"] == 1.0
