"""DiodeBridge: generic AC-to-DC rectifier (full-wave / half-wave)."""

from __future__ import annotations

import math
from typing import Any

from ..component_base import Component


class DiodeBridge(Component):
    """Generic diode bridge rectifier with ripple estimation.

    A simplified model based on the physical equations of diode
    conduction.  No switching pattern or modulation scheme is
    modelled; only the voltage drop across the diodes and the
    resulting DC output with optional smoothing capacitance.

    Parameters:
        diode_drop: forward voltage drop per diode [V].
        type: "full" (full-wave) | "half" (half-wave) | "three_phase".
        smoothing_capacitance: filter capacitor [F] (0 = no filter).
        load_resistance: load R [ohm] for ripple computation.

    Inputs:
        ac_voltage: instantaneous AC voltage [V].

    Outputs:
        dc_voltage: rectified DC voltage [V].
        ripple: estimated ripple voltage [V].
    """

    def __init__(self, comp_id: str, comp_type: str = "DiodeBridge") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "diode_drop": 0.7,
            "type": "full",
            "smoothing_capacitance": 0.0,
            "load_resistance": 10.0,
        }
        self.inputs = {"ac_voltage": 0.0}
        self.outputs = {"dc_voltage": 0.0, "ripple": 0.0}
        self._v_peak: float = 0.0

    def reset(self) -> None:
        self.inputs = {"ac_voltage": 0.0}
        self.outputs = {"dc_voltage": 0.0, "ripple": 0.0}
        self._v_peak = 0.0

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        v_ac = float(self.inputs.get("ac_voltage", 0.0))
        v_drop = float(self.parameters.get("diode_drop", 0.7))
        rect_type = str(self.parameters.get("type", "full")).lower()

        v_abs = abs(v_ac)

        if rect_type == "half":
            v_rect = v_abs - v_drop if v_ac > v_drop else 0.0
        elif rect_type == "three_phase":
            v_rect = (v_abs * math.sqrt(3.0)) - (2.0 * v_drop)
        else:
            v_rect = v_abs - (2.0 * v_drop)

        v_rect = max(0.0, v_rect)

        if v_rect > self._v_peak:
            self._v_peak = v_rect

        c_smooth = float(self.parameters.get("smoothing_capacitance", 0.0))
        r_load = float(self.parameters.get("load_resistance", 10.0))

        if c_smooth > 0.0 and r_load > 0.0:
            i_load = v_rect / r_load
            f_ripple = 100.0 if rect_type == "full" else 50.0
            ripple = i_load / (f_ripple * c_smooth)
        else:
            ripple = self._v_peak - v_rect

        self.outputs["dc_voltage"] = v_rect
        self.outputs["ripple"] = ripple
