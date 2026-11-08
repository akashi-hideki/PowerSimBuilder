"""Helper library for ControllerBox F_xxx functions.

Usage inside a STABLE_USER_CODE block (via Role Snippets):

    from python_runtime.lib.filters import MovingAverage, ExponentialMovingAverage
    from python_runtime.lib.math_utils import clamp

    if not hasattr(self, "_ma"):
        self._ma = MovingAverage(window=10)
    self.globals["smoothed"] = self._ma.update(self.globals["raw"])
"""

from .filters import (
    ExponentialMovingAverage,
    MedianFilter,
    MovingAverage,
    RateLimiter,
)
from .math_utils import clamp, deadband, ramp, saturate

__all__ = [
    "MovingAverage",
    "ExponentialMovingAverage",
    "MedianFilter",
    "RateLimiter",
    "clamp",
    "deadband",
    "ramp",
    "saturate",
]
