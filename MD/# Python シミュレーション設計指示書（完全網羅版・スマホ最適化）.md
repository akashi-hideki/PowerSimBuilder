# Pythonシミュレーション完全版設計指示書（網羅版）

---

## 1. 目的

* 電源、発電機、整流器、溶接機、エンジン、誘導電動機、故障装置の時間シミュレーション
* 各部品は共通インタフェース：

  * 初期化メソッド (`__init__`)
  * 更新メソッド (`update`)
  * 入力/出力ポート (`inputs`, `outputs`)
* 出力を次の部品入力へ伝搬する接続情報を保持
* 条件付きシナリオ制御：単発イベント・条件付き継続イベント、複数条件・複数出力
* JSON/辞書形式で自動生成可能
* VC#でネットリスト変換可能

---

## 2. 共通データ構造

### 2.1 SimulationData

```python
class SimulationData:
    def __init__(self):
        # 電源・発電機系
        self.voltage = 0.0
        self.current = 0.0
        self.frequency = 50.0
        self.waveform = "sine"
      
        # 整流器
        self.diode_drop = 0.7
        self.rectified_voltage = 0.0
        self.ripple = 0.0
        self.rectifier_type = "full"
      
        # 溶接機
        self.welding_current_set = 100.0
        self.welding_current_actual = 0.0
        self.welding_mode = "constant_current"
        self.overheat = False
      
        # 故障装置
        self.short_circuit = False
        self.open_circuit = False
        self.inrush_current = 0.0
      
        # 誘導電動機
        self.motor_rated_power = 5.0
        self.motor_torque = 0.0
        self.motor_speed = 0.0
        self.slip = 0.0
      
        # 全体制御
        self.time_step = 0.001
        self.logs = []  # 各コンポーネントの出力・入力・パラメータを記録
```

### 2.2 Component基底クラス

```python
class Component:
    def __init__(self, comp_id, comp_type):
        self.id = comp_id
        self.type = comp_type
        self.parameters = {}  # 設定値
        self.inputs = {}      # 他コンポーネントからの入力
        self.outputs = {}     # 出力値

    def update(self, t, dt, sim_data: SimulationData):
        """
        t: 現在時間
        dt: 時間刻み
        sim_data: SimulationDataの参照
        """
        pass
```

---

## 3. コンポーネント詳細（入力・出力・パラメータ）


| クラス         | 主要パラメータ                               | 入力        | 出力                                           | 補足                        |
| -------------- | -------------------------------------------- | ----------- | ---------------------------------------------- | --------------------------- |
| PowerSource    | voltage, frequency, waveform                 | -           | voltage, current                               | AC出力波形生成              |
| Generator      | rated\_power, efficiency                     | torque      | voltage, current                               | 発電機駆動用トルク入力      |
| DiodeBridge    | diode\_drop, type                            | ac\_voltage | dc\_voltage, ripple                            | 半波/全波/三相対応          |
| Welder         | welding\_current\_set, mode, overheat\_limit | dc\_voltage | arc\_current, overheat                         | 定電流/定電圧制御、過熱判定 |
| Engine         | power, torque\_max                           | -           | torque                                         | 発電機駆動用トルク出力      |
| InductionMotor | rated\_power, load\_torque                   | voltage     | torque, speed, slip                            | 応答遅れ・負荷トルク反映    |
| FaultDevice    | fault\_type                                  | -           | short\_circuit, open\_circuit, inrush\_current | シナリオ発生装置            |

---

## 4. 接続情報（辞書形式）

```python
connections = [
    {"from":"engine.torque", "to":"generator.torque"},
    {"from":"generator.voltage", "to":"bridge.ac_voltage"},
    {"from":"bridge.dc_voltage", "to":"welder.dc_voltage"}
]
```

* 出力→入力のマッピング
* VC#ネットリスト変換用に利用

---

## 5. 条件付きシナリオ（自動生成対応）

### 5.1 シナリオデータ例

```python
scenario_data = [
    {
        "id": "event1",
        "type": "condition_duration",
        "comp_id": "fault1",
        "condition_expr": "components['generator'].outputs['voltage'] >= 20.0 and components['welder'].outputs['arc_current'] < 50",
        "duration": 4.0,
        "action": ["setattr(comp, 'short_circuit', True)", "setattr(comp, 'inrush_current', 10.0)"]
    },
    {
        "id": "event2",
        "type": "instant",
        "time": 0.01,
        "comp_id": "welder",
        "param": {"welding_current_set": 50}
    }
]
```

### 5.2 シナリオ状態管理

```python
scenario_state = {event["id"]: {"t_start_cond": None, "triggered": False} for event in scenario_data}
```

### 5.3 判定関数

```python
def check_condition_duration(cond_result, current_time, T_required, state):
    if cond_result:
        if state["t_start_cond"] is None:
            state["t_start_cond"] = current_time
        elif current_time - state["t_start_cond"] >= T_required:
            if not state["triggered"]:
                state["triggered"] = True
                return True
    else:
        state["t_start_cond"] = None
        state["triggered"] = False
    return False
```

---

## 6. メイン更新ループ（完全版）

```python
for t in time_vector:
    # コンポーネント更新
    for comp in components.values():
        comp.update(t, dt, sim_data)

    # 接続伝搬
    for conn in connections:
        f_c,f_p = conn["from"].split(".")
        t_c,t_p = conn["to"].split(".")
        components[t_c].inputs[t_p] = components[f_c].outputs[f_p]

    # シナリオ判定
    for event in scenario_data:
        if event["type"] == "instant":
            if abs(t-event["time"])<dt/2:
                comp = components[event["comp_id"]]
                for k,v in event["param"].items():
                    comp.parameters[k] = v
        elif event["type"] == "condition_duration":
            comp = components[event["comp_id"]]
            cond_result = eval(event["condition_expr"])
            if check_condition_duration(cond_result, t, event["duration"], scenario_state[event["id"]]):
                for act in event["action"]:
                    eval(act)
```

---

## 7. ログ・出力管理

* `sim_data.logs` に以下を記録：

  * 各コンポーネントの `inputs` / `outputs` / `parameters`
  * 時刻 `t`
* matplotlib / pandas / CSV に出力可能
* 自動生成ツールはログ出力対象をシナリオデータに含めることも可能

---

## 8. 全体制御

* 時間刻み `dt` は SimulationData で一元管理
* シナリオ評価は `dt` に依存して更新
* 複数条件・複数出力シナリオにも拡張可能
* すべてのコンポーネントは `update(t, dt, sim_data)` を共通インタフェースとして呼び出す

---

## 9. VC#ネットリスト変換対応

* `components` と `connections` を JSON/辞書形式で受け取り SPICE形式に変換
* 各コンポーネントの `id`, `type`, `parameters` をキーに出力
* 条件付きシナリオも文字列条件として保持 → VC# 側で評価コード生成可能
* 自動生成ツールでシナリオJSONを作れば、ネットリスト変換も共通処理で対応可能

---

## 10. ポイントまとめ

1. コンポーネントは共通インタフェースで管理
2. SimulationData で全体状態を統合
3. 接続情報で出力→入力の流れを管理
4. シナリオは **データと判定を分離**
5. 条件付き

継続イベント・瞬時イベントに対応
6\. ログ収集と可視化、ネットリスト変換に対応
7\. 自動生成ツールはシナリオデータを生成するだけでループ処理は共通利用可能
8\. すべてのパラメータ、接続、シナリオ、ログがJSON/辞書で一元管理可能

---

この設計書で、Pythonシミュレーションの**初期化・更新・条件シナリオ・接続・ログ・VC#ネットリスト変換**まで完全網羅しています。
