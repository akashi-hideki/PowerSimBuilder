"""Integrator: y = y0 + integral(u dt)."""

from __future__ import annotations

from typing import Any

from ..component_base import Component


class Integrator(Component):
    """Pure integrator with optional reset input.

    Parameters:
        initial_output: y(0).
        gain: multiplier applied to input before integration.
        lower_limit: saturation lower bound (None to disable).
        upper_limit: saturation upper bound (None to disable).

    Inputs:
        input: signal to integrate.
        reset: truthy value resets output to initial_output.

    Outputs:
        output: integrated value.
    """

    def __init__(self, comp_id: str, comp_type: str = "Integrator") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "initial_output": 0.0,
            "gain": 1.0,
            "lower_limit": None,
            "upper_limit": None,
        }
        self.inputs = {"input": 0.0, "reset": 0.0}
        self.outputs = {"output": float(self.parameters["initial_output"])}

    def reset(self) -> None:
        self.inputs = {"input": 0.0, "reset": 0.0}
        self.outputs = {"output": float(self.parameters.get("initial_output", 0.0))}

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        if float(self.inputs.get("reset", 0.0)) != 0.0:
            self.outputs["output"] = float(self.parameters.get("initial_output", 0.0))
            return

        gain = float(self.parameters.get("gain", 1.0))
        u = float(self.inputs.get("input", 0.0))
        y = float(self.outputs.get("output", 0.0))

        y += gain * u * dt

        lo = self.parameters.get("lower_limit")
        hi = self.parameters.get("upper_limit")
        if lo is not None:
            y = max(float(lo), y)
        if hi is not None:
            y = min(float(hi), y)

        self.outputs["output"] = y
