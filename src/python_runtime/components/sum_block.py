"""Sum: y = sum(gain_i * input_i)."""

from __future__ import annotations

from typing import Any

from ..component_base import Component


class Sum(Component):
    """Sum of n inputs, each with an optional per-channel gain.

    Parameters:
        input_count: number of active inputs.
        gains: list of per-channel gains (default all 1.0).

    Inputs:
        input_1 .. input_N: signals to sum.

    Outputs:
        output: weighted sum.
    """

    def __init__(self, comp_id: str, comp_type: str = "Sum") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {"input_count": 3, "gains": [1.0, 1.0, 1.0]}
        self.inputs = {}
        self.outputs = {"output": 0.0}
        self._sync_ports()

    def _sync_ports(self) -> None:
        n = int(self.parameters.get("input_count", 3))
        gains = list(self.parameters.get("gains", [])) or [1.0] * n
        for i in range(1, n + 1):
            self.inputs.setdefault(f"input_{i}", 0.0)
        self.parameters["gains"] = gains[:n] + [1.0] * max(0, n - len(gains))

    def reset(self) -> None:
        for k in list(self.inputs.keys()):
            self.inputs[k] = 0.0
        self.outputs = {"output": 0.0}

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        n = int(self.parameters.get("input_count", 3))
        gains = list(self.parameters.get("gains", [1.0] * n))
        total = 0.0
        for i in range(1, n + 1):
            g = float(gains[i - 1]) if i - 1 < len(gains) else 1.0
            total += g * float(self.inputs.get(f"input_{i}", 0.0))
        self.outputs["output"] = total
