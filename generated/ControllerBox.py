"""
CookingHeaterControllerコンポーネント
自動生成コード
生成日時: 2026/10/20 19:59:27
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
class ControllerBoxParameters:
    """CookingHeaterControllerパラメータのデータクラス"""
    generated_path: str = "C:\dev\PowerSimBuilder\generated\CookingHeaterController_powersim.py"
    auto_events: str = ""

class ControllerBox(Component):
    """CookingHeaterControllerコンポーネント"""

    def __init__(self, comp_id: str, params: ControllerBoxParameters = None):
        super().__init__(comp_id, "ControllerBox")
        self.params = params if params else ControllerBoxParameters()

        # 入力ポートの定義
        self.inputs = {
            'door_open': 0.0,
            'fan_on': 0.0,
            'heater_bottom_on': 0.0,
            'heater_middle_on': 0.0,
            'heater_top_on': 0.0,
            'humidity': 0.0,
            'ir_value': 0.0,
            'mw_anode_current': 0.0,
            'mw_target_power': 0.0,
            'oven_current_temp': 0.0,
            'oven_target_temp': 0.0,
            'pump_pulse_count': 0.0,
            'rx_buf': 0.0,
            'seq_total': 0.0,
            'stage_duration': 0.0,
            'stage_elapsed': 0.0,
            'steam_sensor': 0.0,
            'steam_target_temp': 0.0,
            'thermistor_bottom': 0.0,
            'thermistor_middle': 0.0,
            'thermistor_top': 0.0,
            'zc_count': 0.0,
        }

        # 出力ポートの定義
        self.outputs = {
            'boiler_temp': 0.0,
            'error_code': 0.0,
            'fan_duty': 0.0,
            'fan_on': 0.0,
            'heater_bottom_duty': 0.0,
            'heater_bottom_on': 0.0,
            'heater_middle_duty': 0.0,
            'heater_middle_on': 0.0,
            'heater_top_duty': 0.0,
            'heater_top_on': 0.0,
            'humidity': 0.0,
            'mag_on': 0.0,
            'mw_power': 0.0,
            'oven_current_temp': 0.0,
            'pump_on': 0.0,
            'pump_pulse_count': 0.0,
            'seq_step': 0.0,
            'stage_elapsed': 0.0,
            'stage_mode': 0.0,
            'zc_count': 0.0,
        }

        # パラメータの設定
        self.parameters = {
            'generated_path': self.params.generated_path,
            'auto_events': self.params.auto_events,
        }

        # 内部状態変数
        self._last_update_time = 0.0

    def update(self, t: float, dt: float, sim_data: Any = None) -> None:
        """コンポーネントの状態を更新"""
        # 入力値の取得
        door_open = self.inputs['door_open']
        fan_on = self.inputs['fan_on']
        heater_bottom_on = self.inputs['heater_bottom_on']
        heater_middle_on = self.inputs['heater_middle_on']
        heater_top_on = self.inputs['heater_top_on']
        humidity = self.inputs['humidity']
        ir_value = self.inputs['ir_value']
        mw_anode_current = self.inputs['mw_anode_current']
        mw_target_power = self.inputs['mw_target_power']
        oven_current_temp = self.inputs['oven_current_temp']
        oven_target_temp = self.inputs['oven_target_temp']
        pump_pulse_count = self.inputs['pump_pulse_count']
        rx_buf = self.inputs['rx_buf']
        seq_total = self.inputs['seq_total']
        stage_duration = self.inputs['stage_duration']
        stage_elapsed = self.inputs['stage_elapsed']
        steam_sensor = self.inputs['steam_sensor']
        steam_target_temp = self.inputs['steam_target_temp']
        thermistor_bottom = self.inputs['thermistor_bottom']
        thermistor_middle = self.inputs['thermistor_middle']
        thermistor_top = self.inputs['thermistor_top']
        zc_count = self.inputs['zc_count']
        # STABLE_USER_CODE_START
        # ここにコンポーネントのロジックを実装
        # 例: 簡単な計算や状態更新
        # ユーザーコードはこのマーカー内に記述してください
        # STABLE_USER_CODE_END

        # 出力値の設定
        self.outputs['boiler_temp'] = 0.0  # 実際の計算結果に置き換える
        self.outputs['error_code'] = 0.0  # 実際の計算結果に置き換える
        self.outputs['fan_duty'] = 0.0  # 実際の計算結果に置き換える
        self.outputs['fan_on'] = 0.0  # 実際の計算結果に置き換える
        self.outputs['heater_bottom_duty'] = 0.0  # 実際の計算結果に置き換える
        self.outputs['heater_bottom_on'] = 0.0  # 実際の計算結果に置き換える
        self.outputs['heater_middle_duty'] = 0.0  # 実際の計算結果に置き換える
        self.outputs['heater_middle_on'] = 0.0  # 実際の計算結果に置き換える
        self.outputs['heater_top_duty'] = 0.0  # 実際の計算結果に置き換える
        self.outputs['heater_top_on'] = 0.0  # 実際の計算結果に置き換える
        self.outputs['humidity'] = 0.0  # 実際の計算結果に置き換える
        self.outputs['mag_on'] = 0.0  # 実際の計算結果に置き換える
        self.outputs['mw_power'] = 0.0  # 実際の計算結果に置き換える
        self.outputs['oven_current_temp'] = 0.0  # 実際の計算結果に置き換える
        self.outputs['pump_on'] = 0.0  # 実際の計算結果に置き換える
        self.outputs['pump_pulse_count'] = 0.0  # 実際の計算結果に置き換える
        self.outputs['seq_step'] = 0.0  # 実際の計算結果に置き換える
        self.outputs['stage_elapsed'] = 0.0  # 実際の計算結果に置き換える
        self.outputs['stage_mode'] = 0.0  # 実際の計算結果に置き換える
        self.outputs['zc_count'] = 0.0  # 実際の計算結果に置き換える

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
    # CookingHeaterControllerのインスタンスを作成
    component = ControllerBox("controllerbox")

    # 入力の設定
    component.inputs['door_open'] = 0.0  # 適切な値に設定
    component.inputs['fan_on'] = 0.0  # 適切な値に設定
    component.inputs['heater_bottom_on'] = 0.0  # 適切な値に設定
    component.inputs['heater_middle_on'] = 0.0  # 適切な値に設定
    component.inputs['heater_top_on'] = 0.0  # 適切な値に設定
    component.inputs['humidity'] = 0.0  # 適切な値に設定
    component.inputs['ir_value'] = 0.0  # 適切な値に設定
    component.inputs['mw_anode_current'] = 0.0  # 適切な値に設定
    component.inputs['mw_target_power'] = 0.0  # 適切な値に設定
    component.inputs['oven_current_temp'] = 0.0  # 適切な値に設定
    component.inputs['oven_target_temp'] = 0.0  # 適切な値に設定
    component.inputs['pump_pulse_count'] = 0.0  # 適切な値に設定
    component.inputs['rx_buf'] = 0.0  # 適切な値に設定
    component.inputs['seq_total'] = 0.0  # 適切な値に設定
    component.inputs['stage_duration'] = 0.0  # 適切な値に設定
    component.inputs['stage_elapsed'] = 0.0  # 適切な値に設定
    component.inputs['steam_sensor'] = 0.0  # 適切な値に設定
    component.inputs['steam_target_temp'] = 0.0  # 適切な値に設定
    component.inputs['thermistor_bottom'] = 0.0  # 適切な値に設定
    component.inputs['thermistor_middle'] = 0.0  # 適切な値に設定
    component.inputs['thermistor_top'] = 0.0  # 適切な値に設定
    component.inputs['zc_count'] = 0.0  # 適切な値に設定

    # シミュレーションループの例
    dt = 0.001  # 時間ステップ
    for t in range(100):
        component.update(t * dt, dt)
        # 結果の利用
        # boiler_temp: {component.outputs['boiler_temp']}
        # error_code: {component.outputs['error_code']}
        # fan_duty: {component.outputs['fan_duty']}
        # fan_on: {component.outputs['fan_on']}
        # heater_bottom_duty: {component.outputs['heater_bottom_duty']}
        # heater_bottom_on: {component.outputs['heater_bottom_on']}
        # heater_middle_duty: {component.outputs['heater_middle_duty']}
        # heater_middle_on: {component.outputs['heater_middle_on']}
        # heater_top_duty: {component.outputs['heater_top_duty']}
        # heater_top_on: {component.outputs['heater_top_on']}
        # humidity: {component.outputs['humidity']}
        # mag_on: {component.outputs['mag_on']}
        # mw_power: {component.outputs['mw_power']}
        # oven_current_temp: {component.outputs['oven_current_temp']}
        # pump_on: {component.outputs['pump_on']}
        # pump_pulse_count: {component.outputs['pump_pulse_count']}
        # seq_step: {component.outputs['seq_step']}
        # stage_elapsed: {component.outputs['stage_elapsed']}
        # stage_mode: {component.outputs['stage_mode']}
        # zc_count: {component.outputs['zc_count']}
