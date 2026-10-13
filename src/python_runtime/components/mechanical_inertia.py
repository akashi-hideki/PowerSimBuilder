"""MechanicalInertia: J * domega/dt = T_drive - T_load - B*omega."""

from __future__ import annotations

import math
from typing import Any

from ..component_base import Component


class MechanicalInertia(Component):
    """Rotational inertia with viscous friction.

    Model:
        J * domega/dt = T_drive - T_load - B * omega

    Parameters:
        inertia: J [kg*m^2].
        viscous_friction: B [N*m*s/rad].
        initial_speed: omega(0) [rad/s].

    Inputs:
        torque_drive: driving torque [N*m].
        torque_load: load torque [N*m].

    Outputs:
        speed: angular speed [rad/s].
        speed_rpm: angular speed [rpm].
        accel: angular acceleration [rad/s^2].
    """

    def __init__(self, comp_id: str, comp_type: str = "MechanicalInertia") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "inertia": 0.01,
            "viscous_friction": 0.0,
            "initial_speed": 0.0,
        }
        self.inputs = {"torque_drive": 0.0, "torque_load": 0.0}
        self.outputs = {
            "speed": float(self.parameters["initial_speed"]),
            "speed_rpm": float(self.parameters["initial_speed"]) * 60.0 / (2.0 * math.pi),
            "accel": 0.0,
        }

    def reset(self) -> None:
        self.inputs = {"torque_drive": 0.0, "torque_load": 0.0}
        w0 = float(self.parameters.get("initial_speed", 0.0))
        self.outputs = {
            "speed": w0,
            "speed_rpm": w0 * 60.0 / (2.0 * math.pi),
            "accel": 0.0,
        }

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        J = float(self.parameters.get("inertia", 0.01))
        B = float(self.parameters.get("viscous_friction", 0.0))
        Td = float(self.inputs.get("torque_drive", 0.0))
        Tl = float(self.inputs.get("torque_load", 0.0))
        w = float(self.outputs.get("speed", 0.0))

        if J <= 0.0:
            a = 0.0
        else:
            a = (Td - Tl - B * w) / J

        w += a * dt

        self.outputs["speed"] = w
        self.outputs["speed_rpm"] = w * 60.0 / (2.0 * math.pi)
        self.outputs["accel"] = a
