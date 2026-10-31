"""OvenChamber: oven cavity thermal dynamics with heater inputs and sensor outputs.

Internally wraps ThermalMass (validated at T=200.00 degC in
demo_statable_runtime_loop.py).

Inputs:
    heater_top_duty    (0-1) top heater duty cycle
    heater_bottom_duty (0-1) bottom heater duty cycle
    fan_duty           (0-1) convection fan duty (reserved)

Outputs:
    oven_current_temp  (degC) PID feedback
    thermistor_top     (degC) top sensor
    thermistor_middle  (degC) middle sensor
    thermistor_bottom  (degC) bottom sensor
    heater_power       (W)    total heater power injected
"""

from __future__ import annotations

from typing import Any

from ..component_base import Component
from .thermal_mass import ThermalMass


class OvenChamber(Component):
    def __init__(self, comp_id: str, comp_type: str = "OvenChamber") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "thermal_resistance": 0.5,
            "thermal_capacity": 200.0,
            "ambient_temperature": 25.0,
            "initial_temperature": 25.0,
            "heater_power_max": 1000.0,
            "sensor_offset_top": 5.0,
            "sensor_offset_bottom": -5.0,
        }
        self.inputs = {
            "heater_top_duty": 0.0,
            "heater_bottom_duty": 0.0,
            "fan_duty": 0.0,
        }
        T0 = float(self.parameters["initial_temperature"])
        self.outputs = {
            "oven_current_temp": T0,
            "thermistor_top": T0 + float(self.parameters["sensor_offset_top"]),
            "thermistor_middle": T0,
            "thermistor_bottom": T0 + float(self.parameters["sensor_offset_bottom"]),
            "heater_power": 0.0,
        }
        self._thermal = ThermalMass(f"{comp_id}_inner", "ThermalMass")
        self._sync_params_to_thermal()

    def _sync_params_to_thermal(self) -> None:
        for k in (
            "thermal_resistance",
            "thermal_capacity",
            "ambient_temperature",
            "initial_temperature",
        ):
            self._thermal.parameters[k] = self.parameters[k]

    def reset(self) -> None:
        self.inputs = {
            "heater_top_duty": 0.0,
            "heater_bottom_duty": 0.0,
            "fan_duty": 0.0,
        }
        T0 = float(self.parameters.get("initial_temperature", 25.0))
        self.outputs = {
            "oven_current_temp": T0,
            "thermistor_top": T0 + float(self.parameters.get("sensor_offset_top", 5.0)),
            "thermistor_middle": T0,
            "thermistor_bottom": T0 + float(self.parameters.get("sensor_offset_bottom", -5.0)),
            "heater_power": 0.0,
        }
        self._sync_params_to_thermal()
        self._thermal.reset()

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        self._sync_params_to_thermal()
        top = float(self.inputs.get("heater_top_duty", 0.0))
        bot = float(self.inputs.get("heater_bottom_duty", 0.0))
        p_max = float(self.parameters.get("heater_power_max", 1000.0))
        Q_in = (max(0.0, top) + max(0.0, bot)) * p_max

        self._thermal.inputs["heat_flow"] = Q_in
        self._thermal.update(t, dt, sim_data)
        T = float(self._thermal.outputs.get("temperature", 25.0))

        self.outputs["oven_current_temp"] = T
        self.outputs["thermistor_top"] = T + float(self.parameters.get("sensor_offset_top", 5.0))
        self.outputs["thermistor_middle"] = T
        self.outputs["thermistor_bottom"] = T + float(
            self.parameters.get("sensor_offset_bottom", -5.0)
        )
        self.outputs["heater_power"] = Q_in
