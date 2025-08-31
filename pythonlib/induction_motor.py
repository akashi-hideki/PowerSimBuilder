import math
import numpy as np
from typing import Dict, List, Any
from dataclasses import dataclass
from component_base import Component

@dataclass
class InductionMotorParameters:
    """誘導電動機パラメータのデータクラス"""
    rated_power: float = 5.0                 # 定格出力 [kW]
    rated_voltage: float = 400.0             # 定格電圧 [V]
    rated_frequency: float = 50.0            # 定格周波数 [Hz]
    rated_speed: float = 1450.0              # 定格回転数 [rpm]
    pole_pairs: int = 2                      # 極対数
    stator_resistance: float = 0.2           # 固定子抵抗 [Ω]
    rotor_resistance: float = 0.15           # 回転子抵抗 [Ω]
    stator_leakage_inductance: float = 0.002 # 固定子漏れインダクタンス [H]
    rotor_leakage_inductance: float = 0.002  # 回転子漏れインダクタンス [H]
    mutual_inductance: float = 0.07          # 相互インダクタンス [H]
    inertia: float = 0.1                     # 回転子慣性 [kg·m²]
    friction_coefficient: float = 0.01       # 摩擦係数 [Nm/(rad/s)]
    thermal_time_constant: float = 300.0     # 熱時定数 [s]
    max_temperature: float = 120.0           # 最大許容温度 [°C]

