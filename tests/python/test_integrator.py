"""Tests for Integrator."""

from __future__ import annotations

from python_runtime.components import Integrator


def test_integrates_constant():
    integ = Integrator("i")
    integ.inputs["input"] = 1.0
    for k in range(100):
        integ.update(k * 0.01, 0.01, None)
    assert abs(integ.outputs["output"] - 1.0) < 1e-6


def test_reset_zeroes_output():
    integ = Integrator("i")
    integ.inputs["input"] = 1.0
    integ.update(0.0, 0.1, None)
    integ.inputs["reset"] = 1.0
    integ.update(0.1, 0.1, None)
    assert integ.outputs["output"] == 0.0


def test_saturation_limits():
    integ = Integrator("i")
    integ.parameters["upper_limit"] = 5.0
    integ.parameters["lower_limit"] = -5.0
    integ.inputs["input"] = 100.0
    for k in range(100):
        integ.update(k * 0.01, 0.01, None)
    assert integ.outputs["output"] == 5.0
