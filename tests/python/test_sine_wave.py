"""Tests for SineWave."""

from __future__ import annotations

import math

from python_runtime.components import SineWave


def test_initial_outputs_zero():
    sw = SineWave("sw")
    assert sw.outputs["value"] == 0.0


def test_sine_at_quarter_period():
    sw = SineWave("sw")
    sw.parameters["amplitude"] = 10.0
    sw.parameters["frequency"] = 1.0  # 1 Hz => period 1 s
    sw.update(0.25, 0.001, None)
    assert abs(sw.outputs["value"] - 10.0) < 1e-9


def test_offset_applied():
    sw = SineWave("sw")
    sw.parameters["amplitude"] = 5.0
    sw.parameters["offset"] = 100.0
    sw.parameters["frequency"] = 1.0
    sw.update(0.25, 0.001, None)
    assert abs(sw.outputs["value"] - 105.0) < 1e-9


def test_phase_shifts():
    sw = SineWave("sw")
    sw.parameters["amplitude"] = 1.0
    sw.parameters["frequency"] = 1.0
    sw.parameters["phase"] = math.pi / 2.0
    sw.update(0.0, 0.001, None)
    assert abs(sw.outputs["value"] - 1.0) < 1e-9


def test_start_stop_window():
    sw = SineWave("sw")
    sw.parameters["amplitude"] = 1.0
    sw.parameters["frequency"] = 1.0
    sw.parameters["offset"] = -1.0
    sw.parameters["start_time"] = 1.0
    sw.parameters["stop_time"] = 2.0
    # before start
    sw.update(0.5, 0.001, None)
    assert sw.outputs["value"] == -1.0
    # after stop
    sw.update(2.5, 0.001, None)
    assert sw.outputs["value"] == -1.0
    # inside
    sw.update(1.25, 0.001, None)
    assert sw.outputs["value"] > -1.0


def test_reset_clears_outputs():
    sw = SineWave("sw")
    sw.parameters["amplitude"] = 1.0
    sw.update(0.25, 0.001, None)
    sw.reset()
    assert sw.outputs["value"] == 0.0
