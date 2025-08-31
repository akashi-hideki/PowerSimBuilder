import math
from typing import Dict, List, Tuple, Any, Optional
from dataclasses import dataclass
from enum import Enum
from abc import ABC, abstractmethod



class GeneratorType(Enum):
    """発電機の種類"""
    SALIENT_POLE = "salient_pole"  # 突極形
    CYLINDRICAL = "cylindrical"    # 円筒形

class ExcitationType(Enum):
    """励磁方式"""
    PERMANENT_MAGNET = "permanent_magnet"  # 永久磁石
    ELECTROMAGNETIC = "electromagnetic"    # 電磁石

class OperationMode(Enum):
    """動作モード"""
    STANDALONE = "standalone"      # 単独動作（回転数直接入力）
    ENGINE_DRIVEN = "engine_driven" # エンジン駆動（トルク入力）

@dataclass
class GeneratorParameters:
    """発電機パラメータのデータクラス"""
    pole_pairs: int = 2              # 極対数
    rated_voltage: float = 220.0     # 定格電圧 [V]
    rated_frequency: float = 50.0    # 定格周波数 [Hz]
    rated_rpm: float = 1500.0        # 定格回転数 [rpm]
    rated_torque: float = 100.0      # 定格トルク [Nm]
    winding_resistance: float = 0.1  # 巻線抵抗 [Ω]
    winding_inductance: float = 0.01 # 巻線インダクタンス [H]
    rotor_inertia: float = 2.5       # 回転子慣性 [kg·m²]（発電機ロータの重さ）
    damping_coefficient: float = 0.2 # 減衰係数 [Nm/(rad/s)]
    generator_type: GeneratorType = GeneratorType.SALIENT_POLE
    excitation_type: ExcitationType = ExcitationType.PERMANENT_MAGNET
    saturation_factor: float = 1.2   # 磁気飽和係数
    operation_mode: OperationMode = OperationMode.STANDALONE  # デフォルトは単独動作

