"""FaultDevice: scenario-driven fault injector."""

from __future__ import annotations

from typing import Any

from ..component_base import Component


class FaultDevice(Component):
    """Scenario-driven fault injector.

    Evaluates ``condition_expr`` each tick.  When the condition stays true
    for ``duration`` seconds, the configured fault is latched until
    :meth:`reset`.

    Parameters:
        fault_type: "none" | "short" | "open" | "inrush".
        condition_expr: Python expression evaluated with {t, dt}.
        duration: delay after condition becomes true [s].
        inrush_current: injected current for "inrush" [A].

    Inputs: (none)

    Outputs:
        short_circuit: True when latched for fault_type="short".
        open_circuit: True when latched for fault_type="open".
        inrush_current: configured current for fault_type="inrush".
    """

    def __init__(self, comp_id: str, comp_type: str = "FaultDevice") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "fault_type": "none",
            "condition_expr": "False",
            "duration": 0.0,
            "inrush_current": 0.0,
        }
        self.inputs: dict[str, float] = {}
        self.outputs = {
            "short_circuit": False,
            "open_circuit": False,
            "inrush_current": 0.0,
        }
        self._scenario_state: dict[str, Any] = {
            "t_start_cond": None,
            "triggered": False,
        }

    def reset(self) -> None:
        self.inputs = {}
        self.outputs = {
            "short_circuit": False,
            "open_circuit": False,
            "inrush_current": 0.0,
        }
        self._scenario_state = {
            "t_start_cond": None,
            "triggered": False,
        }

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        expr = str(self.parameters.get("condition_expr", "False"))
        try:
            # Restricted eval: only t and dt are visible.
            cond = bool(
                eval(  # noqa: B307
                    expr,
                    {"__builtins__": {}},
                    {"t": t, "dt": dt},
                )
            )
        except Exception:
            cond = False

        if cond:
            if self._scenario_state["t_start_cond"] is None:
                self._scenario_state["t_start_cond"] = t
            else:
                elapsed = t - self._scenario_state["t_start_cond"]
                duration = float(self.parameters.get("duration", 0.0))
                if elapsed + 1e-12 >= duration:
                    if not self._scenario_state["triggered"]:
                        self._apply_fault()
                        self._scenario_state["triggered"] = True
        else:
            self._scenario_state["t_start_cond"] = None
            self._scenario_state["triggered"] = False

    def _apply_fault(self) -> None:
        fault_type = str(self.parameters.get("fault_type", "none")).lower()
        if fault_type == "short":
            self.outputs["short_circuit"] = True
        elif fault_type == "open":
            self.outputs["open_circuit"] = True
        elif fault_type == "inrush":
            self.outputs["inrush_current"] = float(self.parameters.get("inrush_current", 0.0))
