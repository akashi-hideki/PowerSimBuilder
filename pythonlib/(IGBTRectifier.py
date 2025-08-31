# igbt_rectifier.py
from typing import Dict, Any
import math

class IGBTRectifier(Component):
    def __init__(self, comp_id: str):
        super().__init__(comp_id, "IGBTRectifier")
        self.initialize_parameters()

    def initialize_parameters(self):
        self.parameters = {
            'switching_frequency': 10000.0,
            'dead_time': 2e-6,
            'sat_voltage': 2.0,
            'on_resistance': 0.01
        }
        
        self.inputs = {
            'ac_voltage': [0.0, 0.0, 0.0],
            'gate_signals': [0.0, 0.0, 0.0],
            'dc_link_voltage': 0.0,
            'temperature': 25.0
        }
        
        self.outputs = {
            'dc_current': 0.0,
            'output_power': 0.0,
            'switching_loss': 0.0,
            'conduction_loss': 0.0,
            'efficiency': 0.0
        }
        
        # 内部状態
        self.switching_state = [0, 0, 0]

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        # パラメータ取得
        f_sw = self.parameters['switching_frequency']
        V_sat = self.parameters['sat_voltage']
        R_on = self.parameters['on_resistance']
        
        # 入力取得
        V_a, V_b, V_c = self.inputs['ac_voltage']
        gate_signals = self.inputs['gate_signals']
        V_dc = self.inputs['dc_link_voltage']
        
        # PWM制御に基づくスイッチング状態の決定
        # 単純化されたモデル：ゲート信号が0.5以上でON
        new_switching_state = [1 if g > 0.5 else 0 for g in gate_signals]
        
        # デッドタイム考慮（簡易）
        if new_switching_state != self.switching_state:
            # スイッチング損失計算
            E_sw = 0.5 * V_dc * self.outputs['dc_current'] * (self.parameters['dead_time'] * f_sw)
        else:
            E_sw = 0
            
        self.switching_state = new_switching_state
        
        # 整流動作（単純化されたモデル）
        # 導通損失計算
        I_dc = sum(self.switching_state) * V_dc / (3 * R_on) if R_on > 0 else 0
        P_cond = I_dc**2 * R_on * 3
        
        # 総損失
        P_loss = P_cond + E_sw
        
        # 出力電力
        P_out = V_dc * I_dc - P_loss
        
        # 出力更新
        self.outputs['dc_current'] = I_dc
        self.outputs['output_power'] = P_out
        self.outputs['switching_loss'] = E_sw
        self.outputs['conduction_loss'] = P_cond
        self.outputs['efficiency'] = (P_out / (P_out + P_loss)) * 100 if (P_out + P_loss) > 0 else 0