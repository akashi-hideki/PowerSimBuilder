"""HeaterBlock: electrical heater with first-order thermal model."""

from __future__ import annotations

from typing import Any

from ..component_base import Component


class HeaterBlock(Component):
    """Electrical heater with lumped thermal mass.

    Model:
        R  = V_rated^2 / P_rated
        P  = V^2 / R
        dT/dt = (P - (T - T_amb) / R_th) / C_th

    Parameters:
        rated_power: rated power [W].
        rated_voltage: rated voltage [V].
        thermal_resistance: R_th [K/W].
        thermal_capacity: C_th [J/K].
        ambient_temperature: T_amb [degC].
        initial_temperature: T(0) [degC].

    Inputs:
        voltage: applied voltage [V].

    Outputs:
        power: dissipated power [W].
        temperature: heater temperature [degC].
        resistance: heater resistance [ohm].
        current: current [A].
    """

    def __init__(self, comp_id: str, comp_type: str = "HeaterBlock") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "rated_power": 1000.0,
            "rated_voltage": 200.0,
            "thermal_resistance": 1.0,
            "thermal_capacity": 1000.0,
            "ambient_temperature": 25.0,
            "initial_temperature": 25.0,
        }
        self.inputs = {"voltage": 0.0}
        self.outputs = {
            "power": 0.0,
            "temperature": float(self.parameters["initial_temperature"]),
            "resistance": 0.0,
            "current": 0.0,
        }

    def reset(self) -> None:
        self.inputs = {"voltage": 0.0}
        self.outputs = {
            "power": 0.0,
            "temperature": float(self.parameters.get("initial_temperature", 25.0)),
            "resistance": 0.0,
            "current": 0.0,
        }

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        P_rated = float(self.parameters.get("rated_power", 1000.0))
        V_rated = float(self.parameters.get("rated_voltage", 200.0))
        R_th = float(self.parameters.get("thermal_resistance", 1.0))
        C_th = float(self.parameters.get("thermal_capacity", 1000.0))
        T_amb = float(self.parameters.get("ambient_temperature", 25.0))

        if P_rated <= 0.0:
            R = 1e9
        else:
            R = (V_rated * V_rated) / P_rated

        V = float(self.inputs.get("voltage", 0.0))
        P = (V * V) / R if R > 0.0 else 0.0
        I = V / R if R > 0.0 else 0.0

        T = float(self.outputs.get("temperature", T_amb))
        dT = (P - (T - T_amb) / R_th) / C_th
        T += dT * dt

        self.outputs["power"] = P
        self.outputs["temperature"] = T
        self.outputs["resistance"] = R
        self.outputs["current"] = I
