"""SineWaveSum: sum of n sine components with independent parameters."""

from __future__ import annotations

import math
from typing import Any

from ..component_base import Component


class SineWaveSum(Component):
    """Sum of multiple sine waves.

    Parameters:
        components: list of dicts, each with keys
            {"amplitude": float, "frequency": float,
             "phase": float, "offset": float}
        max_channels: number of exposed per-channel outputs.

    Inputs: (none)

    Outputs:
        value: total sum.
        channel_1 .. channel_N: individual contributions.
    """

    def __init__(self, comp_id: str, comp_type: str = "SineWaveSum") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "components": [
                {"amplitude": 1.0, "frequency": 1.0, "phase": 0.0, "offset": 0.0},
            ],
            "max_channels": 3,
        }
        self.inputs: dict[str, float] = {}
        self.outputs = {"value": 0.0}
        self._sync_outputs()

    def _sync_outputs(self) -> None:
        n = int(self.parameters.get("max_channels", 3))
        for i in range(1, n + 1):
            self.outputs.setdefault(f"channel_{i}", 0.0)

    def reset(self) -> None:
        self.outputs = {"value": 0.0}
        self._sync_outputs()

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        comps = self.parameters.get("components", []) or []
        n = int(self.parameters.get("max_channels", 3))

        total = 0.0
        for i, c in enumerate(comps[:n]):
            A = float(c.get("amplitude", 1.0))
            f = float(c.get("frequency", 1.0))
            ph = float(c.get("phase", 0.0))
            off = float(c.get("offset", 0.0))
            v = off + A * math.sin(2.0 * math.pi * f * t + ph)
            self.outputs[f"channel_{i + 1}"] = v
            total += v

        for i in range(len(comps[:n]), n):
            self.outputs[f"channel_{i + 1}"] = 0.0

        self.outputs["value"] = total
