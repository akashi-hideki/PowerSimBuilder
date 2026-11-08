"""Simple signal filters for ControllerBox F_xxx functions.

All filters are stateful: instantiate once (via hasattr guard on first use)
and call `.update(x)` each cycle.
"""

from __future__ import annotations


class MovingAverage:
    """Simple moving average over the last `window` samples."""

    def __init__(self, window: int = 10) -> None:
        self.window = max(1, int(window))
        self._buf: list[float] = []

    def update(self, x: float) -> float:
        self._buf.append(float(x))
        if len(self._buf) > self.window:
            self._buf.pop(0)
        return sum(self._buf) / len(self._buf)

    def reset(self) -> None:
        self._buf.clear()


class ExponentialMovingAverage:
    """Exponential moving average.

    alpha = smoothing factor (0 < alpha <= 1).
    - alpha=1.0 -> no smoothing (y = x)
    - alpha=0.1 -> heavy smoothing (~10-sample time constant)

    Note: F_xxx functions do not receive `dt` directly, so alpha-based
    formulation is used. To convert from time constant tau with step dt:
        alpha = dt / (tau + dt)
    """

    def __init__(self, alpha: float = 0.1, initial: float | None = None) -> None:
        if not (0 < alpha <= 1):
            raise ValueError(f"alpha must be in (0, 1], got {alpha}")
        self.alpha = float(alpha)
        self._y = float(initial) if initial is not None else None

    def update(self, x: float) -> float:
        x = float(x)
        if self._y is None:
            self._y = x
        else:
            self._y = self.alpha * x + (1 - self.alpha) * self._y
        return self._y

    def reset(self) -> None:
        self._y = None


class MedianFilter:
    """Median over the last `window` samples (odd window recommended)."""

    def __init__(self, window: int = 5) -> None:
        self.window = max(1, int(window))
        self._buf: list[float] = []

    def update(self, x: float) -> float:
        self._buf.append(float(x))
        if len(self._buf) > self.window:
            self._buf.pop(0)
        s = sorted(self._buf)
        n = len(s)
        return s[n // 2] if n % 2 else 0.5 * (s[n // 2 - 1] + s[n // 2])

    def reset(self) -> None:
        self._buf.clear()


class RateLimiter:
    """Limit change per call to `max_step` (units per cycle).

    Set max_step = max_rate * dt manually since F_xxx does not receive dt.
    E.g., max_rate=10.0 [unit/s], dt=0.001 [s] -> max_step=0.01.
    """

    def __init__(self, max_step: float) -> None:
        self.max_step = abs(float(max_step))
        self._y: float | None = None

    def update(self, target: float) -> float:
        target = float(target)
        if self._y is None:
            self._y = target
            return self._y
        diff = target - self._y
        if diff > self.max_step:
            self._y += self.max_step
        elif diff < -self.max_step:
            self._y -= self.max_step
        else:
            self._y = target
        return self._y

    def reset(self) -> None:
        self._y = None
