"""StaTableRuntime - 全層を束ねるランタイム。

priority 昇順で層を update し、層間の queue イベントを配る。
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any

from .controller_base import StaTableControllerBase


@dataclass
class _QueuedEvent:
    """runtime が配送待ちの queue イベント。"""

    source_layer: str
    event_name: str
    data: Any = None
    target_layer: str | None = None


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
        # queue イベント配送キュー（次 tick 先頭で drain される）
        self._event_queue: list[_QueuedEvent] = []
        # 各層に runtime を逆参照させる（queue_event の配送先解決のため）
        for layer in self.layers:
            layer._bind_runtime(self)

    # ------------------------------------------------------------------
    # Tick
    # ------------------------------------------------------------------

    def tick(self, t: float, dt: float, sim_data: Any) -> None:
        """1 ステップ分、priority 昇順に全層を更新する。

        先頭で前 tick までに溜まった queue イベントを配送し、
        その後で各層を update する（PLC の scan cycle と同じ順序）。
        """
        # 1. queue イベント配送（前 tick で enqueue されたもの）
        self._drain_event_queue()
        # 2. 各層 update
        for layer in self.layers:
            layer.update(t, dt, sim_data)

    def reset(self) -> None:
        """全層と queue をリセット。"""
        self._event_queue = []
        for layer in self.layers:
            layer.reset()

    # ------------------------------------------------------------------
    # Event dispatch
    # ------------------------------------------------------------------

    def fire_event(self, layer_name: str, event_name: str, data: Any = None) -> bool:
        """指定層の指定イベントを即時発火する（direct 相当）。"""
        layer = self.layers_by_name.get(layer_name)
        if layer is None:
            return False
        return layer.fire_event(event_name, data)

    def queue_event(self, layer_name: str, event_name: str, data: Any = None) -> bool:
        """指定層を送信元として queue イベントを投入する。

        実際の配送は次 tick の先頭で行われる。
        """
        layer = self.layers_by_name.get(layer_name)
        if layer is None:
            return False
        self.enqueue(layer_name, event_name, data, layer_name)
        return True

    def enqueue(
        self,
        source_layer: str,
        event_name: str,
        data: Any = None,
        target_layer: str | None = None,
    ) -> None:
        """queue イベントを配送キューに投入する。

        Args:
            source_layer: 送信元層名。
            event_name: イベント名。
            data: イベントデータ。
            target_layer: 配送先層名。
                None: 送信元自身。
                "*": 送信元以外の全層。
                その他: その層のみ。
        """
        self._event_queue.append(_QueuedEvent(source_layer, event_name, data, target_layer))

    # ------------------------------------------------------------------
    # Internal
    # ------------------------------------------------------------------

    def _drain_event_queue(self) -> None:
        if not self._event_queue:
            return
        pending = self._event_queue
        self._event_queue = []
        # 送信元の priority 昇順で安定ソート
        pending.sort(key=self._source_priority)
        for ev in pending:
            self._deliver(ev)

    def _source_priority(self, ev: _QueuedEvent) -> int:
        src = self.layers_by_name.get(ev.source_layer)
        return src.layer_priority if src is not None else 10**9

    def _deliver(self, ev: _QueuedEvent) -> None:
        if ev.target_layer == "*":
            # ブロードキャスト: 送信元以外の全層
            for layer in self.layers:
                if layer.layer_name == ev.source_layer:
                    continue
                layer.fire_event(ev.event_name, ev.data)
            return
        # 特定層（None は送信元自身）
        target_name = ev.target_layer if ev.target_layer is not None else ev.source_layer
        target = self.layers_by_name.get(target_name)
        if target is None:
            return
        target.fire_event(ev.event_name, ev.data)

    # ------------------------------------------------------------------
    # Introspection
    # ------------------------------------------------------------------

    @property
    def pending_event_count(self) -> int:
        """配送待ち queue イベント数（テスト用）。"""
        return len(self._event_queue)

    def get_layer_states(self) -> dict[str, str]:
        """全層の現在状態を返す。"""
        return {layer.layer_name: layer.state for layer in self.layers}

    def __repr__(self) -> str:  # pragma: no cover
        return f"StaTableRuntime(layers={len(self.layers)})"
