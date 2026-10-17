"""StaTableControllerBase - 層 (Tab) 単位の状態機械。

StaTable XML の StateMachine 要素を 1 クラスとして表現する。
各層は Component を継承し、priority 昇順で StaTableRuntime から
update() を呼ばれる。
"""

from __future__ import annotations

from typing import Any

from ..component_base import Component


class StaTableControllerBase(Component):
    """StaTable の 1 層 (Tab) を表す基底クラス。

    サブクラスは以下のクラス属性を必ず定義する:
        LAYER_NAME: str
        LAYER_PRIORITY: int
        INITIAL_STATE: str
        TRANSITIONS: list[tuple[src, evt, condition, action, target]]
        EVENTS: dict[event_name, dict]
    """

    LAYER_NAME: str = ""
    LAYER_PRIORITY: int = 5
    INITIAL_STATE: str = ""
    TRANSITIONS: list[tuple] = []
    EVENTS: dict = {}

    def __init__(
        self,
        comp_id: str,
        comp_type: str,
        layer_name: str,
        layer_priority: int,
        initial_state: str,
        transitions: list[tuple],
        events: dict,
    ) -> None:
        super().__init__(comp_id, comp_type)
        self.layer_name = layer_name
        self.layer_priority = layer_priority
        self._initial_state = initial_state
        self.transitions = list(transitions)
        self.events = dict(events)

        self.state: str = initial_state
        self.pending_events: list[tuple[str, Any]] = []
        self._initialized: bool = False

        self.inputs: dict[str, Any] = {}
        self.outputs: dict[str, Any] = {"current_state": initial_state}

        # StaTableRuntime から _bind_runtime() で設定される
        self._runtime: Any = None

    # ------------------------------------------------------------------
    # Runtime binding
    # ------------------------------------------------------------------

    def _bind_runtime(self, runtime: Any) -> None:
        """StaTableRuntime から呼ばれる。queue_event の配送先解決に使う。"""
        self._runtime = runtime

    # ------------------------------------------------------------------
    # Component API
    # ------------------------------------------------------------------

    def reset(self) -> None:
        """初期状態に戻す。"""
        self.state = self._initial_state
        self.pending_events = []
        self._initialized = False
        self.inputs.clear()
        self.outputs = {"current_state": self._initial_state}

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        """1 ステップ分の更新。

        初回のみ初期化フラグを立てる。state は __init__ で初期化済みのため
        ここでは変更しない（queue 配送で遷移した state を保持するため）。
        runtime 未接続時のフォールバックとして pending_events を処理する。
        """
        if not self._initialized:
            self.outputs["current_state"] = self.state
            self._initialized = True

        # runtime 未接続時のフォールバック処理
        if self.pending_events:
            pending = self.pending_events
            self.pending_events = []
            for evt_name, data in pending:
                self._dispatch(evt_name, data)

    # ------------------------------------------------------------------
    # Event dispatch
    # ------------------------------------------------------------------

    def fire_event(self, event_name: str, data: Any = None) -> bool:
        """direct イベントを即時処理する。

        Returns:
            遷移が発生したら True。
        """
        return self._dispatch(event_name, data)

    def queue_event(
        self,
        event_name: str,
        data: Any = None,
        target_layer: str | None = None,
    ) -> None:
        """queue イベントを配送キューに積む。

        Args:
            event_name: イベント名。
            data: イベントデータ。
            target_layer: 配送先層名。
                None          : 自分自身（runtime 未接続時は pending_events）。
                "*"           : 送信元以外の全層にブロードキャスト。
                その他の文字列 : その層のみ。

        Raises:
            RuntimeError: runtime 未接続で target_layer が自分以外の場合。
        """
        if target_layer is None:
            target_layer = self.layer_name

        if self._runtime is None:
            if target_layer == self.layer_name:
                self.pending_events.append((event_name, data))
                return
            raise RuntimeError(
                f"queue_event(target_layer={target_layer!r}) requires runtime binding "
                "(StaTableRuntime)"
            )

        self._runtime.enqueue(self.layer_name, event_name, data, target_layer)

    def _dispatch(self, event_name: str, data: Any = None) -> bool:
        """現在の状態で event_name に一致する遷移を探して実行。"""
        for entry in self.transitions:
            src, evt, condition, action, target = entry
            if src != self.state:
                continue
            if evt != event_name:
                continue
            if not self._evaluate_condition(condition, data):
                continue
            self._execute_action(action, data)
            self.state = target
            self.outputs["current_state"] = target
            return True
        return False

    def _evaluate_condition(self, condition: Any, data: Any) -> bool:
        """条件を評価。Phase 1 では None / 空文字のみ True。"""
        if condition is None:
            return True
        if isinstance(condition, str) and condition.strip() == "":
            return True
        return True

    def _execute_action(self, action: Any, data: Any) -> None:
        """アクションを実行。Phase 1 では何もしない。"""
        if action is None:
            return
        if isinstance(action, str) and action.strip() == "":
            return
        return

    # ------------------------------------------------------------------
    # Introspection
    # ------------------------------------------------------------------

    def get_state(self) -> str:
        return self.state

    def get_available_events(self) -> list[str]:
        """現在の状態で発火可能なイベント名一覧。"""
        result: list[str] = []
        for entry in self.transitions:
            src, evt, _cond, _act, _tgt = entry
            if src == self.state and evt not in result:
                result.append(evt)
        return result

    def __repr__(self) -> str:  # pragma: no cover
        return f"{self.type}(id={self.id!r}, layer={self.layer_name!r}, state={self.state!r})"
