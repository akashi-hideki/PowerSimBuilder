"""AnalogInput component: scaling + first-order filter."""
from __future__ import annotations

from typing import Any

from ..component_base import Component


class AnalogInput(Component):
    """Analog input with scaling and first-order low-pass filter.

    Parameters:
        v_min: input voltage lower bound [V].
        v_max: input voltage upper bound [V].
        resolution: ADC resolution [bit] (informational).
        filter_alpha: first-order filter coefficient (0..1); 0 disables.

    Inputs:
        raw: raw input voltage [V].

    Outputs:
        raw: input passthrough.
        scaled: normalized to 0..1 (clamped).
        filtered: filtered scaled value.
    """

    def __init__(self, comp_id: str, comp_type: str = "AnalogInput") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "v_min": 0.0,
            "v_max": 5.0,
            "resolution": 12,
            "filter_alpha": 0.0,
        }
        self.inputs = {"raw": 0.0}
        self.outputs = {"raw": 0.0, "scaled": 0.0, "filtered": 0.0}
        self._filtered: float | None = None

    def reset(self) -> None:
        self.inputs = {"raw": 0.0}
        self.outputs = {"raw": 0.0, "scaled": 0.0, "filtered": 0.0}
        self._filtered = None

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        raw = float(self.inputs.get("raw", 0.0))
        v_min = float(self.parameters.get("v_min", 0.0))
        v_max = float(self.parameters.get("v_max", 5.0))
        alpha = float(self.parameters.get("filter_alpha", 0.0))

        if v_max == v_min:
            scaled = 0.0
        else:
            scaled = (raw - v_min) / (v_max - v_min)
        scaled = max(0.0, min(1.0, scaled))

        if self._filtered is None:
            self._filtered = scaled
        elif alpha <= 0.0 or alpha >= 1.0:
            self._filtered = scaled
        else:
            self._filtered = alpha * scaled + (1.0 - alpha) * self._filtered

        self.outputs["raw"] = raw
        self.outputs["scaled"] = scaled
        self.outputs["filtered"] = self._filtered
