"""SineWave source: y = offset + A * sin(2*pi*f*t + phase)."""

from __future__ import annotations

import math
from typing import Any

from ..component_base import Component


class SineWave(Component):
    """Pure sine-wave signal source.

    Parameters:
        amplitude: peak amplitude (any unit).
        frequency: frequency [Hz].
        phase: phase offset [rad].
        offset: DC offset (added after oscillation).
        start_time: time at which the wave starts [s]; before this, value=offset.
        stop_time: time at which the wave stops [s]; after this, value=offset.
                   Use a negative value to disable.

    Inputs: (none)

    Outputs:
        value: instantaneous value.
        phase_rad: current phase [rad] (informational).
    """

    def __init__(self, comp_id: str, comp_type: str = "SineWave") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "amplitude": 1.0,
            "frequency": 1.0,
            "phase": 0.0,
            "offset": 0.0,
            "start_time": 0.0,
            "stop_time": -1.0,
        }
        self.inputs: dict[str, float] = {}
        self.outputs = {"value": 0.0, "phase_rad": 0.0}

    def reset(self) -> None:
        self.outputs = {"value": 0.0, "phase_rad": 0.0}

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        A = float(self.parameters.get("amplitude", 1.0))
        f = float(self.parameters.get("frequency", 1.0))
        ph = float(self.parameters.get("phase", 0.0))
        off = float(self.parameters.get("offset", 0.0))
        t0 = float(self.parameters.get("start_time", 0.0))
        t1 = float(self.parameters.get("stop_time", -1.0))

        active = t >= t0 and (t1 < 0.0 or t < t1)
        if not active:
            self.outputs["value"] = off
            self.outputs["phase_rad"] = 0.0
            return

        phase = 2.0 * math.pi * f * (t - t0) + ph
        self.outputs["value"] = off + A * math.sin(phase)
        self.outputs["phase_rad"] = phase
