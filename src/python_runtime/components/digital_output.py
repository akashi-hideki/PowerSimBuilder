"""DigitalOutput component: active_low aware."""

from __future__ import annotations

from typing import Any

from ..component_base import Component


class DigitalOutput(Component):
    """Digital output with active_low support.

    Parameters:
        active_low: if True, physical level is inverted.
        initial: initial command (0 or 1).
        on_current_ma: load current when ON [mA].

    Inputs:
        command: logical command (0 or 1).

    Outputs:
        state: physical pin level (inverted if active_low).
        current_draw: load current [mA] (non-zero only when command is 1).
    """

    def __init__(self, comp_id: str, comp_type: str = "DigitalOutput") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {"active_low": False, "initial": 0, "on_current_ma": 20.0}
        self.inputs = {"command": 0}
        self.outputs = {"state": 0, "current_draw": 0.0}

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        cmd = 1 if self.inputs.get("command", 0) else 0
        active_low = bool(self.parameters.get("active_low", False))
        on_current = float(self.parameters.get("on_current_ma", 20.0))

        physical = (1 - cmd) if active_low else cmd

        self.outputs["state"] = physical
        self.outputs["current_draw"] = on_current if cmd == 1 else 0.0
