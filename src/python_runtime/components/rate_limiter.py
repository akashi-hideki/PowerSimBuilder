"""RateLimiter: limit slew rate of a signal."""

from __future__ import annotations

from typing import Any

from ..component_base import Component


class RateLimiter(Component):
    """Limit the rate of change of an input signal.

    Parameters:
        rising_rate: max positive slope [unit/s].
        falling_rate: max negative slope magnitude [unit/s] (>0).
        initial_output: y(0).

    Inputs:
        input: signal.

    Outputs:
        output: rate-limited signal.
        saturated: 1.0 if limited, else 0.0.
    """

    def __init__(self, comp_id: str, comp_type: str = "RateLimiter") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "rising_rate": 1.0,
            "falling_rate": 1.0,
            "initial_output": 0.0,
        }
        self.inputs = {"input": 0.0}
        self.outputs = {"output": 0.0, "saturated": 0.0}

    def reset(self) -> None:
        self.inputs = {"input": 0.0}
        self.outputs = {
            "output": float(self.parameters.get("initial_output", 0.0)),
            "saturated": 0.0,
        }

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        rr = float(self.parameters.get("rising_rate", 1.0))
        fr = abs(float(self.parameters.get("falling_rate", 1.0)))
        u = float(self.inputs.get("input", 0.0))
        y = float(self.outputs.get("output", 0.0))

        max_up = rr * dt
        max_dn = fr * dt
        diff = u - y

        if diff > max_up:
            y += max_up
            self.outputs["saturated"] = 1.0
        elif diff < -max_dn:
            y -= max_dn
            self.outputs["saturated"] = 1.0
        else:
            y = u
            self.outputs["saturated"] = 0.0

        self.outputs["output"] = y
