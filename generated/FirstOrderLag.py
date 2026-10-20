"""
First Order Lagコンポーネント
自動生成コード
生成日時: 2026/10/20 20:24:37
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
class FirstOrderLagParameters:
    """First Order Lagパラメータのデータクラス"""
    time_constant: float = 1
    initial_output: float = 0

class FirstOrderLag(Component):
    """First Order Lagコンポーネント"""

    def __init__(self, comp_id: str, params: FirstOrderLagParameters = None):
        super().__init__(comp_id, "FirstOrderLag")
        self.params = params if params else FirstOrderLagParameters()

        # 入力ポートの定義
        self.inputs = {
            'input': 0.0,
        }

        # 出力ポートの定義
        self.outputs = {
            'output': 0.0,
        }

        # パラメータの設定
        self.parameters = {
            'time_constant': self.params.time_constant,
            'initial_output': self.params.initial_output,
        }

        # 内部状態変数
        self._last_update_time = 0.0

    def update(self, t: float, dt: float, sim_data: Any = None) -> None:
        """コンポーネントの状態を更新"""
        # 入力値の取得
        input = self.inputs['input']
        # STABLE_USER_CODE_START
        # ここにコンポーネントのロジックを実装
        # 例: 簡単な計算や状態更新
        # ユーザーコードはこのマーカー内に記述してください
        # STABLE_USER_CODE_END

        # 出力値の設定
        self.outputs['output'] = 0.0  # 実際の計算結果に置き換える

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
    # First Order Lagのインスタンスを作成
    component = FirstOrderLag("firstorderlag")

    # 入力の設定
    component.inputs['input'] = 0.0  # 適切な値に設定

    # シミュレーションループの例
    dt = 0.001  # 時間ステップ
    for t in range(100):
        component.update(t * dt, dt)
        # 結果の利用
        # output: {component.outputs['output']}
