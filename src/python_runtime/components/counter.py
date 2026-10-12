"""Counter component."""

from __future__ import annotations

from typing import Any

from ..component_base import Component


class Counter(Component):
    """Up/down counter with overflow and underflow.

    Parameters:
        min_count, max_count: bounds.
        initial: starting count.
        direction: "up" | "down" | "updown".
        wrap: if True, wrap around on limit; else saturate.

    Inputs:
        clock: bool (rising edge increments/decrements).
        reset: bool.

    Outputs:
        count: int
        overflow: bool
        underflow: bool
    """

    def __init__(self, comp_id: str, comp_type: str = "Counter") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "min_count": 0,
            "max_count": 10,
            "initial": 0,
            "direction": "up",
            "wrap": False,
        }
        self.inputs = {"clock": False, "reset": False}
        self.outputs = {"count": 0, "overflow": False, "underflow": False}
        self._prev_clock: bool = False

    def reset(self) -> None:
        init = int(self.parameters.get("initial", 0))
        self.outputs["count"] = init
        self.outputs["overflow"] = False
        self.outputs["underflow"] = False
        self._prev_clock = False

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        clock = bool(self.inputs.get("clock", False))
        do_reset = bool(self.inputs.get("reset", False))

        if do_reset:
            self.reset()
            return

        rising = clock and not self._prev_clock
        self._prev_clock = clock

        if not rising:
            return

        self.outputs["overflow"] = False
        self.outputs["underflow"] = False

        count = int(self.outputs.get("count", 0))
        mn = int(self.parameters.get("min_count", 0))
        mx = int(self.parameters.get("max_count", 10))
        direction = str(self.parameters.get("direction", "up"))
        wrap = bool(self.parameters.get("wrap", False))

        if direction == "down":
            count -= 1
            if count < mn:
                if wrap:
                    count = mx
                else:
                    count = mn
                    self.outputs["underflow"] = True
        else:
            count += 1
            if count > mx:
                if wrap:
                    count = mn
                else:
                    count = mx
                    self.outputs["overflow"] = True

        self.outputs["count"] = count
