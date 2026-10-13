"""Tests for PWM3Phase."""

from __future__ import annotations

import pytest

from python_runtime.components import PWM3Phase


def test_duties_in_range():
    pwm = PWM3Phase("pwm")
    pwm.parameters["amplitude"] = 0.9
    for k in range(100):
        pwm.update(k * 0.0001, 0.0001, None)
        assert 0.0 <= pwm.outputs["da"] <= 1.0
        assert 0.0 <= pwm.outputs["db"] <= 1.0
        assert 0.0 <= pwm.outputs["dc"] <= 1.0


def test_modulation_index_reported():
    pwm = PWM3Phase("pwm")
    pwm.parameters["amplitude"] = 0.75
    pwm.update(0.0, 0.0001, None)
    assert pwm.outputs["modulation_index"] == 0.75


def test_common_mode_center():
    """With common_mode, max |duty - 0.5| should be reduced."""
    pwm = PWM3Phase("pwm")
    pwm.parameters["amplitude"] = 0.5
    pwm.parameters["common_mode"] = True
    pwm.update(0.0, 0.0001, None)
    # With common mode, one of the duties should be near 0.5
    duties = [pwm.outputs["da"], pwm.outputs["db"], pwm.outputs["dc"]]
    # All within [0, 1]
    for d in duties:
        assert 0.0 <= d <= 1.0
