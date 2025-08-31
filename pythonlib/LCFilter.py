# lc_filter.py
from typing import Dict, Any
import math

class LCFilter(Component):
    def __init__(self, comp_id: str):
        super().__init__(comp_id, "LCFilter")
        self.initialize_parameters()

    def initialize_parameters(self):
        self.parameters = {
            'inductance': 0.01,
            'capacitance': 0.001,
            'esr_inductor': 0.1,
            'esr_capacitor': 0.01,
            'filter_type': 'low_pass'
        }
        
        self.inputs = {
            'input_voltage': 0.0,
            'input_current': 0.0,
            'load_impedance': 10.0,
            'frequency': 50.0
        }
        
        self.outputs = {
            'output_voltage': 0.0,
            'output_current': 0.0,
            'attenuation': 0.0,
            'resonant_frequency': 0.0,
            'quality_factor': 0.0
        }
        
        # 内部状態変数
        self.inductor_current = 0.0
        self.capacitor_voltage = 0.0

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        # パラメータ取得
        L = self.parameters['inductance']
        C = self.parameters['capacitance']
        R_inductor = self.parameters['esr_inductor']
        R_capacitor = self.parameters['esr_capacitor']
        
        # 入力取得
        V_in = self.inputs['input_voltage']
        I_in = self.inputs['input_current']
        R_load = self.inputs['load_impedance']
        freq = self.inputs['frequency']
        
        # 状態空間表現に基づく更新
        # 簡略化されたLCフィルタの微分方程式
        dI_dt = (V_in - self.capacitor_voltage - self.inductor_current * R_inductor) / L
        dV_dt = (self.inductor_current - self.capacitor_voltage / R_load) / C
        
        # オイラー法による数値積分
        self.inductor_current += dI_dt * dt
        self.capacitor_voltage += dV_dt * dt
        
        # 出力計算
        self.outputs['output_voltage'] = self.capacitor_voltage
        self.outputs['output_current'] = self.capacitor_voltage / R_load
        
        # 特性計算
        self.outputs['resonant_frequency'] = 1 / (2 * math.pi * math.sqrt(L * C))
        
        # 減衰率計算（簡略化）
        w = 2 * math.pi * freq
        if self.parameters['filter_type'] == 'low_pass':
            attenuation = 20 * math.log10(1 / math.sqrt((1 - (w**2 * L * C))**2 + (w * (R_inductor + R_capacitor) / R_load)**2))
            self.outputs['attenuation'] = attenuation
        
        # Q値計算
        self.outputs['quality_factor'] = (1 / (R_inductor + R_capacitor)) * math.sqrt(L / C)