# diode_bridge.py
from typing import Dict, Any
import math

class DiodeBridge(Component):
    def __init__(self, comp_id: str):
        super().__init__(comp_id, "DiodeBridge")
        self.initialize_parameters()

    def initialize_parameters(self):
        self.parameters = {
            'diode_forward_voltage': 0.7,
            'diode_resistance': 0.01,
            'thermal_coefficient': -0.002,
            'bridge_type': 'full_wave'
        }
        
        self.inputs = {
            'ac_voltage': [0.0, 0.0, 0.0],
            'load_resistance': 10.0,
            'temperature': 25.0
        }
        
        self.outputs = {
            'dc_voltage': 0.0,
            'dc_current': 0.0,
            'ripple_voltage': 0.0,
            'efficiency': 0.0,
            'power_loss': 0.0
        }
        
        # 内部状態
        self.prev_output_voltage = 0.0

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        # 温度補正
        temp_diff = self.inputs['temperature'] - 25.0
        V_f = self.parameters['diode_forward_voltage'] + temp_diff * self.parameters['thermal_coefficient']
        R_d = self.parameters['diode_resistance']
        
        # 3相電圧取得
        V_a, V_b, V_c = self.inputs['ac_voltage']
        R_load = self.inputs['load_resistance']
        
        # 整流動作（理想的な全波整流）
        # 各相の整流後の電圧
        V_rect = max(V_a, V_b, V_c) - min(V_a, V_b, V_c)
        
        # ダイオードの順方向電圧降下と抵抗を考慮
        V_dc = V_rect - 2 * V_f  # 2つのダイオードが常に導通
        I_dc = V_dc / (R_load + 2 * R_d)
        
        # リップル電圧計算（簡易）
        ripple = abs(V_dc - self.prev_output_voltage)
        
        # 効率計算
        P_in = (V_a**2 + V_b**2 + V_c**2) / (3 * R_load)  # 簡易計算
        P_out = V_dc * I_dc
        efficiency = (P_out / P_in) * 100 if P_in > 0 else 0
        
        # 出力更新
        self.outputs['dc_voltage'] = V_dc
        self.outputs['dc_current'] = I_dc
        self.outputs['ripple_voltage'] = ripple
        self.outputs['efficiency'] = efficiency
        self.outputs['power_loss'] = P_in - P_out
        
        self.prev_output_voltage = V_dc