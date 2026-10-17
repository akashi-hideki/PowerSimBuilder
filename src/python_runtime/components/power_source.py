"""PowerSource: single-phase AC voltage source."""

from __future__ import annotations

import math
from typing import Any

from ..component_base import Component


class PowerSource(Component):
    """Single-phase AC voltage source with configurable waveform.

    Parameters:
        voltage: RMS voltage [V].
        frequency: frequency [Hz].
        waveform: "sine" | "square" | "triangle".
        phase: phase offset [rad].
        resistance: load resistance for current computation [ohm].

    Inputs: (none)

    Outputs:
        voltage: instantaneous output voltage [V].
        current: V / resistance [A] (0 if resistance <= 0).
    """

    def __init__(self, comp_id: str, comp_type: str = "PowerSource") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "voltage": 100.0,
            "frequency": 50.0,
            "waveform": "sine",
            "phase": 0.0,
            "resistance": 10.0,
        }
        self.inputs: dict[str, float] = {}
        self.outputs = {"voltage": 0.0, "current": 0.0}

    def reset(self) -> None:
        self.inputs = {}
        self.outputs = {"voltage": 0.0, "current": 0.0}

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        v_rms = float(self.parameters.get("voltage", 100.0))
        f = float(self.parameters.get("frequency", 50.0))
        waveform = str(self.parameters.get("waveform", "sine")).lower()
        phase = float(self.parameters.get("phase", 0.0))
        r_load = float(self.parameters.get("resistance", 10.0))

        v_peak = v_rms * math.sqrt(2.0)
        theta = 2.0 * math.pi * f * t + phase

        if waveform == "square":
            v = v_peak if math.sin(theta) >= 0.0 else -v_peak
        elif waveform == "triangle":
            x = (theta / (2.0 * math.pi)) % 1.0
            tri = 4.0 * abs(x - 0.5) - 1.0
            v = v_peak * tri
        else:
            v = v_peak * math.sin(theta)

        current = (v / r_load) if r_load > 0.0 else 0.0
        self.outputs["voltage"] = v
        self.outputs["current"] = current
