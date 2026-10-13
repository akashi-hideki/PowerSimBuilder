"""OverCurrentDetector: threshold-based over-current trip with hysteresis."""

from __future__ import annotations

from typing import Any

from ..component_base import Component


class OverCurrentDetector(Component):
    """Over-current detector with hysteresis and latching option.

    Parameters:
        threshold: trip threshold [A].
        hysteresis: reset margin below threshold [A].
        latch: if True, stays tripped until reset input is high.
        initial_tripped: initial trip state.

    Inputs:
        current: measured current [A].
        reset: truthy value clears a latched trip.

    Outputs:
        tripped: 1.0 if tripped, else 0.0.
        peak: highest observed current [A].
    """

    def __init__(self, comp_id: str, comp_type: str = "OverCurrentDetector") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "threshold": 10.0,
            "hysteresis": 1.0,
            "latch": True,
            "initial_tripped": False,
        }
        self.inputs = {"current": 0.0, "reset": 0.0}
        self.outputs = {
            "tripped": 1.0 if self.parameters["initial_tripped"] else 0.0,
            "peak": 0.0,
        }

    def reset(self) -> None:
        self.inputs = {"current": 0.0, "reset": 0.0}
        self.outputs = {
            "tripped": 1.0 if self.parameters.get("initial_tripped", False) else 0.0,
            "peak": 0.0,
        }

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        thr = float(self.parameters.get("threshold", 10.0))
        hys = float(self.parameters.get("hysteresis", 1.0))
        latch = bool(self.parameters.get("latch", True))

        i = abs(float(self.inputs.get("current", 0.0)))
        reset = float(self.inputs.get("reset", 0.0)) != 0.0

        # peak tracking
        peak = max(float(self.outputs.get("peak", 0.0)), i)
        self.outputs["peak"] = peak

        tripped = bool(self.outputs.get("tripped", 0.0))

        if reset:
            tripped = False
        elif not tripped and i >= thr:
            tripped = True
        elif tripped and not latch and i < (thr - hys):
            tripped = False

        self.outputs["tripped"] = 1.0 if tripped else 0.0
