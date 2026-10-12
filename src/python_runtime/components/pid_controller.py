"""PIDController component."""
from __future__ import annotations

from typing import Any

from ..component_base import Component


def _clamp(v: float, lo: float, hi: float) -> float:
    return max(lo, min(hi, v))


class PIDController(Component):
    """Discrete-time PID controller.

    Parameters:
        kp, ki, kd: gains.
        out_min, out_max: output clamp range.
        setpoint: target value (can be overridden via inputs).
        anti_windup: if True, prevent integral windup when saturated.

    Inputs:
        measured: current process value.
        setpoint: optional per-step override.

    Outputs:
        control: clamped controller output.
        error: setpoint - measured.
        p_term, i_term, d_term: individual contributions.
        saturated: True when output was clamped.
    """

    def __init__(self, comp_id: str, comp_type: str = "PIDController") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "kp": 1.0,
            "ki": 0.0,
            "kd": 0.0,
            "out_min": -1e9,
            "out_max": 1e9,
            "setpoint": 0.0,
            "anti_windup": False,
        }
        self.inputs = {"measured": 0.0, "setpoint": None}
        self.outputs = {
            "control": 0.0,
            "error": 0.0,
            "p_term": 0.0,
            "i_term": 0.0,
            "d_term": 0.0,
            "saturated": False,
        }
        self._integral: float = 0.0
        self._prev_error: float = 0.0
        self._initialized: bool = False

    def reset(self) -> None:
        self._integral = 0.0
        self._prev_error = 0.0
        self._initialized = False

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        sp_in = self.inputs.get("setpoint")
        setpoint = float(sp_in) if sp_in is not None else float(
            self.parameters.get("setpoint", 0.0)
        )
        measured = float(self.inputs.get("measured", 0.0))
        kp = float(self.parameters.get("kp", 1.0))
        ki = float(self.parameters.get("ki", 0.0))
        kd = float(self.parameters.get("kd", 0.0))
        out_min = float(self.parameters.get("out_min", -1e9))
        out_max = float(self.parameters.get("out_max", 1e9))
        anti_windup = bool(self.parameters.get("anti_windup", False))

        error = setpoint - measured

        if not self._initialized:
            self._prev_error = error
            self._initialized = True

        p_term = kp * error
        i_term = ki * self._integral
        d_term = kd * (error - self._prev_error) / dt if dt > 0 else 0.0

        raw = p_term + i_term + d_term
        control = _clamp(raw, out_min, out_max)
        saturated = (control != raw)

        if not (anti_windup and saturated):
            self._integral += error * dt

        self._prev_error = error
        self.outputs["control"] = control
        self.outputs["error"] = error
        self.outputs["p_term"] = p_term
        self.outputs["i_term"] = i_term
        self.outputs["d_term"] = d_term
        self.outputs["saturated"] = saturated
