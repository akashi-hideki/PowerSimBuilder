# Python シミュレーション設計指示書（完全版・条件付きシナリオ対応・スマホ最適化）

---

## 1. 目的

* 電源、発電機、整流器、溶接機、エンジン、誘導電動機、故障装置の時間シミュレーション
* **各部品は共通インタフェース**を持つ：
  * 初期化メソッド (`__init__`)
  * 更新メソッド (`update`)
  * 入力/出力ポート (`inputs`, `outputs`)
* 出力を次の部品入力へ伝搬する**接続情報**を保持
* シナリオ制御に対応：単発イベントや条件付き時間継続イベント
* VC#でネットリスト変換できる構造

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
        pass  # サブクラスで実装
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
    """
    cond: 条件成立(True/False)
    current_time: 現在時刻
    T_required: 継続時間[s]
    triggered_flag: dict, {"t_start_cond": None or float, "triggered": False}
    """
    if cond:
        if triggered_flag["t_start_cond"] is None:
            triggered_flag["t_start_cond"] = current_time
        elif current_time - triggered_flag["t_start_cond"] >= T_required:
            if not triggered_flag["triggered"]:
                triggered_flag["triggered"] = True
                return True
    else:
        # 条件が外れたらタイマーリセット
        triggered_flag["t_start_cond"] = None
        triggered_flag["triggered"] = False
    return False
```

---

### 3.2 シナリオ定義（条件付きと単発を統一）

```python
scenario = [
    {
        "type": "condition_duration",
        "comp_id": "fault1",
        "condition": lambda comps: comps["generator"].outputs["voltage"] >= 20.0,
        "duration": 4.0,  # 秒
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

* `type`：`condition_duration` または `instant`
* 条件イベントはラムダで現在コンポーネント状態を参照
* アクションもラムダで記述

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

## 7. 補足・スマホ向け工夫

1. 段落を短く、見出しを簡潔化
2. キーワードは太字・コード表記
3. コードブロックは最小化、要点だけ
4. 箇条書き・表で情報整理
5. 長大コードやプロットはリンク参照
6. 条件付きシナリオ対応で表現力アップ

---

✅ **ポイント**

* 条件付き継続時間シナリオと単発イベントを統一
* JSON/辞書形式で柔軟に記述可能
* VC#ネットリスト変換でも条件情報を保持可能
* スマホでも見やすいよう整理済み

---

これで**条件付き時間継続シナリオを含めた完全版Pythonシミュレーション設計書**となります。

---

必要であれば、次のステップとして **VC#ネットリスト変換クラス設計書（条件付きシナリオ対応版）** を作成してMDにまとめることもできます。

作りますか？
