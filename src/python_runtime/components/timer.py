"""Timer component: delay / oneshot / interval."""

from __future__ import annotations

from typing import Any

from ..component_base import Component


class Timer(Component):
    """Timer with three modes.

    Parameters:
        mode: "delay" | "oneshot" | "interval".
        duration: seconds.

    Inputs:
        trigger: bool
        reset: bool

    Outputs:
        done: bool
        running: bool
        elapsed: float (seconds)

    Modes:
        delay: done becomes True after trigger stays high >= duration.
        oneshot: on rising edge, done stays True for exactly duration.
        interval: while trigger is high, done toggles every duration.
    """

    def __init__(self, comp_id: str, comp_type: str = "Timer") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {"mode": "delay", "duration": 1.0}
        self.inputs = {"trigger": False, "reset": False}
        self.outputs = {"done": False, "running": False, "elapsed": 0.0}
        self._start_t: float | None = None
        self._prev_trigger: bool = False
        self._interval_state: bool = False
        self._last_toggle_t: float | None = None

    def reset(self) -> None:
        self._start_t = None
        self._prev_trigger = False
        self._interval_state = False
        self._last_toggle_t = None
        self.outputs = {"done": False, "running": False, "elapsed": 0.0}

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        trigger = bool(self.inputs.get("trigger", False))
        do_reset = bool(self.inputs.get("reset", False))
        mode = str(self.parameters.get("mode", "delay"))
        duration = float(self.parameters.get("duration", 1.0))

        if do_reset:
            self.reset()
            self._prev_trigger = trigger
            return

        if mode == "delay":
            if trigger:
                if self._start_t is None:
                    self._start_t = t
                elapsed = t - self._start_t
                done = elapsed >= duration
                self.outputs["elapsed"] = elapsed
                self.outputs["running"] = not done
                self.outputs["done"] = done
            else:
                self._start_t = None
                self.outputs["elapsed"] = 0.0
                self.outputs["running"] = False
                self.outputs["done"] = False

        elif mode == "oneshot":
            rising = trigger and not self._prev_trigger
            if rising:
                self._start_t = t
            if self._start_t is not None:
                elapsed = t - self._start_t
                if elapsed < duration:
                    self.outputs["done"] = True
                    self.outputs["running"] = True
                    self.outputs["elapsed"] = elapsed
                else:
                    self.outputs["done"] = False
                    self.outputs["running"] = False
                    self.outputs["elapsed"] = duration
                    self._start_t = None
            else:
                self.outputs["done"] = False
                self.outputs["running"] = False
                self.outputs["elapsed"] = 0.0

        elif mode == "interval":
            if trigger:
                if self._last_toggle_t is None:
                    self._last_toggle_t = t
                    self._interval_state = True
                elif (t - self._last_toggle_t) >= duration:
                    self._last_toggle_t = t
                    self._interval_state = not self._interval_state
                self.outputs["done"] = self._interval_state
                self.outputs["running"] = True
                self.outputs["elapsed"] = t - self._last_toggle_t
            else:
                self._last_toggle_t = None
                self._interval_state = False
                self.outputs["done"] = False
                self.outputs["running"] = False
                self.outputs["elapsed"] = 0.0

        self._prev_trigger = trigger
