"""Tests for FirstOrderLag."""

from __future__ import annotations

import pytest

from python_runtime.components import FirstOrderLag


def test_initial_output():
    fol = FirstOrderLag("fol")
    fol.parameters["initial_output"] = 5.0
    fol.reset()
    assert fol.outputs["output"] == 5.0


def test_converges_to_input():
    fol = FirstOrderLag("fol")
    fol.parameters["time_constant"] = 0.1
    fol.inputs["input"] = 10.0
    for k in range(1000):
        fol.update(k * 0.001, 0.001, None)
    assert abs(fol.outputs["output"] - 10.0) < 0.01


def test_zero_tau_passthrough():
    fol = FirstOrderLag("fol")
    fol.parameters["time_constant"] = 0.0
    fol.inputs["input"] = 42.0
    fol.update(0.0, 0.001, None)
    assert fol.outputs["output"] == 42.0


def test_exponential_response():
    fol = FirstOrderLag("fol")
    fol.parameters["time_constant"] = 1.0
    fol.inputs["input"] = 1.0
    # After 1 tau, should reach ~63.2%
    for k in range(1000):
        fol.update(k * 0.001, 0.001, None)
    assert abs(fol.outputs["output"] - 0.632) < 0.01
