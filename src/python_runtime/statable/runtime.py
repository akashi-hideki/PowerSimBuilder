"""StaTableRuntime - 全層を束ねるランタイム。

priority 昇順で層を update し、層間の queue イベントを配る。
"""

from __future__ import annotations

from typing import Any

from .controller_base import StaTableControllerBase


class StaTableRuntime:
    """StaTable プロジェクトのランタイム状態。

    Attributes:
        layers: priority 昇順にソート済みの層リスト。
        globals: GlobalDefinitions.SystemVariables の値。
        layers_by_name: 層名 -> 層インスタンス。
    """

    def __init__(
        self,
        layers: list[StaTableControllerBase],
        global_variables: dict[str, Any] | None = None,
    ) -> None:
        # priority 昇順にソート（安定ソート）
        self.layers: list[StaTableControllerBase] = sorted(
            layers, key=lambda layer: layer.layer_priority
        )
        self.globals: dict[str, Any] = dict(global_variables or {})
        self.layers_by_name: dict[str, StaTableControllerBase] = {
            layer.layer_name: layer for layer in self.layers
        }

    # ------------------------------------------------------------------
    # Tick
    # ------------------------------------------------------------------

    def tick(self, t: float, dt: float, sim_data: Any) -> None:
        """1 ステップ分、priority 昇順に全層を更新する。"""
        for layer in self.layers:
            layer.update(t, dt, sim_data)

    def reset(self) -> None:
        """全層をリセット。"""
        for layer in self.layers:
            layer.reset()

    # ------------------------------------------------------------------
    # Event dispatch
    # ------------------------------------------------------------------

    def fire_event(self, layer_name: str, event_name: str, data: Any = None) -> bool:
        """指定層の指定イベントを即時発火する。"""
        layer = self.layers_by_name.get(layer_name)
        if layer is None:
            return False
        return layer.fire_event(event_name, data)

    def queue_event(self, layer_name: str, event_name: str, data: Any = None) -> bool:
        """指定層に queue イベントを追加する。"""
        layer = self.layers_by_name.get(layer_name)
        if layer is None:
            return False
        layer.queue_event(event_name, data)
        return True

    # ------------------------------------------------------------------
    # Introspection
    # ------------------------------------------------------------------

    def get_layer_states(self) -> dict[str, str]:
        """全層の現在状態を返す。"""
        return {layer.layer_name: layer.state for layer in self.layers}

    def __repr__(self) -> str:  # pragma: no cover
        return f"StaTableRuntime(layers={len(self.layers)})"
