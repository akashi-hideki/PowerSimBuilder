"""Tests for DiodeBridge."""

from __future__ import annotations

import math

import pytest

from python_runtime.components import DiodeBridge


def test_full_wave_positive_input():
    d = DiodeBridge("d")
    d.parameters["diode_drop"] = 0.7
    d.inputs["ac_voltage"] = 10.0
    d.update(0.0, 0.001, None)
    # |10| - 2*0.7 = 8.6
    assert d.outputs["dc_voltage"] == pytest.approx(8.6)


def test_full_wave_negative_input():
    d = DiodeBridge("d")
    d.inputs["ac_voltage"] = -10.0
    d.update(0.0, 0.001, None)
    # abs(-10) - 2*0.7 = 8.6
    assert d.outputs["dc_voltage"] == pytest.approx(8.6)


def test_half_wave_negative_blocked():
    d = DiodeBridge("d")
    d.parameters["type"] = "half"
    d.inputs["ac_voltage"] = -10.0
    d.update(0.0, 0.001, None)
    assert d.outputs["dc_voltage"] == 0.0


def test_half_wave_positive_pass():
    d = DiodeBridge("d")
    d.parameters["type"] = "half"
    d.parameters["diode_drop"] = 0.7
    d.inputs["ac_voltage"] = 10.0
    d.update(0.0, 0.001, None)
    assert d.outputs["dc_voltage"] == pytest.approx(9.3)


def test_below_threshold_zero():
    d = DiodeBridge("d")
    d.inputs["ac_voltage"] = 0.5  # < 2*0.7
    d.update(0.0, 0.001, None)
    assert d.outputs["dc_voltage"] == 0.0


def test_ripple_without_capacitor():
    d = DiodeBridge("d")
    d.inputs["ac_voltage"] = 10.0
    d.update(0.0, 0.001, None)
    # peak = 8.6, dc = 8.6 -> ripple = 0
    assert d.outputs["ripple"] == pytest.approx(0.0, abs=1e-9)


def test_ripple_with_capacitor():
    d = DiodeBridge("d")
    d.parameters["smoothing_capacitance"] = 0.001  # 1 mF
    d.parameters["load_resistance"] = 10.0
    d.inputs["ac_voltage"] = 10.0
    d.update(0.0, 0.001, None)
    # ripple = I_load / (100 * C) = (8.6/10) / (100*0.001) = 0.86
    expected = (8.6 / 10.0) / (100.0 * 0.001)
    assert d.outputs["ripple"] == pytest.approx(expected)


def test_three_phase_factor():
    d = DiodeBridge("d")
    d.parameters["type"] = "three_phase"
    d.parameters["diode_drop"] = 0.7
    d.inputs["ac_voltage"] = 10.0
    d.update(0.0, 0.001, None)
    expected = 10.0 * math.sqrt(3.0) - 2.0 * 0.7
    assert d.outputs["dc_voltage"] == pytest.approx(expected)
