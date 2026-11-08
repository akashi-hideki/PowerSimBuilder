"""RecordedSignal: replay one variable from a .psblog log file."""

from __future__ import annotations

from typing import Any

from ..component_base import Component


class RecordedSignal(Component):
    """Replay a single variable from a .psblog log file.

    Parameters:
        psblog_path: Path to the .psblog file (absolute recommended).
        var_name: Variable name to replay.
        loop: If True, restart from beginning when reaching the end.
        hold_last: If True (and loop=False), hold the last value at end.

    Inputs:
        (none)

    Outputs:
        value: current sample value from the log.
    """

    def __init__(self, comp_id: str, comp_type: str = "RecordedSignal") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "psblog_path": "",
            "var_name": "",
            "loop": False,
            "hold_last": True,
        }
        self.inputs = {}
        self.outputs = {"value": 0.0}
        self._series: list[tuple[float, float]] | None = None
        self._idx: int = 0
        self._t0: float = 0.0

    def reset(self) -> None:
        self.inputs = {}
        self.outputs = {"value": 0.0}
        self._series = None
        self._idx = 0
        self._t0 = 0.0

    def _ensure_loaded(self) -> None:
        if self._series is not None:
            return
        path = str(self.parameters.get("psblog_path", "") or "")
        var_name = str(self.parameters.get("var_name", "") or "")
        if not path or not var_name:
            self._series = []
            return
        try:
            from python_runtime.psblog import parse_psblog

            trace = parse_psblog(path)
            self._series = trace.series(var_name)
            self._t0 = self._series[0][0] if self._series else 0.0
        except (FileNotFoundError, ValueError, OSError):
            self._series = []

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        self._ensure_loaded()
        if not self._series:
            self.outputs["value"] = 0.0
            return

        target = t - self._t0
        series = self._series
        end_t = series[-1][0]

        if bool(self.parameters.get("loop", False)) and end_t > 0.0:
            target = target % end_t

        if target >= end_t:
            hold = bool(self.parameters.get("hold_last", True))
            self.outputs["value"] = series[-1][1] if hold else series[0][1]
            return

        if target <= series[0][0]:
            self.outputs["value"] = series[0][1]
            return

        idx = self._idx
        if idx > 0 and series[idx][0] > target:
            idx = 0
        while idx + 1 < len(series) and series[idx + 1][0] <= target:
            idx += 1
        self._idx = idx

        t0, v0 = series[idx]
        if idx + 1 < len(series):
            t1, v1 = series[idx + 1]
            if t1 > t0:
                alpha = (target - t0) / (t1 - t0)
                self.outputs["value"] = v0 + alpha * (v1 - v0)
                return
        self.outputs["value"] = v0
