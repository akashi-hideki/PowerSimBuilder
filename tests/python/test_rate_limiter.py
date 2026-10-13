"""Tests for RateLimiter."""

from __future__ import annotations

import pytest

from python_runtime.components import RateLimiter


def test_rising_rate_limit():
    rl = RateLimiter("rl")
    rl.parameters["rising_rate"] = 10.0
    rl.parameters["falling_rate"] = 10.0
    rl.inputs["input"] = 100.0
    rl.update(0.0, 0.1, None)
    # max change = 10 * 0.1 = 1.0
    assert abs(rl.outputs["output"] - 1.0) < 1e-6
    assert rl.outputs["saturated"] == 1.0


def test_falling_rate_limit():
    rl = RateLimiter("rl")
    rl.parameters["initial_output"] = 100.0
    rl.reset()
    rl.parameters["rising_rate"] = 10.0
    rl.parameters["falling_rate"] = 10.0
    rl.inputs["input"] = 0.0
    rl.update(0.0, 0.1, None)
    assert abs(rl.outputs["output"] - 99.0) < 1e-6


def test_no_saturation():
    rl = RateLimiter("rl")
    rl.parameters["rising_rate"] = 1000.0
    rl.inputs["input"] = 1.0
    rl.update(0.0, 0.1, None)
    assert rl.outputs["output"] == 1.0
    assert rl.outputs["saturated"] == 0.0