class ThreePhaseGenerator(Component):
    """
    Componentベースクラスを継承した3相交流発電機のシミュレーションクラス
    エンジンシミュレーション有無にかかわらず動作可能
    """
    
    def __init__(self, comp_id: str, params: GeneratorParameters = None):
        """
        発電機の初期化
        
        Args:
            comp_id: コンポーネントID
            params: 発電機パラメータ（省略時はデフォルト値を使用）
        """
        super().__init__(comp_id, "ThreePhaseGenerator")
        self.params = params if params else GeneratorParameters()
        
        # 入出力ポートの定義（両モード対応）
        self.inputs = {
            'engine_torque': 0.0,                    # エンジンからのトルク入力 [Nm]
            'rotation_speed_input': self.params.rated_rpm,  # 直接回転数入力 [rpm]
            'load_impedance_R': 10.0,                # 負荷抵抗 [Ω]
            'load_impedance_L': 0.02,                # 負荷インダクタンス [H]
            'speed_reference': self.params.rated_rpm # 速度指令値 [rpm]
        }
        
        self.outputs = {
            'phase_voltages': [0.0, 0.0, 0.0],     # 相電圧 [V]
            'line_voltages': [0.0, 0.0, 0.0],      # 線間電圧 [V]
            'phase_currents': [0.0, 0.0, 0.0],     # 相電流 [A]
            'frequency': 0.0,                      # 周波数 [Hz]
            'output_power': 0.0,                   # 出力電力 [W]
            'rotation_speed': 0.0,                 # 回転数 [rpm]
            'electrical_angle': 0.0,               # 電気角 [rad]
            'mechanical_angle': 0.0,               # 機械角 [rad]
            'load_torque': 0.0,                    # 負荷トルク [Nm]
            'torque_ripple': 0.0,                  # トルク変動 [Nm]
            'mechanical_power': 0.0,               # 機械入力動力 [W]
            'acceleration': 0.0,                   # 角加速度 [rad/s²]
            'kinetic_energy': 0.0                  # 運動エネルギー [J]
        }
        
        self.parameters = {
            'pole_pairs': self.params.pole_pairs,
            'rated_voltage': self.params.rated_voltage,
            'rated_frequency': self.params.rated_frequency,
            'rated_rpm': self.params.rated_rpm,
            'rated_torque': self.params.rated_torque,
            'rotor_inertia': self.params.rotor_inertia,
            'damping_coefficient': self.params.damping_coefficient,
            'generator_type': self.params.generator_type.value,
            'excitation_type': self.params.excitation_type.value,
            'saturation_factor': self.params.saturation_factor,
            'operation_mode': self.params.operation_mode.value
        }
        
        # 内部状態変数
        self._field_flux = 1.0                   # 界磁磁束 [Wb]
        self._last_update_time = 0.0             # 前回の更新時間
        self._angular_velocity = 0.0             # 角速度 [rad/s]
        self._torque_ripple_phase = 0.0          # トルク変動の位相
        self._last_angular_velocity = 0.0        # 前回の角速度（加速度計算用）
        
        # 履歴データ
        self.voltage_history = []
        self.current_history = []
        self.time_history = []
        self.torque_history = []
        self.speed_history = []
        
    def update(self, t: float, dt: float, sim_data: Any = None) -> None:
        """
        発電機の状態を更新（Componentインターフェースの実装）
        
        Args:
            t: 現在時間 [秒]
            dt: 時間ステップ [秒]
            sim_data: シミュレーションデータ（任意）
        """
        # 動作モードに応じた回転数計算
        if self.params.operation_mode == OperationMode.ENGINE_DRIVEN:
            # エンジン駆動モード：エンジントルクから回転数を計算
            engine_torque = self.inputs['engine_torque']
            self._calculate_rotation_speed_from_torque(engine_torque, dt)
        else:
            # 単独動作モード：直接回転数入力を使用
            input_rpm = self.inputs['rotation_speed_input']
            self._calculate_rotation_speed_from_input(input_rpm, dt)
        
        # 負荷インピーダンスの取得
        load_impedance = (self.inputs['load_impedance_R'], self.inputs['load_impedance_L'])
        speed_reference = self.inputs['speed_reference']
        
        # 回転数の制限（速度指令値に基づく）
        rotation_speed = min(self.outputs['rotation_speed'], speed_reference * 1.1)
        self.outputs['rotation_speed'] = rotation_speed
        
        # 周波数の計算
        frequency = (self.params.pole_pairs * rotation_speed) / 120
        self.outputs['frequency'] = frequency
        
        # 角度の更新
        mechanical_speed = rotation_speed / 60  # [rps]
        delta_mechanical_angle = 2 * math.pi * mechanical_speed * dt
        mechanical_angle = (self.outputs['mechanical_angle'] + delta_mechanical_angle) % (2 * math.pi)
        self.outputs['mechanical_angle'] = mechanical_angle
        
        # 電気角の計算（極数倍）
        electrical_angle = mechanical_angle * self.params.pole_pairs
        self.outputs['electrical_angle'] = electrical_angle
        
        # 磁気飽和効果の考慮
        saturation_effect = self._calculate_saturation_effect(rotation_speed)
        
        # 誘導起電力の計算
        induced_voltage = self._calculate_induced_voltage(rotation_speed, saturation_effect)
        
        # 3相交流電圧の計算
        phase_voltages = self._calculate_phase_voltages(induced_voltage, electrical_angle)
        self.outputs['phase_voltages'] = phase_voltages
        
        # 線間電圧の計算
        line_voltages = self._calculate_line_voltages(phase_voltages)
        self.outputs['line_voltages'] = line_voltages
        
        # 負荷電流の計算
        phase_currents = self._calculate_load_current(phase_voltages, load_impedance, frequency, electrical_angle)
        self.outputs['phase_currents'] = phase_currents
        
        # 出力電力の計算
        output_power = self._calculate_output_power(phase_voltages, phase_currents)
        self.outputs['output_power'] = output_power
        
        # 負荷トルクとトルク変動の計算
        load_torque, torque_ripple = self._calculate_torque(output_power, rotation_speed, electrical_angle, dt)
        self.outputs['load_torque'] = load_torque
        self.outputs['torque_ripple'] = torque_ripple
        
        # 機械入力動力の計算（エンジン駆動モードのみ）
        if self.params.operation_mode == OperationMode.ENGINE_DRIVEN:
            engine_torque = self.inputs['engine_torque']
            mechanical_power = engine_torque * self._angular_velocity
            self.outputs['mechanical_power'] = mechanical_power
        
        # 角加速度の計算
        acceleration = (self._angular_velocity - self._last_angular_velocity) / dt if dt > 0 else 0
        self.outputs['acceleration'] = acceleration
        self._last_angular_velocity = self._angular_velocity
        
        # 運動エネルギーの計算
        kinetic_energy = 0.5 * self.params.rotor_inertia * self._angular_velocity**2
        self.outputs['kinetic_energy'] = kinetic_energy
        
        # 履歴の保存
        self._save_history(t, phase_voltages, phase_currents, load_torque, torque_ripple, rotation_speed)
    
    def _calculate_rotation_speed_from_torque(self, engine_torque: float, dt: float):
        """エンジントルクから回転数を計算（エンジン駆動モード）"""
        # 現在の角速度
        current_angular_velocity = self._angular_velocity
        
        # 負荷トルク（前回ステップの値を使用）
        load_torque = self.outputs['load_torque']
        
        # 運動方程式: J * dω/dt = T_engine - T_load - D * ω
        acceleration = (engine_torque - load_torque - self.params.damping_coefficient * current_angular_velocity) 
        acceleration /= self.params.rotor_inertia
        
        # 角速度の更新
        new_angular_velocity = current_angular_velocity + acceleration * dt
        self._angular_velocity = max(0, new_angular_velocity)  # 負の回転数は許容しない
        
        # 回転数[rpm]に変換
        rotation_speed = self._angular_velocity * 60 / (2 * math.pi)
        self.outputs['rotation_speed'] = rotation_speed
    
    def _calculate_rotation_speed_from_input(self, input_rpm: float, dt: float):
        """直接回転数入力から回転数を計算（単独動作モード）"""
        # 入力回転数を角速度に変換
        target_angular_velocity = input_rpm * (2 * math.pi) / 60
        
        # 簡易的な過渡応答のモデリング（1次遅れ系）
        time_constant = 0.1  # 応答時定数 [秒]
        alpha = dt / (time_constant + dt)
        
        # 角速度の更新
        self._angular_velocity = alpha * target_angular_velocity + (1 - alpha) * self._angular_velocity
        
        # 回転数[rpm]に変換
        rotation_speed = self._angular_velocity * 60 / (2 * math.pi)
        self.outputs['rotation_speed'] = rotation_speed
        
        # 単独動作モードでは機械入力動力は計算しない
        self.outputs['mechanical_power'] = 0.0
    
    def _calculate_saturation_effect(self, rotation_speed: float) -> float:
        """磁気飽和効果の計算"""
        # 簡単な飽和モデル: 回転数が高いほど飽和が進む
        saturation_factor = 1.0 + (rotation_speed / self.params.rated_rpm - 1.0) * 0.2
        return min(saturation_factor, self.params.saturation_factor)
    
    def _calculate_induced_voltage(self, rotation_speed: float, saturation_effect: float) -> float:
        """誘導起電力の計算"""
        # 基本の誘導起電力
        base_voltage = self.params.rated_voltage
        
        # 回転数に比例した電圧変化
        speed_ratio = rotation_speed / self.params.rated_rpm
        
        # 励磁方式による調整
        if self.params.excitation_type == ExcitationType.PERMANENT_MAGNET:
            # 永久磁石の場合、回転数に比例
            induced_voltage = base_voltage * speed_ratio
        else:
            # 電磁石の場合、より複雑なモデル
            induced_voltage = base_voltage * speed_ratio * saturation_effect
        
        return induced_voltage
    
    def _calculate_phase_voltages(self, induced_voltage: float, electrical_angle: float) -> List[float]:
        """3相電圧の計算"""
        # 基本波の計算
        phase_u = induced_voltage * math.sin(electrical_angle)
        phase_v = induced_voltage * math.sin(electrical_angle - 2 * math.pi / 3)
        phase_w = induced_voltage * math.sin(electrical_angle - 4 * math.pi / 3)
        
        # 高調波成分の追加（発電機種類による）
        if self.params.generator_type == GeneratorType.SALIENT_POLE:
            # 突極形の場合、3次高調波を追加
            harmonic_ratio = 0.05  # 基本波に対する高調波の比率
            phase_u += induced_voltage * harmonic_ratio * math.sin(3 * electrical_angle)
            phase_v += induced_voltage * harmonic_ratio * math.sin(3 * (electrical_angle - 2 * math.pi / 3))
            phase_w += induced_voltage * harmonic_ratio * math.sin(3 * (electrical_angle - 4 * math.pi / 3))
        
        return [phase_u, phase_v, phase_w]
    
    def _calculate_line_voltages(self, phase_voltages: List[float]) -> List[float]:
        """線間電圧の計算"""
        u, v, w = phase_voltages
        return [u - v, v - w, w - u]
    
    def _calculate_load_current(self, phase_voltages: List[float], 
                               load_impedance: Tuple[float, float], 
                               frequency: float, electrical_angle: float) -> List[float]:
        """負荷電流の計算"""
        R, L = load_impedance
        omega = 2 * math.pi * frequency
        Z = complex(R, omega * L)  # インピーダンス
        impedance_magnitude = abs(Z)
        phase_shift = math.atan2(omega * L, R)  # 位相遅れ
        
        # 各相の電流計算
        currents = []
        for i, voltage in enumerate(phase_voltages):
            # 複素インピーダンスを用いた電流計算
            phase_offset = 2 * math.pi * i / 3  # 各相の位相オフセット
            current_angle = electrical_angle - phase_offset - phase_shift
            current_magnitude = abs(voltage) / impedance_magnitude if impedance_magnitude > 0 else 0
            currents.append(current_magnitude * math.sin(current_angle))
        
        return currents
    
    def _calculate_output_power(self, voltages: List[float], currents: List[float]) -> float:
        """出力電力の計算"""
        power = 0.0
        for v, i in zip(voltages, currents):
            power += v * i
        return power
    
    def _calculate_torque(self, output_power: float, rotation_speed: float, 
                         electrical_angle: float, dt: float) -> Tuple[float, float]:
        """負荷トルクとトルク変動の計算"""
        # 角速度 [rad/s]
        angular_velocity = rotation_speed * (2 * math.pi) / 60
        
        if angular_velocity > 0.1:  # 微小速度では除算を避ける
            # 基本負荷トルク = 出力電力 / 角速度
            base_load_torque = output_power / angular_velocity
        else:
            base_load_torque = 0.0
        
        # トルク変動（リップル）の計算
        #  coggingトルク、磁気的不均衡などを模擬
        torque_ripple = self._calculate_torque_ripple(electrical_angle, dt)
        
        # 総負荷トルク
        total_load_torque = base_load_torque + torque_ripple
        
        return total_load_torque, torque_ripple
    
    def _calculate_torque_ripple(self, electrical_angle: float, dt: float) -> float:
        """トルク変動（リップル）の計算"""
        # トルク変動の位相更新
        self._torque_ripple_phase += 2 * math.pi * 6 * dt  # 6Hzの変動成分
        self._torque_ripple_phase %= 2 * math.pi
        
        # さまざまな周波数成分のトルク変動を模擬
        ripple_1 = 0.05 * self.params.rated_torque * math.sin(6 * electrical_angle)  # 6次調波
        ripple_2 = 0.03 * self.params.rated_torque * math.sin(12 * electrical_angle)  # 12次調波
        ripple_3 = 0.02 * self.params.rated_torque * math.sin(self._torque_ripple_phase)  # 低速変動
        
        total_ripple = ripple_1 + ripple_2 + ripple_3
        return total_ripple
    
    def _save_history(self, time: float, voltages: List[float], currents: List[float], 
                     load_torque: float, torque_ripple: float, rotation_speed: float):
        """履歴データの保存"""
        self.time_history.append(time)
        self.voltage_history.append(voltages.copy())
        self.current_history.append(currents.copy())
        self.torque_history.append((load_torque, torque_ripple))
        self.speed_history.append(rotation_speed)
        
        # メモリ管理のため、履歴は最新1000点まで保持
        if len(self.time_history) > 1000:
            self.time_history.pop(0)
            self.voltage_history.pop(0)
            self.current_history.pop(0)
            self.torque_history.pop(0)
            self.speed_history.pop(0)
    
    def set_operation_mode(self, mode: OperationMode):
        """動作モードを設定"""
        self.params.operation_mode = mode
        self.parameters['operation_mode'] = mode.value
    
    def set_rotor_inertia(self, inertia: float):
        """回転子慣性を設定"""
        self.params.rotor_inertia = inertia
        self.parameters['rotor_inertia'] = inertia
    
    def reset(self):
        """発電機状態をリセット"""
        self._field_flux = 1.0
        self._last_update_time = 0.0
        self._angular_velocity = 0.0
        self._torque_ripple_phase = 0.0
        self._last_angular_velocity = 0.0
        self.voltage_history = []
        self.current_history = []
        self.time_history = []
        self.torque_history = []
        self.speed_history = []
        
        # 出力を初期化
        for key in self.outputs:
            if isinstance(self.outputs[key], list):
                self.outputs[key] = [0.0] * len(self.outputs[key])
            else:
                self.outputs[key] = 0.0

