"""Tests for python_runtime.lib (filters + math_utils)."""

import pytest

from python_runtime.lib import (
    ExponentialMovingAverage,
    MedianFilter,
    MovingAverage,
    RateLimiter,
    clamp,
    deadband,
    ramp,
    saturate,
)

# --- MovingAverage ---


def test_ma_single_value():
    ma = MovingAverage(window=5)
    assert ma.update(10.0) == 10.0


def test_ma_fills_and_slides():
    ma = MovingAverage(window=3)
    assert [ma.update(v) for v in [1, 2, 3]] == [1.0, 1.5, 2.0]
    assert ma.update(4) == pytest.approx(3.0)


def test_ma_reset():
    ma = MovingAverage(window=3)
    ma.update(100.0)
    ma.reset()
    assert ma.update(5.0) == 5.0


def test_ma_window_minimum():
    ma = MovingAverage(window=0)
    assert ma.window == 1


# --- ExponentialMovingAverage ---


def test_ema_first_call_returns_input():
    ema = ExponentialMovingAverage(alpha=0.5)
    assert ema.update(10.0) == 10.0


def test_ema_alpha_1_is_identity():
    ema = ExponentialMovingAverage(alpha=1.0)
    for v in [1.0, 2.0, 3.0, 4.0]:
        assert ema.update(v) == v


def test_ema_smoothing():
    ema = ExponentialMovingAverage(alpha=0.5, initial=0.0)
    assert ema.update(10.0) == pytest.approx(5.0)
    assert ema.update(10.0) == pytest.approx(7.5)


def test_ema_invalid_alpha():
    with pytest.raises(ValueError):
        ExponentialMovingAverage(alpha=0.0)
    with pytest.raises(ValueError):
        ExponentialMovingAverage(alpha=1.5)


def test_ema_reset():
    ema = ExponentialMovingAverage(alpha=0.5, initial=0.0)
    ema.update(10.0)
    ema.reset()
    assert ema.update(20.0) == 20.0


# --- MedianFilter ---


def test_median_odd_window():
    mf = MedianFilter(window=3)
    assert mf.update(1) == 1
    assert mf.update(3) == 2.0
    assert mf.update(5) == 3.0
    assert mf.update(1) == 3.0
    assert mf.update(9) == 5.0  # buf=[5,1,9] sorted=[1,5,9], median=5.0


def test_median_even_window():
    mf = MedianFilter(window=4)
    results = [mf.update(v) for v in [1, 2, 3, 4]]
    assert results[-1] == pytest.approx(2.5)


# --- RateLimiter ---


def test_rate_first_call_returns_target():
    rl = RateLimiter(max_step=1.0)
    assert rl.update(100.0) == 100.0


def test_rate_limits_upward():
    rl = RateLimiter(max_step=1.0)
    rl.update(0.0)
    assert rl.update(5.0) == 1.0
    assert rl.update(5.0) == 2.0


def test_rate_limits_downward():
    rl = RateLimiter(max_step=1.0)
    rl.update(5.0)
    assert rl.update(0.0) == 4.0


# --- math_utils ---


def test_clamp():
    assert clamp(5, 0, 10) == 5
    assert clamp(-1, 0, 10) == 0
    assert clamp(11, 0, 10) == 10


def test_clamp_reversed_bounds():
    assert clamp(5, 10, 0) == 5
    assert clamp(-1, 10, 0) == 0


def test_saturate():
    assert saturate(0.5) == 0.5
    assert saturate(-0.1) == 0.0
    assert saturate(1.5) == 1.0


def test_deadband():
    assert deadband(0.5, 1.0) == 0.0
    assert deadband(-0.5, 1.0) == 0.0
    assert deadband(2.0, 1.0) == 2.0
    assert deadband(-2.0, 1.0) == -2.0


def test_ramp():
    assert ramp(0, 10, 3) == 3
    assert ramp(10, 0, 3) == 7
    assert ramp(0, 2, 3) == 2
