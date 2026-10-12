"""AnalogOutput component: PWM duty conversion."""

from __future__ import annotations

from typing import Any

from ..component_base import Component


class AnalogOutput(Component):
    """Analog output modeled as PWM.

    Parameters:
        v_ref: reference output voltage [V].
        pwm_freq: PWM frequency [Hz] (informational).
        resolution: PWM resolution [bit] (informational).

    Inputs:
        value: target value (0..1, clamped).

    Outputs:
        duty: duty cycle (0..1).
        voltage: v_ref * duty [V].
        active: True when duty > 0.
    """

    def __init__(self, comp_id: str, comp_type: str = "AnalogOutput") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {"v_ref": 5.0, "pwm_freq": 1000.0, "resolution": 10}
        self.inputs = {"value": 0.0}
        self.outputs = {"duty": 0.0, "voltage": 0.0, "active": False}

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        v = float(self.inputs.get("value", 0.0))
        duty = max(0.0, min(1.0, v))
        v_ref = float(self.parameters.get("v_ref", 5.0))

        self.outputs["duty"] = duty
        self.outputs["voltage"] = v_ref * duty
        self.outputs["active"] = duty > 0.0
