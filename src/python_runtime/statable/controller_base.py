"""StaTableControllerBase - 層 (Tab) 単位の状態機械。

StaTable XML の StateMachine 要素を 1 クラスとして表現する。
各層は Component を継承し、priority 昇順で StaTableRuntime から
update() を呼ばれる。
"""

from __future__ import annotations

from typing import Any

from ..component_base import Component

# 条件式 / アクション式の eval() / exec() で利用可能な builtins（サンドボックス）
_SAFE_BUILTINS = {
    "abs": abs,
    "min": min,
    "max": max,
    "round": round,
    "int": int,
    "float": float,
    "bool": bool,
    "str": str,
    "len": len,
    "True": True,
    "False": False,
    "None": None,
}


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

        # Phase 2-B-2: 条件式 / アクション式から参照されるグローバル変数
        # （runtime 未接続時はローカル、_bind_runtime で runtime.globals を共有）
        self.globals: dict[str, Any] = {}

        # Phase 2-B-2: eval / exec 失敗時の警告蓄積
        self.warnings: list[str] = []

        # StaTableRuntime から _bind_runtime() で設定される
        self._runtime: Any = None

    # ------------------------------------------------------------------
    # Runtime binding
    # ------------------------------------------------------------------

    def _bind_runtime(self, runtime: Any) -> None:
        """StaTableRuntime から呼ばれる。

        queue_event の配送先解決と、グローバル変数の共有に使う。
        """
        self._runtime = runtime
        # runtime.globals を参照共有（同じ dict オブジェクト）
        self.globals = runtime.globals

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
        self.warnings.clear()

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

    # ------------------------------------------------------------------
    # Condition / Action evaluation (Phase 2-B-2)
    # ------------------------------------------------------------------

    def _evaluate_condition(self, condition: Any, data: Any) -> bool:
        """条件式を Python 式として評価する。

        Phase 2-B-2: eval() による評価。サンドボックス化のため
        builtins を _SAFE_BUILTINS に制限し、self のみ公開する。

        - None / 空文字は True（無条件遷移）
        - 評価エラー時は False（安全側）で、warning を記録
        """
        if condition is None:
            return True
        if isinstance(condition, str) and condition.strip() == "":
            return True

        scope = {"__builtins__": _SAFE_BUILTINS, "self": self}
        try:
            return bool(eval(condition, scope))  # noqa: S307
        except Exception as exc:  # noqa: BLE001
            self.warnings.append(f"condition eval failed: {condition!r}: {exc}")
            return False

    def _execute_action(self, action: Any, data: Any) -> None:
        """アクション式を Python 文として実行する。

        Phase 2-B-2: exec() による実行。condition と同じサンドボックス。
        エラー時は warning を記録するが、遷移自体は継続する。
        """
        if action is None:
            return
        if isinstance(action, str) and action.strip() == "":
            return

        scope = {"__builtins__": _SAFE_BUILTINS, "self": self}
        try:
            exec(action, scope)  # noqa: S102
        except Exception as exc:  # noqa: BLE001
            self.warnings.append(f"action exec failed: {action!r}: {exc}")

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
