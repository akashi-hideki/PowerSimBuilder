"""Engine: prime mover producing torque with startup ramp."""

from __future__ import annotations

from typing import Any

from ..component_base import Component


class Engine(Component):
    """Prime mover producing mechanical torque.

    A real engine idles at low torque, then ramps up to ``torque_max``
    over ``ramp_time`` seconds when commanded on.  This simplified model
    starts at ``idle_ratio * torque_max`` and reaches ``torque_max``
    after ``ramp_time``.

    Parameters:
        power: rated mechanical power [W] (informational).
        torque_max: maximum output torque [N*m].
        ramp_time: seconds to reach torque_max from idle [s].
        idle_ratio: fraction of torque_max at t=0 (0..1).

    Inputs: (none)

    Outputs:
        torque: current output torque [N*m].
    """

    def __init__(self, comp_id: str, comp_type: str = "Engine") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "power": 5000.0,
            "torque_max": 50.0,
            "ramp_time": 0.1,
            "idle_ratio": 0.1,
        }
        self.inputs: dict[str, float] = {}
        self.outputs = {"torque": 0.0}

    def reset(self) -> None:
        self.inputs = {}
        self.outputs = {"torque": 0.0}

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        t_max = float(self.parameters.get("torque_max", 50.0))
        ramp = float(self.parameters.get("ramp_time", 0.1))
        idle = float(self.parameters.get("idle_ratio", 0.1))

        if ramp > 0.0:
            ratio = min(1.0, t / ramp)
        else:
            ratio = 1.0

        torque = t_max * (idle + (1.0 - idle) * ratio)
        self.outputs["torque"] = torque
