"""ThermalMass: generic lumped thermal capacitance."""

from __future__ import annotations

from typing import Any

from ..component_base import Component


class ThermalMass(Component):
    """Lumped thermal mass with input heat flow.

    Model:
        dT/dt = (Q_in - Q_out) / C_th
        Q_out = (T - T_amb) / R_th

    Parameters:
        thermal_resistance: R_th [K/W].
        thermal_capacity: C_th [J/K].
        ambient_temperature: T_amb [degC].
        initial_temperature: T(0) [degC].

    Inputs:
        heat_flow: incoming heat [W].

    Outputs:
        temperature: body temperature [degC].
        heat_loss: heat lost to ambient [W].
    """

    def __init__(self, comp_id: str, comp_type: str = "ThermalMass") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "thermal_resistance": 1.0,
            "thermal_capacity": 1000.0,
            "ambient_temperature": 25.0,
            "initial_temperature": 25.0,
        }
        self.inputs = {"heat_flow": 0.0}
        self.outputs = {
            "temperature": float(self.parameters["initial_temperature"]),
            "heat_loss": 0.0,
        }

    def reset(self) -> None:
        self.inputs = {"heat_flow": 0.0}
        self.outputs = {
            "temperature": float(self.parameters.get("initial_temperature", 25.0)),
            "heat_loss": 0.0,
        }

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        R_th = float(self.parameters.get("thermal_resistance", 1.0))
        C_th = float(self.parameters.get("thermal_capacity", 1000.0))
        T_amb = float(self.parameters.get("ambient_temperature", 25.0))

        Q_in = float(self.inputs.get("heat_flow", 0.0))
        T = float(self.outputs.get("temperature", T_amb))
        Q_out = (T - T_amb) / R_th if R_th > 0.0 else 0.0

        dT = (Q_in - Q_out) / C_th
        T += dT * dt

        self.outputs["temperature"] = T
        self.outputs["heat_loss"] = Q_out
