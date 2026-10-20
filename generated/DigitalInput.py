"""
Digital Inputコンポーネント
自動生成コード
生成日時: 2026/10/20 20:58:18
"""

import math
from typing import Dict, List, Tuple, Any
from dataclasses import dataclass
from enum import Enum
from abc import ABC, abstractmethod

try:
    from component_base import Component
except ImportError:
    # フォールバック定義
    class Component:
        def __init__(self, comp_id: str, comp_type: str):
            self.id = comp_id
            self.type = comp_type
            self.inputs = {}
            self.outputs = {}
            self.parameters = {}

        def update(self, t: float, dt: float, sim_data: Any = None):
            pass

@dataclass
class DigitalInputParameters:
    """Digital Inputパラメータのデータクラス"""
    debounce_ms: float = 0
    initial: int = 0

class DigitalInput(Component):
    """Digital Inputコンポーネント"""

    def __init__(self, comp_id: str, params: DigitalInputParameters = None):
        super().__init__(comp_id, "DigitalInput")
        self.params = params if params else DigitalInputParameters()

        # 入力ポートの定義
        self.inputs = {
            'state': 0.0,
        }

        # 出力ポートの定義
        self.outputs = {
            'state': 0.0,
            'edge_rising': 0.0,
            'edge_falling': 0.0,
        }

        # パラメータの設定
        self.parameters = {
            'debounce_ms': self.params.debounce_ms,
            'initial': self.params.initial,
        }

        # 内部状態変数
        self._last_update_time = 0.0

    def update(self, t: float, dt: float, sim_data: Any = None) -> None:
        """コンポーネントの状態を更新"""
        # 入力値の取得
        state = self.inputs['state']
        # STABLE_USER_CODE_START
        # ここにコンポーネントのロジックを実装
        # 例: 簡単な計算や状態更新
        # ユーザーコードはこのマーカー内に記述してください
        # STABLE_USER_CODE_END

        # 出力値の設定
        self.outputs['state'] = 0.0  # 実際の計算結果に置き換える
        self.outputs['edge_rising'] = 0.0  # 実際の計算結果に置き換える
        self.outputs['edge_falling'] = 0.0  # 実際の計算結果に置き換える

        self._last_update_time = t

    def reset(self):
        """状態をリセット"""
        self._last_update_time = 0.0

        # 出力を初期化
        for key in self.outputs:
            if isinstance(self.outputs[key], list):
                self.outputs[key] = [0.0] * len(self.outputs[key])
            else:
                self.outputs[key] = 0.0

# 使用例
if __name__ == "__main__":
    # Digital Inputのインスタンスを作成
    component = DigitalInput("digitalinput")

    # 入力の設定
    component.inputs['state'] = 0.0  # 適切な値に設定

    # シミュレーションループの例
    dt = 0.001  # 時間ステップ
    for t in range(100):
        component.update(t * dt, dt)
        # 結果の利用
        # state: {component.outputs['state']}
        # edge_rising: {component.outputs['edge_rising']}
        # edge_falling: {component.outputs['edge_falling']}
