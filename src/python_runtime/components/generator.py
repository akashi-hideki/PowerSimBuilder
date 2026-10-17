"""Generator: torque-driven AC generator (V proportional to torque)."""

from __future__ import annotations

from typing import Any

from ..component_base import Component


class Generator(Component):
    """Torque-driven generator with efficiency and saturation caps.

    Simplified model:
        V = efficiency * torque * voltage_per_torque   (capped at rated_voltage)
        I = V / load_resistance                        (0 if R <= 0)
        P = min(V * I, rated_power)

    Parameters:
        rated_power: maximum output power [W].
        rated_voltage: maximum output voltage [V].
        efficiency: mechanical-to-electrical efficiency (0..1).
        voltage_per_torque: gain k_v [V/(N*m)].
        load_resistance: load R [ohm] (<=0 disables current).

    Inputs:
        torque: driving torque [N*m].

    Outputs:
        voltage: output voltage [V].
        current: output current [A].
        power: output power [W].
    """

    def __init__(self, comp_id: str, comp_type: str = "Generator") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "rated_power": 5000.0,
            "rated_voltage": 200.0,
            "efficiency": 0.9,
            "voltage_per_torque": 10.0,
            "load_resistance": 10.0,
        }
        self.inputs = {"torque": 0.0}
        self.outputs = {"voltage": 0.0, "current": 0.0, "power": 0.0}

    def reset(self) -> None:
        self.inputs = {"torque": 0.0}
        self.outputs = {"voltage": 0.0, "current": 0.0, "power": 0.0}

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        torque = float(self.inputs.get("torque", 0.0))
        eta = float(self.parameters.get("efficiency", 0.9))
        kv = float(self.parameters.get("voltage_per_torque", 10.0))
        r_load = float(self.parameters.get("load_resistance", 10.0))
        p_rated = float(self.parameters.get("rated_power", 5000.0))
        v_rated = float(self.parameters.get("rated_voltage", 200.0))

        v = eta * torque * kv
        if v > v_rated:
            v = v_rated
        if v < 0.0:
            v = 0.0

        current = (v / r_load) if r_load > 0.0 else 0.0
        power = v * current
        if power > p_rated:
            power = p_rated

        self.outputs["voltage"] = v
        self.outputs["current"] = current
        self.outputs["power"] = power
