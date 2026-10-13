"""LowPassFilter: first-order discrete low-pass filter."""

from __future__ import annotations

import math
from typing import Any

from ..component_base import Component


class LowPassFilter(Component):
    """First-order low-pass filter.

    Two modes:
        - cutoff_frequency > 0: alpha computed from fc and dt
        - cutoff_frequency <= 0: alpha taken from alpha parameter directly

    Parameters:
        cutoff_frequency: fc [Hz]; <=0 to disable and use alpha.
        alpha: direct filter coefficient (0..1), used when fc<=0.
        initial_output: y(0).

    Inputs:
        input: signal to filter.

    Outputs:
        output: filtered signal.
    """

    def __init__(self, comp_id: str, comp_type: str = "LowPassFilter") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "cutoff_frequency": 10.0,
            "alpha": 0.1,
            "initial_output": 0.0,
        }
        self.inputs = {"input": 0.0}
        self.outputs = {"output": 0.0}

    def reset(self) -> None:
        self.inputs = {"input": 0.0}
        self.outputs = {"output": float(self.parameters.get("initial_output", 0.0))}

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        fc = float(self.parameters.get("cutoff_frequency", 0.0))
        alpha_param = float(self.parameters.get("alpha", 0.1))

        if fc > 0.0 and dt > 0.0:
            rc = 1.0 / (2.0 * math.pi * fc)
            alpha = dt / (rc + dt)
        else:
            alpha = alpha_param

        alpha = max(0.0, min(1.0, alpha))

        u = float(self.inputs.get("input", 0.0))
        y = float(self.outputs.get("output", 0.0))
        self.outputs["output"] = alpha * u + (1.0 - alpha) * y
