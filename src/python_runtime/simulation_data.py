"""SimulationData and stimulus injection.

SPEC_POWERSIM.md §2.1 準拠 + 机上検証用の stimulus / logs 機構。
"""

from __future__ import annotations

from collections.abc import Callable, Iterable
from typing import Any

StimulusSeries = Iterable[tuple[float, Any]]
StimulusSource = Callable[[float], Any] | StimulusSeries


class SimulationData:
    """シミュレーション全体のパラメータと刺激入力を保持する。"""

    def __init__(self, time_step: float = 0.001) -> None:
        self.time_step: float = time_step
        self.t: float = 0.0
        self.logs: list[dict[str, Any]] = []
        # path -> ("callable", fn) | ("series", [(t, v), ...])
        self.stimulus: dict[str, tuple[str, Any]] = {}

    # ---- stimulus ----

    def set_stimulus(self, path: str, source: StimulusSource) -> None:
        """入力刺激を登録する。

        Args:
            path: "comp_id.inputs.port_name" 形式。
            source: 呼び出し可能 `f(t)->value`、または `(t, value)` のリスト。
        """
        if callable(source):
            self.stimulus[path] = ("callable", source)
        else:
            series = sorted(source, key=lambda tv: tv[0])
            self.stimulus[path] = ("series", series)

    def get_stimulus_value(self, path: str, t: float) -> Any:
        """指定時刻 t での刺激値を返す。未登録なら None。"""
        if path not in self.stimulus:
            return None
        kind, data = self.stimulus[path]
        if kind == "callable":
            return data(t)
        # series: 最後に t 以下である値を返す（ステップ補間）
        value = None
        for tt, vv in data:
            if tt <= t:
                value = vv
            else:
                break
        return value

    def apply_stimulus(self, components: dict[str, Any], t: float) -> None:
        """登録済みの刺激をコンポーネントの inputs に反映する。"""
        for path in self.stimulus:
            parts = path.split(".")
            if len(parts) != 3:
                continue
            cid, section, port = parts
            if section != "inputs":
                continue
            comp = components.get(cid)
            if comp is None:
                continue
            comp.inputs[port] = self.get_stimulus_value(path, t)

    # ---- logging ----

    def log(self, t: float, components: dict[str, Any]) -> None:
        """現在時刻のコンポーネント状態をスナップショットとして記録する。"""
        self.logs.append(
            {
                "time": t,
                "components": {
                    cid: {
                        "inputs": comp.inputs.copy(),
                        "outputs": comp.outputs.copy(),
                        "parameters": comp.parameters.copy(),
                    }
                    for cid, comp in components.items()
                },
            }
        )

    def clear_logs(self) -> None:
        self.logs.clear()
