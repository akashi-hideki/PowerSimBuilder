"""PWM3Phase: 3-phase PWM duty generator (sinusoidal reference)."""

from __future__ import annotations

import math
from typing import Any

from ..component_base import Component

_SQRT3_2 = math.sqrt(3.0) / 2.0


class PWM3Phase(Component):
    """Three-phase PWM duty calculator.

    Generates normalized duties in [0, 1] for a three-phase inverter using
    a reference sine and (by default) a space-vector-like common-mode offset.
    The carrier is not explicitly modelled; only duties are produced.

    Parameters:
        amplitude: reference peak amplitude (0..1 typical).
        frequency: reference frequency [Hz].
        carrier_frequency: informational (not simulated) [Hz].
        common_mode: if True, applies (min+max)/2 offset to increase linear range.

    Inputs: (none)

    Outputs:
        da, db, dc: duty ratios in [0, 1].
        modulation_index: amplitude (informational).
    """

    def __init__(self, comp_id: str, comp_type: str = "PWM3Phase") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "amplitude": 0.9,
            "frequency": 50.0,
            "carrier_frequency": 10000.0,
            "common_mode": True,
        }
        self.inputs: dict[str, float] = {}
        self.outputs = {
            "da": 0.5,
            "db": 0.5,
            "dc": 0.5,
            "modulation_index": 0.0,
        }

    def reset(self) -> None:
        self.outputs = {
            "da": 0.5,
            "db": 0.5,
            "dc": 0.5,
            "modulation_index": 0.0,
        }

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        A = float(self.parameters.get("amplitude", 0.9))
        f = float(self.parameters.get("frequency", 50.0))
        cm = bool(self.parameters.get("common_mode", True))

        theta = 2.0 * math.pi * f * t
        ma = A * math.sin(theta)
        mb = A * math.sin(theta - 2.0 * math.pi / 3.0)
        mc = A * math.sin(theta + 2.0 * math.pi / 3.0)

        if cm:
            offset = -(max(ma, mb, mc) + min(ma, mb, mc)) / 2.0
            ma += offset
            mb += offset
            mc += offset

        # Duty = 0.5 * (1 + m), clamped to [0, 1]
        da = 0.5 * (1.0 + ma)
        db = 0.5 * (1.0 + mb)
        dc = 0.5 * (1.0 + mc)

        da = max(0.0, min(1.0, da))
        db = max(0.0, min(1.0, db))
        dc = max(0.0, min(1.0, dc))

        self.outputs["da"] = da
        self.outputs["db"] = db
        self.outputs["dc"] = dc
        self.outputs["modulation_index"] = A
