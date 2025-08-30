# Python シミュレーション設計指示書（通常版・条件付きシナリオ対応）

---

## 1. 目的

* 電源、発電機、整流器、溶接機、エンジン、誘導電動機、故障装置の時間シミュレーション
* 各部品は共通インタフェースを持つ：
  * 初期化メソッド (`__init__`)
  * 更新メソッド (`update`)
  * 入力/出力ポート (`inputs`, `outputs`)
* 出力を次の部品入力へ伝搬する接続情報を保持
* シナリオ制御に対応：単発イベントや条件付き時間継続イベント
* VC#でネットリスト変換可能な構造

---

## 2. データ構造

### 2.1 Component基底クラス

```python
class Component:
    def __init__(self, comp_id, comp_type):
        self.id = comp_id
        self.type = comp_type
        self.parameters = {}
        self.inputs = {}
        self.outputs = {}

    def update(self, t, dt):
        pass
```

---

### 2.2 部品クラス例


| クラス         | 主要パラメータ               | 入力        | 出力                          | 補足                   |
| -------------- | ---------------------------- | ----------- | ----------------------------- | ---------------------- |
| PowerSource    | voltage, frequency, waveform | -           | voltage, current              | AC出力波形生成         |
| Generator      | rated\_power                 | torque      | voltage, current              | トルク入力 → 電圧出力 |
| DiodeBridge    | diode\_drop, type            | ac\_voltage | dc\_voltage, ripple           | 半波/全波/三相対応     |
| Welder         | welding\_current\_set, mode  | dc\_voltage | arc\_current                  | 定電流/定電圧制御      |
| Engine         | power                        | -           | torque                        | 発電機駆動用トルク     |
| InductionMotor | rated\_power                 | voltage     | torque, speed                 | 負荷トルク・回転数計算 |
| FaultDevice    | -                            | -           | short\_circuit, open\_circuit | 故障シナリオ発生       |

---

### 2.3 SimulationData（全体データ管理）

```python
class SimulationData:
    def __init__(self):
        self.voltage = 0.0
        self.current = 0.0
        self.frequency = 50.0
        self.waveform = "sine"
        self.diode_drop = 0.7
        self.rectified_voltage = 0.0
        self.ripple = 0.0
        self.rectifier_type = "full"
        self.welding_current_set = 100.0
        self.welding_current_actual = 0.0
        self.welding_mode = "constant_current"
        self.overheat = False
        self.short_circuit = False
        self.open_circuit = False
        self.inrush_current = 0.0
        self.motor_rated_power = 5.0
        self.motor_torque = 0.0
        self.motor_speed = 0.0
        self.slip = 0.0
        self.time_step = 0.001
        self.logs = []
```

---

## 3. シナリオ設計

### 3.1 条件付き時間継続イベント用メソッド

```python
def check_condition_duration(cond, current_time, T_required, triggered_flag):
    if cond:
        if triggered_flag["t_start_cond"] is None:
            triggered_flag["t_start_cond"] = current_time
        elif current_time - triggered_flag["t_start_cond"] >= T_required:
            if not triggered_flag["triggered"]:
                triggered_flag["triggered"] = True
                return True
    else:
        triggered_flag["t_start_cond"] = None
        triggered_flag["triggered"] = False
    return False
```

---

### 3.2 シナリオ定義

```python
scenario = [
    {
        "type": "condition_duration",
        "comp_id": "fault1",
        "condition": lambda comps: comps["generator"].outputs["voltage"] >= 20.0,
        "duration": 4.0,
        "action": lambda comp: setattr(comp, "short_circuit", True)
    },
    {
        "type": "instant",
        "time": 0.01,
        "comp_id": "welder",
        "param": {"welding_current_set": 50}
    }
]
```

---

## 4. メイン更新ループ（条件付きシナリオ対応）

```python
scenario_state = {i: {"t_start_cond":None, "triggered":False} for i in range(len(scenario))}

for t in time:
    # コンポーネント更新
    for comp in components.values(): comp.update(t, dt)

    # 接続伝搬
    for conn in connections:
        f_c,f_p = conn["from"].split(".")
        t_c,t_p = conn["to"].split(".")
        components[t_c].inputs[t_p] = components[f_c].outputs[f_p]

    # シナリオ判定
    for idx, event in enumerate(scenario):
        if event["type"] == "instant":
            if abs(t-event["time"])<dt/2:
                comp = components[event["comp_id"]]
                for k,v in event["param"].items(): comp.parameters[k] = v
        elif event["type"] == "condition_duration":
            comp = components[event["comp_id"]]
            cond_result = event["condition"](components)
            if check_condition_duration(cond_result, t, event["duration"], scenario_state[idx]):
                event["action"](comp)
```

---

## 5. 接続情報（辞書形式）

```python
connections = [
    {"from":"engine.torque", "to":"generator.torque"},
    {"from":"generator.voltage", "to":"bridge.ac_voltage"},
    {"from":"bridge.dc_voltage", "to":"welder.dc_voltage"}
]
```

* 出力→入力のマッピング
* VC#ネットリスト変換用に容易に利用可能

---

## 6. ログ・出力管理

* `outputs`、`inputs`、`parameters`すべて記録
* matplotlib等で簡易プロット可能

---

## 7. ポイント

* 条件付き継続時間シナリオと単発イベントを統一
* JSON/辞書形式で柔軟に記述可能
* VC#ネットリスト変換でも条件情報を保持可能

---

💡 **違い（スマホ版との比較）**

* スマホ版は段落を短く、見出しを簡略化、箇条書き強調
* 通常版は**コードや表を中心に情報密度を高く**した
* スマホ版は閲覧性優先、通常版は詳細設計向き

---

この指示書を元に、Pythonシミュレーションを完全に実装可能で、条件付きシナリオにも対応しています。
