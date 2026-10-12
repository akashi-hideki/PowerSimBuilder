"""StateMachine component."""

from __future__ import annotations

from typing import Any

from ..component_base import Component


class StateMachine(Component):
    """Finite state machine driven by events.

    Parameters:
        initial_state: str
        transitions: dict mapping "state:event" -> next_state.
        states: optional list of known states.

    Inputs:
        event: str or None
        reset: bool

    Outputs:
        current_state: str
        prev_state: str
        transitioned: bool
    """

    def __init__(self, comp_id: str, comp_type: str = "StateMachine") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "initial_state": "IDLE",
            "transitions": {},
            "states": ["IDLE"],
        }
        self.inputs = {"event": None, "reset": False}
        self.outputs = {
            "current_state": "IDLE",
            "prev_state": "IDLE",
            "transitioned": False,
        }
        self._initialized: bool = False

    def reset(self) -> None:
        init = str(self.parameters.get("initial_state", "IDLE"))
        self.outputs["current_state"] = init
        self.outputs["prev_state"] = init
        self.outputs["transitioned"] = False
        self._initialized = True

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        do_reset = bool(self.inputs.get("reset", False))
        if do_reset:
            self.reset()
            return

        if not self._initialized:
            init = str(self.parameters.get("initial_state", "IDLE"))
            self.outputs["current_state"] = init
            self.outputs["prev_state"] = init
            self._initialized = True

        current = str(self.outputs.get("current_state", "IDLE"))
        event = self.inputs.get("event")

        self.outputs["transitioned"] = False
        self.outputs["prev_state"] = current

        if event is None or event == "":
            return

        transitions = self.parameters.get("transitions", {}) or {}
        key = f"{current}:{event}"
        if key in transitions:
            nxt = str(transitions[key])
            self.outputs["prev_state"] = current
            self.outputs["current_state"] = nxt
            self.outputs["transitioned"] = True
