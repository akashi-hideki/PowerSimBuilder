"""Gain: y = k * u + b."""

from __future__ import annotations

from typing import Any

from ..component_base import Component


class Gain(Component):
    """Multiplicative gain with optional bias.

    Parameters:
        gain: k.
        bias: b, added after multiplication (y = k*u + b).

    Inputs:
        input: signal.

    Outputs:
        output: k*u + b.
    """

    def __init__(self, comp_id: str, comp_type: str = "Gain") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {"gain": 1.0, "bias": 0.0}
        self.inputs = {"input": 0.0}
        self.outputs = {"output": 0.0}

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        k = float(self.parameters.get("gain", 1.0))
        b = float(self.parameters.get("bias", 0.0))
        u = float(self.inputs.get("input", 0.0))
        self.outputs["output"] = k * u + b
