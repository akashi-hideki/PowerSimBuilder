# pwm_chopper.py
from typing import Dict, Any
import math

class PWMChopper(Component):
    def __init__(self, comp_id: str):
        super().__init__(comp_id, "PWMChopper")
        self.initialize_parameters()

    def initialize_parameters(self):
        self.parameters = {
            'switching_frequency': 20000.0,
            'max_duty_cycle': 0.95,
            'min_duty_cycle': 0.05,
            'inductance': 0.001,
            'capacitance': 0.0001
        }
        
        self.inputs = {
            'input_voltage': 0.0,
            'duty_cycle': 0.5,
            'load_current': 0.0,
            'temperature': 25.0
        }
        
        self.outputs = {
            'output_voltage': 0.0,
            'output_current': 0.0,
            'ripple_current': 0.0,
            'efficiency': 0.0,
            'power_loss': 0.0
        }
        
        # 内部状態
        self.inductor_current = 0.0
        self.capacitor_voltage = 0.0
        self.prev_inductor_current = 0.0

    def update(self, t: float, dt: float, sim_data: Any) -> None:
        # パラメータ取得
        f_sw = self.parameters['switching_frequency']
        L = self.parameters['inductance']
        C = self.parameters['capacitance']
        
        # 入力取得
        V_in = self.inputs['input_voltage']
        duty = max(self.parameters['min_duty_cycle'], 
                  min(self.parameters['max_duty_cycle'], 
                      self.inputs['duty_cycle']))
        I_load = self.inputs['load_current']
        
        # PWM動作（平均モデル）
        V_avg = V_in * duty
        
        # インダクタ電流の変化
        dI_dt = (V_avg - self.capacitor_voltage) / L
        self.inductor_current += dI_dt * dt
        
        # キャパシタ電圧の変化
        dV_dt = (self.inductor_current - I_load) / C
        self.capacitor_voltage += dV_dt * dt
        
        # リップル電流計算
        ripple_current = abs(self.inductor_current - self.prev_inductor_current)
        
        # 効率計算（簡易）
        P_in = V_in * self.inductor_current
        P_out = self.capacitor_voltage * I_load
        efficiency = (P_out / P_in) * 100 if P_in > 0 else 0
        
        # 出力更新
        self.outputs['output_voltage'] = self.capacitor_voltage
        self.outputs['output_current'] = I_load
        self.outputs['ripple_current'] = ripple_current
        self.outputs['efficiency'] = efficiency
        self.outputs['power_loss'] = P_in - P_out
        
        self.prev_inductor_current = self.inductor_current