class InductionMotor(Component):
    """
    誘導電動機コンポーネント
    dq変換を用いた動的モデルを実装
    """
    
    def __init__(self, comp_id: str, params: InductionMotorParameters = None):
        super().__init__(comp_id, "InductionMotor")
        self.params = params if params else InductionMotorParameters()
        
        # 入力ポートの定義
        self.inputs = {
            'stator_voltage': [0.0, 0.0, 0.0],  # 固定子電圧（3相）[V]
            'supply_frequency': 50.0,           # 電源周波数 [Hz]
            'load_torque': 0.0,                 # 負荷トルク [Nm]
            'inertia': self.params.inertia,     # 負荷慣性 [kg·m²]
            'mechanical_damping': self.params.friction_coefficient, # 機械的減衰 [Nm/(rad/s)]
        }
        
        # 出力ポートの定義
        self.outputs = {
            'rotor_speed': 0.0,                 # 回転子速度 [rpm]
            'electrical_angle': 0.0,            # 電気角 [rad]
            'stator_current': [0.0, 0.0, 0.0],  # 固定子電流（3相）[A]
            'rotor_current': [0.0, 0.0, 0.0],   # 回転子電流（3相）[A]
            'output_torque': 0.0,               # 出力トルク [Nm]
            'slip': 0.0,                        # すべり [-]
            'efficiency': 0.0,                  # 効率 [%]
            'power_factor': 0.0,                # 力率 [-]
            'input_power': 0.0,                 # 入力電力 [W]
            'output_power': 0.0,                # 出力電力 [W]
            'temperature': 25.0,                # 温度 [°C]
        }
        
        # パラメータの設定
        self.parameters = {
            'rated_power': self.params.rated_power,
            'rated_voltage': self.params.rated_voltage,
            'rated_frequency': self.params.rated_frequency,
            'rated_speed': self.params.rated_speed,
            'pole_pairs': self.params.pole_pairs,
            'stator_resistance': self.params.stator_resistance,
            'rotor_resistance': self.params.rotor_resistance,
            'stator_leakage_inductance': self.params.stator_leakage_inductance,
            'rotor_leakage_inductance': self.params.rotor_leakage_inductance,
            'mutual_inductance': self.params.mutual_inductance,
            'inertia': self.params.inertia,
            'friction_coefficient': self.params.friction_coefficient,
            'thermal_time_constant': self.params.thermal_time_constant,
            'max_temperature': self.params.max_temperature,
        }
        
        # 内部状態変数
        self._psi_ds = 0.0  # d軸固定子磁束
        self._psi_qs = 0.0  # q軸固定子磁束
        self._psi_dr = 0.0  # d軸回転子磁束
        self._psi_qr = 0.0  # q軸回転子磁束
        self._omega_r = 0.0  # 回転子角速度 [rad/s]
        self._theta_e = 0.0  # 電気角 [rad]
        self._temperature = 25.0  # 温度 [°C]
        self._last_update_time = 0.0
        
        # インダクタンスの計算
        self._ls = self.params.stator_leakage_inductance + self.params.mutual_inductance
        self._lr = self.params.rotor_leakage_inductance + self.params.mutual_inductance
        self._lm = self.params.mutual_inductance
        
    def update(self, t: float, dt: float, sim_data: Any = None) -> None:
        """誘導電動機の状態を更新"""
        # 時間ステップの確認
        if dt <= 0:
            return
            
        # 入力値の取得
        stator_voltage = self.inputs['stator_voltage']
        supply_frequency = self.inputs['supply_frequency']
        load_torque = self.inputs['load_torque']
        inertia = self.inputs['inertia']
        mechanical_damping = self.inputs['mechanical_damping']
        
        # 電気角速度 [rad/s]
        omega_e = 2 * math.pi * supply_frequency
        
        # 3相電圧をdq成分に変換
        v_ds, v_qs = self._abc_to_dq(stator_voltage, self._theta_e)
        
        # 状態方程式の係数
        sigma = 1 - (self._lm**2) / (self._ls * self._lr)
        tau_r = self._lr / self.params.rotor_resistance  # 回転子時定数
        
        # dq軸電流の計算
        i_ds = (self._psi_ds / sigma - self._lm * self._psi_dr / (sigma * self._ls * self._lr)) / self._ls
        i_qs = (self._psi_qs / sigma - self._lm * self._psi_qr / (sigma * self._ls * self._lr)) / self._ls
        i_dr = (self._psi_dr / sigma - self._lm * self._psi_ds / (sigma * self._ls * self._lr)) / self._lr
        i_qr = (self._psi_qr / sigma - self._lm * self._psi_qs / (sigma * self._ls * self._lr)) / self._lr
        
        # 磁束の微分方程式
        d_psi_ds = v_ds - self.params.stator_resistance * i_ds + omega_e * self._psi_qs
        d_psi_qs = v_qs - self.params.stator_resistance * i_qs - omega_e * self._psi_ds
        d_psi_dr = -self.params.rotor_resistance * i_dr + (omega_e - self._omega_r) * self._psi_qr
        d_psi_qr = -self.params.rotor_resistance * i_qr - (omega_e - self._omega_r) * self._psi_dr
        
        # 磁束の更新（オイラー法）
        self._psi_ds += d_psi_ds * dt
        self._psi_qs += d_psi_qs * dt
        self._psi_dr += d_psi_dr * dt
        self._psi_qr += d_psi_qr * dt
        
        # 電気トルクの計算
        te = 1.5 * self.params.pole_pairs * (self._psi_ds * i_qs - self._psi_qs * i_ds)
        
        # 機械系の運動方程式
        acceleration = (te - load_torque - mechanical_damping * self._omega_r) / inertia
        self._omega_r += acceleration * dt
        
        # 電気角の更新
        self._theta_e += self._omega_r * dt
        self._theta_e %= 2 * math.pi
        
        # 出力値の計算
        rotor_speed = self._omega_r * 60 / (2 * math.pi)  # [rad/s] to [rpm]
        slip = (omega_e - self._omega_r) / omega_e if omega_e != 0 else 0
        
        # dq電流を3相電流に変換
        i_abc = self._dq_to_abc(i_ds, i_qs, self._theta_e)
        
        # 電力計算
        input_power = 1.5 * (v_ds * i_ds + v_qs * i_qs)
        output_power = te * self._omega_r
        efficiency = output_power / input_power * 100 if input_power > 0 else 0
        
        # 力率計算
        apparent_power = math.sqrt(v_ds**2 + v_qs**2) * math.sqrt(i_ds**2 + i_qs**2) * 1.5
        power_factor = input_power / apparent_power if apparent_power > 0 else 0
        
        # 温度上昇の計算（簡易モデル）
        power_loss = input_power - output_power
        temp_rise = power_loss * dt / (self.params.thermal_time_constant * 1000)  # 簡易的な熱モデル
        self._temperature += temp_rise
        self._temperature = min(self._temperature, self.params.max_temperature)
        
        # 出力の更新
        self.outputs.update({
            'rotor_speed': rotor_speed,
            'electrical_angle': self._theta_e,
            'stator_current': i_abc,
            'rotor_current': [i_dr, i_qr, 0],  # 簡易的に表示
            'output_torque': te,
            'slip': slip,
            'efficiency': efficiency,
            'power_factor': power_factor,
            'input_power': input_power,
            'output_power': output_power,
            'temperature': self._temperature,
        })
        
        self._last_update_time = t
    
    def _abc_to_dq(self, abc: List[float], theta: float) -> tuple:
        """3相abcをdq成分に変換"""
        a, b, c = abc
        alpha = 2/3 * (a - 0.5*b - 0.5*c)
        beta = 2/3 * (math.sqrt(3)/2 * b - math.sqrt(3)/2 * c)
        
        d = alpha * math.cos(theta) + beta * math.sin(theta)
        q = -alpha * math.sin(theta) + beta * math.cos(theta)
        
        return d, q
    
    def _dq_to_abc(self, d: float, q: float, theta: float) -> List[float]:
        """dq成分を3相abcに変換"""
        alpha = d * math.cos(theta) - q * math.sin(theta)
        beta = d * math.sin(theta) + q * math.cos(theta)
        
        a = alpha
        b = -0.5 * alpha + math.sqrt(3)/2 * beta
        c = -0.5 * alpha - math.sqrt(3)/2 * beta
        
        return [a, b, c]
    
    def reset(self):
        """状態をリセット"""
        self._psi_ds = 0.0
        self._psi_qs = 0.0
        self._psi_dr = 0.0
        self._psi_qr = 0.0
        self._omega_r = 0.0
        self._theta_e = 0.0
        self._temperature = 25.0
        
        # 出力を初期化
        for key in self.outputs:
            if isinstance(self.outputs[key], list):
                self.outputs[key] = [0.0] * len(self.outputs[key])
            else:
                self.outputs[key] = 0.0