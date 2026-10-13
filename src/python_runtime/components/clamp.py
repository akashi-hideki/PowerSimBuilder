"""Clamp: limit value to [lower, upper]."""

from __future__ import annotations

from typing import Any

from ..component_base import Component


class Clamp(Component):
    """Clamp (saturation).

    Parameters:
        lower: lower bound.
        upper: upper bound.

    Inputs:
        input: signal.

    Outputs:
        output: clamped signal.
        active: 1.0 if clamped, else 0.0.
    """

    def __init__(self, comp_id: str, comp_type: str = "Clamp") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {"lower": -1.0, "upper": 1.0}
        self.inputs = {"input": 0.0}
        self.outputs = {"output": 0.0, "active": 0.0}

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        lo = float(self.parameters.get("lower", -1.0))
        hi = float(self.parameters.get("upper", 1.0))
        u = float(self.inputs.get("input", 0.0))

        if u < lo:
            self.outputs["output"] = lo
            self.outputs["active"] = 1.0
        elif u > hi:
            self.outputs["output"] = hi
            self.outputs["active"] = 1.0
        else:
            self.outputs["output"] = u
            self.outputs["active"] = 0.0
