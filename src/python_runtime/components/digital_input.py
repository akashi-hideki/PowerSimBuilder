"""DigitalInput component: debounce + edge detection."""

from __future__ import annotations

from typing import Any

from ..component_base import Component


class DigitalInput(Component):
    """Digital input with debounce and edge detection.

    Parameters:
        debounce_ms: debounce time [ms]; 0 disables debounce.
        initial: initial state (0 or 1).

    Inputs:
        state: raw physical input (0 or 1).

    Outputs:
        state: stabilized state after debounce.
        edge_rising: True on rising edge (one step only).
        edge_falling: True on falling edge (one step only).
    """

    def __init__(self, comp_id: str, comp_type: str = "DigitalInput") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {"debounce_ms": 0.0, "initial": 0}
        self.inputs = {"state": 0}
        self.outputs = {"state": 0, "edge_rising": False, "edge_falling": False}
        self._prev_stable: int = 0
        self._pending: int = 0
        self._last_change_t: float = 0.0
        self._initialized: bool = False

    def reset(self) -> None:
        init = int(self.parameters.get("initial", 0))
        self.inputs = {"state": init}
        self.outputs = {"state": init, "edge_rising": False, "edge_falling": False}
        self._prev_stable = init
        self._pending = init
        self._last_change_t = 0.0
        self._initialized = True

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        raw = 1 if self.inputs.get("state", 0) else 0
        debounce_s = float(self.parameters.get("debounce_ms", 0.0)) / 1000.0

        if not self._initialized:
            init = int(self.parameters.get("initial", 0))
            self._prev_stable = init
            self._pending = init
            self._last_change_t = t
            self._initialized = True

        if raw != self._pending:
            self._pending = raw
            self._last_change_t = t

        if debounce_s <= 0.0 or (t - self._last_change_t) >= debounce_s:
            new_stable = self._pending
        else:
            new_stable = self._prev_stable

        self.outputs["edge_rising"] = self._prev_stable == 0 and new_stable == 1
        self.outputs["edge_falling"] = self._prev_stable == 1 and new_stable == 0
        self._prev_stable = new_stable
        self.outputs["state"] = new_stable