# 使用例
if __name__ == "__main__":
    # 単独動作モードでの使用例
    print("単独動作モードでのシミュレーション")
    print("=" * 50)
    
    # 発電機パラメータ設定（ロータ慣性を大きく設定）
    gen_params = GeneratorParameters(
        pole_pairs=4,
        rated_voltage=400.0,
        rated_frequency=60.0,
        rated_rpm=1800.0,
        rated_torque=200.0,
        rotor_inertia=5.0,  # 重いロータ
        generator_type=GeneratorType.CYLINDRICAL,
        excitation_type=ExcitationType.ELECTROMAGNETIC,
        operation_mode=OperationMode.STANDALONE
    )
    
    generator = ThreePhaseGenerator("generator", gen_params)
    
    # 入力の設定
    generator.inputs['rotation_speed_input'] = 1800.0  # 直接回転数を設定
    generator.inputs['load_impedance_R'] = 15.0
    generator.inputs['load_impedance_L'] = 0.03
    
    # シミュレーション実行
    time_step = 0.01
    simulation_time = 1.0
    
    for step in range(int(simulation_time / time_step)):
        current_time = step * time_step
        
        # 発電機更新
        generator.update(current_time, time_step)
        
        # 結果表示（0.1秒ごと）
        if step % 10 == 0:
            print(f"時間: {current_time:.2f}s, 回転数: {generator.outputs['rotation_speed']:.0f}rpm")
            print(f"周波数: {generator.outputs['frequency']:.1f}Hz, 出力: {generator.outputs['output_power']:.1f}W")
            print(f"加速度: {generator.outputs['acceleration']:.2f}rad/s²")
            print(f"運動エネルギー: {generator.outputs['kinetic_energy']:.1f}J")
            print("-" * 30)
    
    # エンジン駆動モードでの使用例
    print("\nエンジン駆動モードでのシミュレーション")
    print("=" * 50)
    
    gen_params.operation_mode = OperationMode.ENGINE_DRIVEN
    generator.set_operation_mode(OperationMode.ENGINE_DRIVEN)
    
    for step in range(int(simulation_time / time_step)):
        current_time = step * time_step
        
        # エンジントルクを設定（シミュレーション用の適当な値）
        generator.inputs['engine_torque'] = 150.0 + 50.0 * math.sin(2 * math.pi * 0.5 * current_time)
        
        # 発電機更新
        generator.update(current_time, time_step)
        
        # 結果表示（0.1秒ごと）
        if step % 10 == 0:
            print(f"時間: {current_time:.2f}s, 回転数: {generator.outputs['rotation_speed']:.0f}rpm")
            print(f"エンジントルク: {generator.inputs['engine_torque']:.1f}Nm")
            print(f"負荷トルク: {generator.outputs['load_torque']:.1f}Nm")
            print(f"機械入力動力: {generator.outputs['mechanical_power']:.1f}W")
            print("-" * 30)