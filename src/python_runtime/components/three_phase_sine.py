"""ThreePhaseSine: balanced 3-phase sinusoidal voltage source."""

from __future__ import annotations

import math
from typing import Any

from ..component_base import Component

_SQRT3_2 = math.sqrt(3.0) / 2.0


class ThreePhaseSine(Component):
    """Balanced three-phase sine source (Ua, Ub, Uc).

    Parameters:
        amplitude: phase peak amplitude [V].
        frequency: frequency [Hz].
        phase: phase offset of Ua [rad].
        sequence: "positive" (abc), "negative" (acb), "zero".

    Inputs: (none)

    Outputs:
        ua, ub, uc: instantaneous phase voltages [V].
        u_alpha, u_beta: Clarke transform (amplitude-invariant).
    """

    def __init__(self, comp_id: str, comp_type: str = "ThreePhaseSine") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "amplitude": 100.0,
            "frequency": 50.0,
            "phase": 0.0,
            "sequence": "positive",
        }
        self.inputs: dict[str, float] = {}
        self.outputs = {
            "ua": 0.0,
            "ub": 0.0,
            "uc": 0.0,
            "u_alpha": 0.0,
            "u_beta": 0.0,
        }

    def reset(self) -> None:
        self.outputs = {
            "ua": 0.0,
            "ub": 0.0,
            "uc": 0.0,
            "u_alpha": 0.0,
            "u_beta": 0.0,
        }

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        A = float(self.parameters.get("amplitude", 100.0))
        f = float(self.parameters.get("frequency", 50.0))
        ph = float(self.parameters.get("phase", 0.0))
        seq = str(self.parameters.get("sequence", "positive"))

        if seq == "negative":
            s = -1.0
        elif seq == "zero":
            s = 0.0
        else:
            s = 1.0

        theta = 2.0 * math.pi * f * t + ph
        ua = A * math.sin(theta)
        ub = A * math.sin(theta - s * 2.0 * math.pi / 3.0)
        uc = A * math.sin(theta + s * 2.0 * math.pi / 3.0)

        u_alpha = (2.0 / 3.0) * (ua - 0.5 * ub - 0.5 * uc)
        u_beta = (2.0 / 3.0) * (_SQRT3_2 * (ub - uc))

        self.outputs["ua"] = ua
        self.outputs["ub"] = ub
        self.outputs["uc"] = uc
        self.outputs["u_alpha"] = u_alpha
        self.outputs["u_beta"] = u_beta
