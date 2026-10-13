"""Tests for Gain."""

from __future__ import annotations

from python_runtime.components import Gain


def test_basic_multiplication():
    g = Gain("g")
    g.parameters["gain"] = 3.0
    g.inputs["input"] = 5.0
    g.update(0.0, 0.001, None)
    assert g.outputs["output"] == 15.0


def test_with_bias():
    g = Gain("g")
    g.parameters["gain"] = 3.0
    g.parameters["bias"] = 1.0
    g.inputs["input"] = 5.0
    g.update(0.0, 0.001, None)
    assert g.outputs["output"] == 16.0


def test_zero_gain():
    g = Gain("g")
    g.parameters["gain"] = 0.0
    g.inputs["input"] = 100.0
    g.update(0.0, 0.001, None)
    assert g.outputs["output"] == 0.0
