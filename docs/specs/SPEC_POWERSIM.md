# Pythonシミュレーション設計指示書（完全版）

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

### 2.1 SimulationData（全パラメータ集約）

- 電源・発電機：電圧、電流、周波数、波形
- 整流回路：ダイオード降下、整流方式、リップル、出力電圧
- 溶接機：設定電流、実電流、制御モード、過熱フラグ
- 故障：短絡、開放、突入電流
- 誘導電動機：定格出力、トルク、回転数、すべり
- 全体制御：時間刻み、ログ

### 2.2 Component基底クラス

- ID、タイプ、パラメータ辞書
- inputs, outputs 辞書
- update(t, dt, sim_data) メソッド

```python
class Component:
    def __init__(self, comp_id, comp_type):
        self.id = comp_id
        self.type = comp_type
        self.parameters = {}
        self.inputs = {}
        self.outputs = {}

    def update(self, t, dt, sim_data):
        pass
```


## 3. コンポーネント詳細


| クラス         | パラメータ                                   | 入力                        | 出力                                           | 補足                             |
| -------------- | -------------------------------------------- | --------------------------- | ---------------------------------------------- | -------------------------------- |
| PowerSource    | voltage, frequency, waveform                 | -                           | voltage, current                               | AC出力                           |
| Generator      | rated\_power, efficiency                     | torque                      | voltage, current                               | 発電機駆動                       |
| DiodeBridge    | diode\_drop, type                            | ac\_voltage                 | dc\_voltage, ripple                            | 半波/全波/三相                   |
| Welder         | welding\_current\_set, mode, overheat\_limit | dc\_voltage, short\_circuit | arc\_current, overheat                         | 定電流/定電圧制御                |
| Engine         | power, torque\_max                           | -                           | torque                                         | 発電機駆動トルク                 |
| InductionMotor | rated\_power, load\_torque                   | voltage                     | torque, speed, slip                            | 応答遅れ・負荷トルク             |
| FaultDevice    | fault\_type, condition\_expr, duration       | -                           | short\_circuit, open\_circuit, inrush\_current | update内でシナリオ判定・状態管理 |

---

## 4. Connections 原理

* 出力→入力の接続を辞書化・JSON化
* MAIN ループで update 後に伝搬
* FaultDevice も同様に接続反映

### 4.1 Mermaid 図例

```mermaid
flowchart TD
    A[Engine] -->|outputs.torque| B[Generator.inputs.torque]
    B -->|outputs.voltage| C[DiodeBridge.inputs.ac_voltage]
    C -->|outputs.dc_voltage| D[Welder.inputs.dc_voltage]
    E[FaultDevice] -->|outputs.short_circuit| D[Welder.inputs.short_circuit]
    D -->|outputs.arc_current| F[SimulationData]
```

### 4.2 JSON形式例

```json
[
  {"from":"engine.outputs.torque","to":"generator.inputs.torque"},
  {"from":"generator.outputs.voltage","to":"bridge.inputs.ac_voltage"},
  {"from":"bridge.outputs.dc_voltage","to":"welder.inputs.dc_voltage"},
  {"from":"fault1.outputs.short_circuit","to":"welder.inputs.short_circuit"}
]
```

---

## 5. FaultDevice 内部シナリオ管理

* condition\_expr と duration でトリガー判定
* scenario\_state を内部保持
* MAIN ループから状態管理不要

```python
class FaultDevice(Component):
    def __init__(self, comp_id, comp_type):
        super().__init__(comp_id, comp_type)
        self.parameters = {"fault_type":"none","condition_expr":"False","duration":0.0}
        self.outputs = {"short_circuit":False,"open_circuit":False,"inrush_current":0.0}
        self._scenario_state = {"t_start_cond":None,"triggered":False}

    def update(self, t, dt, sim_data):
        cond_result = eval(self.parameters.get("condition_expr","False"))
        if cond_result:
            if self._scenario_state["t_start_cond"] is None:
                self._scenario_state["t_start_cond"] = t
            elif t - self._scenario_state["t_start_cond"] >= self.parameters.get("duration",0):
                if not self._scenario_state["triggered"]:
                    self.outputs["short_circuit"] = True
                    self.outputs["inrush_current"] = self.parameters.get("inrush_current",0.0)
                    self._scenario_state["triggered"] = True
        else:
            self._scenario_state["t_start_cond"] = None
            self._scenario_state["triggered"] = False
```

---

## 6. MAINループ設計

* SimulationData 生成
* Components 登録
* Connections 伝搬
* update → 接続 → ログ収集

```python
def main():
    import numpy as np
    dt = 0.001
    t_end = 5.0
    sim_data = SimulationData()
    sim_data.time_step = dt

    components = {
        "power": PowerSource("power","PowerSource"),
        "generator": Generator("generator","Generator"),
        "bridge": DiodeBridge("bridge","DiodeBridge"),
        "welder": Welder("welder","Welder"),
        "engine": Engine("engine","Engine"),
        "motor": InductionMotor("motor","InductionMotor"),
        "fault1": FaultDevice("fault1","FaultDevice")
    }

    connections = [
        {"from":"engine.outputs.torque","to":"generator.inputs.torque"},
        {"from":"generator.outputs.voltage","to":"bridge.inputs.ac_voltage"},
        {"from":"bridge.outputs.dc_voltage","to":"welder.inputs.dc_voltage"},
        {"from":"fault1.outputs.short_circuit","to":"welder.inputs.short_circuit"}
    ]

    for t in np.arange(0, t_end, dt):
        for comp in components.values():
            comp.update(t, dt, sim_data)

        for conn in connections:
            f_c,f_p = conn["from"].split(".")
            t_c,t_p = conn["to"].split(".")
            components[t_c].inputs[t_p] = components[f_c].outputs[f_p]

        sim_data.logs.append({
            "time": t,
            "components": {cid: {"inputs": comp.inputs.copy(),
                                 "outputs": comp.outputs.copy(),
                                 "parameters": comp.parameters.copy()}
                           for cid, comp in components.items()}
        })
```

---

## 7. VC# ネットリスト変換対応

* Components / Connections を JSON で保持
* FaultDevice の condition\_expr も文字列で保持
* 自動生成ツールで scenario\_expr 設定
* SPICE形式に変換可能


```

```
