"""InductionMotor: simplified slip-based induction motor model."""

from __future__ import annotations

import math
from typing import Any

from ..component_base import Component


class InductionMotor(Component):
    """Simplified induction motor with slip-based torque curve.

    Uses a generic torque-slip characteristic (Kloss formula) rather
    than a specific field-oriented control implementation.  No
    d-q axis transformation, sensorless control, or manufacturer-
    specific modulation is modelled.

    Parameters:
        rated_power: rated mechanical power [W].
        rated_speed: rated speed [rpm].
        load_torque: mechanical load torque [N*m].
        poles: number of poles.
        frequency: supply frequency [Hz].
        breakdown_torque_ratio: T_break / T_rated (default 2.5).
        initial_speed: initial rotor speed [rpm].
        inertia: rotor inertia [kg*m^2].
        rated_voltage: rated voltage [V] for torque scaling.

    Inputs:
        voltage: applied RMS voltage [V].

    Outputs:
        torque: electromagnetic torque [N*m].
        speed: rotor speed [rpm].
        slip: slip (0..1).
    """

    def __init__(self, comp_id: str, comp_type: str = "InductionMotor") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "rated_power": 1500.0,
            "rated_speed": 1450.0,
            "load_torque": 5.0,
            "poles": 4,
            "frequency": 50.0,
            "breakdown_torque_ratio": 2.5,
            "initial_speed": 0.0,
            "inertia": 0.01,
            "rated_voltage": 200.0,
        }
        self.inputs = {"voltage": 0.0}
        self.outputs = {"torque": 0.0, "speed": 0.0, "slip": 0.0}
        self._omega: float = 0.0

    def reset(self) -> None:
        self.inputs = {"voltage": 0.0}
        self.outputs = {"torque": 0.0, "speed": 0.0, "slip": 0.0}
        n0 = float(self.parameters.get("initial_speed", 0.0))
        self._omega = n0 * 2.0 * math.pi / 60.0

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        v_rms = float(self.inputs.get("voltage", 0.0))
        f = float(self.parameters.get("frequency", 50.0))
        poles = int(self.parameters.get("poles", 4))
        p_rated = float(self.parameters.get("rated_power", 1500.0))
        n_rated = float(self.parameters.get("rated_speed", 1450.0))
        t_load = float(self.parameters.get("load_torque", 5.0))
        k_break = float(self.parameters.get("breakdown_torque_ratio", 2.5))
        inertia = float(self.parameters.get("inertia", 0.01))
        v_rated = float(self.parameters.get("rated_voltage", 200.0))

        n_sync_rpm = 120.0 * f / poles
        omega_sync = n_sync_rpm * 2.0 * math.pi / 60.0

        if omega_sync > 0.0:
            slip = (omega_sync - self._omega) / omega_sync
        else:
            slip = 1.0
        slip = max(0.0, min(1.0, slip))

        omega_rated = n_rated * 2.0 * math.pi / 60.0
        t_rated = p_rated / omega_rated if omega_rated > 0.0 else 0.0
        t_break = k_break * t_rated

        s_break = 0.2
        if 0.001 < slip <= 1.0:
            t_em = 2.0 * t_break / (slip / s_break + s_break / slip)
        else:
            t_em = 0.0

        if v_rated > 0.0:
            v_ratio = v_rms / v_rated
        else:
            v_ratio = 0.0
        t_em *= v_ratio**2

        if inertia > 0.0:
            alpha = (t_em - t_load) / inertia
        else:
            alpha = 0.0
        self._omega += alpha * dt
        if self._omega < 0.0:
            self._omega = 0.0

        self.outputs["torque"] = t_em
        self.outputs["speed"] = self._omega * 60.0 / (2.0 * math.pi)
        self.outputs["slip"] = slip
