"""Tests for LowPassFilter."""

from __future__ import annotations

import pytest

from python_runtime.components import LowPassFilter


def test_initial_output():
    lpf = LowPassFilter("lpf")
    lpf.parameters["initial_output"] = 2.0
    lpf.reset()
    assert lpf.outputs["output"] == 2.0


def test_filter_smooths_step():
    lpf = LowPassFilter("lpf")
    lpf.parameters["cutoff_frequency"] = 10.0
    lpf.inputs["input"] = 10.0
    lpf.update(0.0, 0.001, None)
    # First step: alpha < 1, output < input
    assert 0.0 < lpf.outputs["output"] < 10.0


def test_alpha_passthrough():
    lpf = LowPassFilter("lpf")
    lpf.parameters["cutoff_frequency"] = 0.0
    lpf.parameters["alpha"] = 1.0
    lpf.inputs["input"] = 5.0
    lpf.update(0.0, 0.001, None)
    assert lpf.outputs["output"] == 5.0


def test_converges_to_constant():
    lpf = LowPassFilter("lpf")
    lpf.parameters["cutoff_frequency"] = 10.0
    lpf.inputs["input"] = 1.0
    for k in range(5000):
        lpf.update(k * 0.001, 0.001, None)
    assert abs(lpf.outputs["output"] - 1.0) < 1e-3
