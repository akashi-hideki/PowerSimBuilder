"""FirstOrderLag: dy/dt = (u - y) / tau."""

from __future__ import annotations

from typing import Any

from ..component_base import Component


class FirstOrderLag(Component):
    """First-order lag element.

    Parameters:
        time_constant: tau [s]. tau <= 0 disables lag (y = u).
        initial_output: y(0).

    Inputs:
        input: driving signal.

    Outputs:
        output: y(t).
    """

    def __init__(self, comp_id: str, comp_type: str = "FirstOrderLag") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {"time_constant": 1.0, "initial_output": 0.0}
        self.inputs = {"input": 0.0}
        self.outputs = {"output": 0.0}

    def reset(self) -> None:
        self.inputs = {"input": 0.0}
        self.outputs = {"output": float(self.parameters.get("initial_output", 0.0))}

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        tau = float(self.parameters.get("time_constant", 1.0))
        u = float(self.inputs.get("input", 0.0))
        y = float(self.outputs.get("output", 0.0))

        if tau <= 0.0:
            y = u
        else:
            y += ((u - y) / tau) * dt

        self.outputs["output"] = y
