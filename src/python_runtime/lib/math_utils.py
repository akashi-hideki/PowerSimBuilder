"""Small math helpers for ControllerBox F_xxx functions."""

from __future__ import annotations


def clamp(x: float, lo: float, hi: float) -> float:
    """Limit x to [lo, hi]."""
    if lo > hi:
        lo, hi = hi, lo
    return max(lo, min(hi, x))


def saturate(x: float, lo: float = 0.0, hi: float = 1.0) -> float:
    """Alias of clamp for the common 0..1 saturation case."""
    return clamp(x, lo, hi)


def deadband(x: float, width: float) -> float:
    """Return 0 when |x| <= width, else x (preserves sign)."""
    width = abs(float(width))
    return 0.0 if abs(x) <= width else float(x)


def ramp(current: float, target: float, step: float) -> float:
    """Move current toward target by at most `step`."""
    step = abs(float(step))
    diff = target - current
    if diff > step:
        return current + step
    if diff < -step:
        return current - step
    return target
