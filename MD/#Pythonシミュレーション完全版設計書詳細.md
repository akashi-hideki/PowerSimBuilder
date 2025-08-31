# Pythonシミュレーション完全版設計書

## 1. 目的
- 電源、発電機、整流器、溶接機、エンジン、誘導電動機、故障装置の時間シミュレーション
- 共通インタフェースで部品管理：
  - 初期化メソッド (`__init__`)
  - 更新メソッド (`update`)
  - 入力/出力ポート (`inputs`, `outputs`)
- 出力を次の部品入力へ伝搬
- 条件付きシナリオ制御をコンポーネント内部で管理
- JSON/辞書形式で自動生成・VC#ネットリスト変換対応

---

## 2. 共通データ構造

### SimulationData（全パラメータ集約）
- 電源・発電機：電圧、電流、周波数、波形
- 整流回路：ダイオード降下、整流方式、リップル、出力電圧
- 溶接機：設定電流、実電流、制御モード、過熱フラグ
- 故障：短絡、開放、突入電流
- 誘導電動機：定格出力、トルク、回転数、すべり
- 全体制御：時間刻み、ログ

### Component基底クラス
- ID、タイプ、パラメータ辞書
- inputs, outputs 辞書
- update(t, dt, sim_data) メソッド

---

## 3. コンポーネント詳細

| クラス | パラメータ | 入力 | 出力 | 補足 |
|--------|------------|------|------|------|
| PowerSource | voltage, frequency, waveform | - | voltage, current | AC出力 |
| Generator | rated_power, efficiency | torque | voltage, current | 発電機駆動 |
| DiodeBridge | diode_drop, type | ac_voltage | dc_voltage, ripple | 半波/全波/三相 |
| Welder | welding_current_set, mode, overheat_limit | dc_voltage, short_circuit | arc_current, overheat | 定電流/定電圧制御 |
| Engine | power, torque_max | - | torque | 発電機駆動トルク |
| InductionMotor | rated_power, load_torque | voltage | torque, speed, slip | 応答遅れ・負荷トルク |
| FaultDevice | fault_type, condition_expr, duration | - | short_circuit, open_circuit, inrush_current | update内でシナリオ判定・状態管理 |

---

## 4. Connections 原理

- 出力→入力の接続を辞書化・JSON化
- MAIN ループで update 後に伝搬
- FaultDevice も同様に接続反映

### Mermaid 図例
```mermaid
flowchart TD
    A[Engine] -->|outputs.torque| B[Generator.inputs.torque]
    B -->|outputs.voltage| C[DiodeBridge.inputs.ac_voltage]
    C -->|outputs.dc_voltage| D[Welder.inputs.dc_voltage]
    E[FaultDevice] -->|outputs.short_circuit| D[Welder.inputs.short_circuit]
    D -->|outputs.arc_current| F[SimulationData]
