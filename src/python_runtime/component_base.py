"""Component base class.

SPEC_POWERSIM.md §2.2 準拠。全ての入出力コンポーネントの基底。
"""

from __future__ import annotations

from typing import Any


class Component:
    """すべてのコンポーネントの基底クラス。

    Attributes:
        id: コンポーネント識別子。
        type: コンポーネント種別名。
        parameters: 静的パラメータ辞書。
        inputs: 入力ポート辞書。
        outputs: 出力ポート辞書。
    """

    def __init__(self, comp_id: str, comp_type: str) -> None:
        self.id: str = comp_id
        self.type: str = comp_type
        self.parameters: dict[str, Any] = {}
        self.inputs: dict[str, Any] = {}
        self.outputs: dict[str, Any] = {}

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        """1 ステップ分の状態更新。

        サブクラスでオーバーライドする。既定では何もしない。
        """
        return None

    def reset(self) -> None:
        """内部状態と入出力を初期化する。"""
        self.inputs.clear()
        self.outputs.clear()

    def __repr__(self) -> str:  # pragma: no cover - 表示のみ
        return f"{self.type}(id={self.id!r})"
