"""Tests for PowerSource."""

from __future__ import annotations

import math

import pytest

from python_runtime.components import PowerSource


def test_sine_at_zero_phase():
    ps = PowerSource("ps")
    ps.parameters["voltage"] = 100.0
    ps.parameters["frequency"] = 50.0
    ps.parameters["phase"] = 0.0
    ps.update(0.0, 0.001, None)
    assert ps.outputs["voltage"] == pytest.approx(0.0, abs=1e-9)


def test_sine_peak_at_quarter_period():
    ps = PowerSource("ps")
    ps.parameters["voltage"] = 100.0
    ps.parameters["frequency"] = 50.0
    # Quarter period = 1/(4*50) = 0.005 s.
    ps.update(1.0 / 200.0, 0.001, None)
    expected_peak = 100.0 * math.sqrt(2.0)
    assert ps.outputs["voltage"] == pytest.approx(expected_peak, rel=1e-6)


def test_rms_to_peak_conversion():
    ps = PowerSource("ps")
    ps.parameters["voltage"] = 100.0
    ps.parameters["frequency"] = 50.0
    ps.update(1.0 / 200.0, 0.001, None)
    assert ps.outputs["voltage"] == pytest.approx(141.421356, rel=1e-5)


def test_current_from_voltage_and_resistance():
    ps = PowerSource("ps")
    ps.parameters["voltage"] = 100.0
    ps.parameters["resistance"] = 50.0
    ps.update(1.0 / 200.0, 0.001, None)
    v = ps.outputs["voltage"]
    assert ps.outputs["current"] == pytest.approx(v / 50.0)


def test_zero_resistance_current_is_zero():
    ps = PowerSource("ps")
    ps.parameters["resistance"] = 0.0
    ps.update(0.001, 0.001, None)
    assert ps.outputs["current"] == 0.0


def test_square_waveform():
    ps = PowerSource("ps")
    ps.parameters["waveform"] = "square"
    ps.parameters["voltage"] = 100.0
    ps.parameters["frequency"] = 50.0
    ps.update(0.0, 0.001, None)
    v_peak = 100.0 * math.sqrt(2.0)
    assert ps.outputs["voltage"] == pytest.approx(v_peak, rel=1e-6)


def test_triangle_waveform():
    ps = PowerSource("ps")
    ps.parameters["waveform"] = "triangle"
    ps.parameters["voltage"] = 100.0
    ps.parameters["frequency"] = 50.0
    ps.update(0.0, 0.001, None)
    v_peak = 100.0 * math.sqrt(2.0)
    assert ps.outputs["voltage"] == pytest.approx(v_peak, rel=1e-6)


def test_reset_clears_outputs():
    ps = PowerSource("ps")
    ps.update(0.005, 0.001, None)
    assert ps.outputs["voltage"] != 0.0
    ps.reset()
    assert ps.outputs["voltage"] == 0.0
    assert ps.outputs["current"] == 0.0
