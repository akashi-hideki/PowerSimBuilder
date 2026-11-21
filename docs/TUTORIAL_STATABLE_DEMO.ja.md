# チュートリアル: StaTable XML から PSB でデモを作る

> **対象読者**: PowerSimBuilder (PSB) を初めて使う方
> **所要時間**: 約 20 分
> **前提**: PSB v1.10.1 以降、StaTable XML (`cooking_heater_controller.xml` など)
> **参考デモ**: `examples/demos/oven_control_statable.psbproj`

---

## 1. 概要

> **はじめての方へ**: まずは既存デモ `examples/demos/oven_control_statable.psbproj`
> を開いて **Create Simulation → Run** を試してください。Gating 方式で完成済みの動作を確認できます。
> その後、このチュートリアルで **Role 関数方式** を実装すると、実機制御に近い構造を学べます。



PowerSimBuilder は StaTable で設計した 7 層状態機械 (XML) を読み込み、**物理モデル** と組み合わせて Python シミュレーションを実行します。

```
StaTable (設計)
   → cooking_heater_controller.xml
   → PSB (ControllerBox 配置 + 物理モデル配線 + role 関数実装)
   → demo_main.py 生成
   → シミュレーション実行
   → simulation_signals.png (温度制御波形)
```

このチュートリアルでは **MwOven 層** (オーブン PID 加熱) を使い、**ControllerBox の role 関数で加熱制御** を行うデモを作ります。

### 制御アーキテクチャ（重要）

```
[PID Controller]                    [Controller Box]              [OvenChamber]
  control (0-1) ──────────────────> heater_command
                                      │
                                      ▼ F_SetTopHeaterDuty
                                    heater_top_duty (0-100) ────> heater_top_duty
                                                                       │
                                      oven_current_temp <───────────── oven_current_temp
                                      measured <────────────────────── oven_current_temp
```

- **PID 出力 (0-1)** は ControllerBox の `heater_command` 入力へ
- **Role 関数 `F_SetTopHeaterDuty`** が 0-1 を 0-100 にスケーリング
- **Role 関数 `F_CheckOvenOverheat`** が安全停止
- **Role 関数 `F_ReadThermistors`** が温度センサー値を計算
- 状態機械の gating パラメータは**使用しない** (制御は role 関数で完結)

### 完成時の性能

- `oven_current_temp` が 25°C から 200°C まで上昇
- PID 制御で 200°C 付近に整定 (ss ≈ 200.68°C, ovs ≈ 1.45%, settle ≈ 64.6s)
- 定量検証コマンドで数値確認可能

---

## 2. 前提条件

### 2.1 必要なファイル

| ファイル | 場所 | 用途 |
|---|---|---|
| `cooking_heater_controller.xml` | `tests\\data\\scenarios\\cooking_heater\\` | StaTable XML |
| `PowerSimBuilder.exe` | `bin\\Debug\\net10.0-windows\\` | PSB 本体 |

### 2.2 PSB の起動

```powershell
cd C:\\dev\\PowerSimBuilder
.\\bin\\Debug\\net10.0-windows\\PowerSimBuilder.exe
```

---

## 3. ステップバイステップ

### Step 1: 新規プロジェクト

**Ctrl+N** を押す、または **File → New Project** を選択。

**期待**: Canvas が完全に空になる。

### Step 2: StaTable XML のインポート

1. ツールバーの **「StaTable Import」** ボタンをクリック
2. **「XML File」** 欄の **[Browse...]** で `cooking_heater_controller.xml` を選択
3. プレビューで XML の構造を確認 (7 層の状態機械)
4. **[OK]** をクリック

**期待**: Canvas 上に **`Controller Box`** コンポーネントが配置される。

> **v1.10.2 Port Selection**: XML インポートで **Generate** を押すと、
> **Port Selection Dialog** が表示されます。`globals` 変数から入出力ポートを
> チェックボックスで選択できます（Auto-suggest により `thermistor_*` → Input、
> `*_duty` → Output 等が初期選択されます）。
>
> **旧挙動 (v1.10.1 以前)**: ポートは StaTable XML から自動導出のみ。role 関数が
> 空スタブの場合にポート数が不足する問題 (Trap #64) がありました。
> 詳細は `docs/SPEC_PORT_SELECTION.md` を参照。

### Step 3: 物理モデルの配置

パレット (左側) から以下の 2 つを Canvas にドラッグ&ドロップ:

| コンポーネント | カテゴリ | 用途 |
|---|---|---|
| **PID Controller** | Control | 温度 PID 制御 |
| **OvenChamber** | Thermal | オーブン熱モデル |

**期待**: Canvas に 3 つのコンポーネントが並ぶ。

### Step 4: 配線 (4 本)

ポートをクリックして接続線を引きます。**方向に注意** (出力 → 入力):

| # | 接続元 (出力) | 接続先 (入力) | 意味 |
|:---:|---|---|---|
| 1 | `PID Controller.control` | `Controller Box.heater_command` | PID 出力 (0-1) → ControllerBox |
| 2 | `Controller Box.heater_top_duty` | `OvenChamber.heater_top_duty` | 加熱指令 (0-100) → オーブン |
| 3 | `OvenChamber.oven_current_temp` | `Controller Box.oven_current_temp` | 現在温度 → ControllerBox |
| 4 | `OvenChamber.oven_current_temp` | `PID Controller.measured` | 現在温度 → PID フィードバック |

**期待**: Canvas 上に 4 本の接続線が描画される。

### Step 5: Controller Box のパラメータ設定

`Controller Box` を**ダブルクリック** → パラメータ編集画面。

| パラメータ | 値 | 意味 |
|---|---|---|
| `generated_path` | `generated/CookingHeaterController_powersim.py` | 生成済み ControllerBox のパス (自動設定) |
| `auto_events` | `demo_oven` | デモ用の自動イベント駆動 |

> **重要**: **`gate_layer` / `gate_states` / `gate_input_port` / `gate_output_port` は設定しないでください** (空のまま)。制御は Step 6 の role 関数で行います。

### Step 5.5: Role 関数の実装（核心）

ControllerBox の role 関数を実装します。これが今回のデモの制御ロジックの中核です。

#### 5.5.1 Role 関数とは

ControllerBox は StaTable XML から 60 個以上の role 関数 (`F_xxx`) を持ちます。各関数は `# STABLE_USER_CODE_START` と `# STABLE_USER_CODE_END` の間を**ユーザーが編集**でき、シミュレーション実行時に呼ばれます。

> **重要**: 状態機械の `do` アクションで role 関数が呼ばれる設定になっていない場合、**Python エディタで本体を実装するだけでは呼ばれません**。呼び出しの有効化は 5.5.5 を参照。

#### 5.5.2 Role 関数の編集方法 (GUI)

1. `Controller Box` を**ダブルクリック** → Python エディタが開く
2. 左ペイン **File Structure** で `📦 CookingHeaterController` → `MwOven` を展開
3. `F_SetTopHeaterDuty` をクリック → 右ペインに本体表示
4. `# STABLE_USER_CODE_START` と `# STABLE_USER_CODE_END` の間を編集
5. **Ctrl+S** で保存

#### 5.5.3 実装する 4 つの関数

| 関数 | 役割 | 呼び出しタイミング |
|---|---|---|
| `F_ReadThermistors` | `oven_current_temp` から `thermistor_top/middle/bottom` を計算 | 毎ティック |
| `F_StartOvenPid` | PID 制御開始フラグ `heater_top_on` をセット | `OvIdle → PidHeating` 遷移時 |
| `F_StopOvenPid` | 制御停止、duty=0 | `PidHeating → OvIdle` 遷移時 |
| `F_SetTopHeaterDuty` | `heater_command` (0-1) → `heater_top_duty` (0-100) | 毎ティック (PidHeating 中) |
| `F_CheckOvenOverheat` | 250°C 超過で緊急停止 | 毎ティック |

#### 5.5.4 実装例

**F_ReadThermistors**:

```python
def F_ReadThermistors(self):
    # STABLE_USER_CODE_START
    temp = self.inputs.get("oven_current_temp", 0) or 0
    self.globals["thermistor_top"] = temp + 5
    self.globals["thermistor_middle"] = temp
    self.globals["thermistor_bottom"] = temp - 5
    return 0
    # STABLE_USER_CODE_END
```

**F_StartOvenPid**:

```python
def F_StartOvenPid(self):
    # STABLE_USER_CODE_START
    self.globals["heater_top_on"] = 1
    return 0
    # STABLE_USER_CODE_END
```

**F_StopOvenPid**:

```python
def F_StopOvenPid(self):
    # STABLE_USER_CODE_START
    self.globals["heater_top_on"] = 0
    self.globals["heater_top_duty"] = 0
    return 0
    # STABLE_USER_CODE_END
```

**F_SetTopHeaterDuty** (核心):

```python
def F_SetTopHeaterDuty(self):
    # STABLE_USER_CODE_START
    if not self.globals.get("heater_top_on", 0):
        self.globals["heater_top_duty"] = 0
        return 0
    cmd = float(self.inputs.get("heater_command", 0.0))  # 0.0 - 1.0
    duty = int(max(0, min(100, cmd * 100)))               # 0 - 100
    self.globals["heater_top_duty"] = duty
    return 0
    # STABLE_USER_CODE_END
```

**F_CheckOvenOverheat**:

```python
def F_CheckOvenOverheat(self):
    # STABLE_USER_CODE_START
    temp = self.globals.get("thermistor_top", 25)
    if temp > 250:
        self.globals["heater_top_on"] = 0
        self.globals["heater_top_duty"] = 0
        self.globals["error_code"] = 1
    return 0
    # STABLE_USER_CODE_END
```

#### 5.5.5 Role 関数の呼び出し有効化（重要）

StaTable XML の `PidHeating` 状態に `do` アクションが空の場合、role 関数は自動で呼ばれません。以下のいずれかの方法で有効化します:

| 方法 | 手順 | 難易度 |
|---|---|---|
| **(a) XML 編集** | StaTable GUI で `PidHeating` の `do` に `F_SetTopHeaterDuty` を追加 → XML 再出力 → PSB 再インポート → ControllerBox 再生成 | 中 |
| **(a') ポート再選択** (v1.10.2+) | キャンバス上の ControllerBox を **右クリック →「Select Ports...」** でポートを追加・削除（XML 編集・再インポート不要） | 易 |
| **(b) 生成 Python 編集** | `generated/CookingHeaterController_powersim.py` の main loop に `self.F_SetTopHeaterDuty()` 等を追加 | 中 |
| **(c) Gating 使用** | Role 関数を使わず、`gate_layer` / `gate_states` で制御 | 易 |

**推奨**: (a) または (b)。

**(b) の具体例** — `generated/CookingHeaterController_powersim.py` の `update()` 内、`tick()` の直後に追加:

```python
def update(self, t, dt, sim_data=None):
    # ... existing code ...
    self._runtime.tick(t, dt, sim_data)

    # Role function dispatch (explicit, since XML do action is empty)
    self.F_ReadThermistors()
    if self.globals.get("heater_top_on", 0):
        self.F_SetTopHeaterDuty()
    self.F_CheckOvenOverheat()
```

#### 5.5.6 代替: CLI 一括編集 (`tools/edit_role_function.py`)

GUI を使わず CLI で一括編集する場合:

```powershell
python tools\\edit_role_function.py generated/CookingHeaterController_powersim.py MwOven F_ReadThermistors --body "temp = self.inputs.get('oven_current_temp', 0) or 0; self.globals['thermistor_top'] = temp + 5; self.globals['thermistor_middle'] = temp; self.globals['thermistor_bottom'] = temp - 5; return 0"
```

### Step 6: PID Controller のパラメータ設定

`PID Controller` をダブルクリック:

| パラメータ | 値 | 意味 |
|---|---|---|
| `kp` | `0.01` | 比例ゲイン |
| `ki` | `0.0001` | 積分ゲイン |
| `kd` | `0.0` | 微分ゲイン |
| `out_min` | `0.0` | 出力下限 |
| `out_max` | `1.0` | 出力上限 |
| `setpoint` | `200.0` | 目標温度 (°C) |
| `anti_windup` | `False` | アンチワインドアップ |

> **注**: このゲインは既存デモで定量検証済み (ss=200.68, ovs=1.45%, settle=64.6s)。

### Step 7: OvenChamber のパラメータ設定

`OvenChamber` をダブルクリック:

| パラメータ | 値 | 意味 |
|---|---|---|
| `thermal_resistance` | `0.5` | 熱抵抗 (K/W) |
| `thermal_capacity` | `200` | 熱容量 (J/K) |
| `ambient_temperature` | `25` | 周囲温度 (°C) |
| `initial_temperature` | `25` | 初期温度 (°C) |
| `heater_power_max` | `1000` | ヒーター最大出力 (W) |
| `sensor_offset_top` | `5` | 上部サーミスタのオフセット |
| `sensor_offset_bottom` | `-5` | 下部サーミスタのオフセット |

### Step 8: シミュレーション時間の設定 (任意)

ツールバーの `t_end` 入力欄を **`300`** (秒) に設定。

### Step 9: シミュレーション生成

ツールバーの **「Create Simulation」** をクリック。

**期待**:
- `PythonEditorForm` が開く
- `generated/demo_main.py` が生成される
- `generated/CookingHeaterController_powersim.py` に role 関数の編集内容が保持されている

### Step 10: シミュレーション実行

`PythonEditorForm` の **「Run」** ボタンをクリック。

**期待**:
- シミュレーション完了メッセージ
- `simulation_signals.png` が生成される
- 波形グラフに温度制御曲線が表示される

---

## 4. 結果の確認

### 4.1 波形グラフ

`simulation_signals.png` を開いて、以下を確認:

| 信号 | 期待 |
|---|---|
| `oven_current_temp` | 25°C → 200°C に上昇し、200°C 付近で整定 |
| `heater_top_duty` | 0% から始まり、整定後は低い duty (10〜30%) に収束 |

### 4.2 定量的検証

```powershell
python tools\\verify_statable_psbproj.py <your-project.psbproj>
```

**期待される出力**:
```
ss      = 200.68 degC   (定常偏差 -0.68°C)
ovs     = 1.45 %        (オーバーシュート)
settle  = 64.6 s        (整定時間)
```

---

## 5. 保存と再利用

### 5.1 プロジェクトの保存

**Ctrl+S** または **File → Save As...** で `.psbproj` として保存:

```
examples\\demos\\my_oven_demo.psbproj
```

### 5.2 ロード

**File → Open** で `.psbproj` を選択。

> **v1.10.2**: ポート選択結果 (`selected_input_ports` / `selected_output_ports`) も
> `.psbproj` に保存されます。再ロード時に前回の選択が復元されるため、
> 同じプロジェクトを開けば同じポート構成で作業を継続できます。

---

## 6. トラブルシューティング

### 6.1 「Role 関数を編集したのに波形が 0 のまま」

**原因**: role 関数が呼ばれていない (XML の `do` アクションが空)。

**対処**: 5.5.5 の方法 (a) または (b) で呼び出しを有効化。

### 6.2 「波形が直線 (定数)」

**原因**: `plot_config` の `port` 名が実際の出力ポートと一致していない。

**対処**: v1.10.1 以降は自動修正済み。それ以前なら `generated/demo_main.py` の `plot_config` を確認。

### 6.3 「シミュレーション結果が常に 0」

**原因**: role 関数で `heater_top_on` が 1 になっていない、または `heater_command` が 0。

**対処**: `F_StartOvenPid` が呼ばれているか、PID の `setpoint` が正しく設定されているか確認。

### 6.4 「Controller Box に Add Physics が出ない」

**原因**: StaTable インポート由来の ControllerBox には物理モデルを追加できません (仕様)。

**対処**: 物理モデルは別コンポーネント (`OvenChamber` 等) として配置し、配線。

---

## 7. 既存デモとの比較

| 項目 | 本チュートリアル | `oven_control_statable.psbproj` |
|---|---|---|
| 制御方式 | **Role 関数** (`F_SetTopHeaterDuty`) | **Gating** (`gate_layer="MwOven"`) |
| 加熱制御 | ControllerBox 内部で 0-1 → 0-100 変換 | Gating で素通し |
| 安全チェック | `F_CheckOvenOverheat` | Gating 状態で保証 |
| 定量性能 | 同等 (ss=200.68, ovs=1.45%) | 同等 |
| 現実性 | **高い** (実機制御と同じ構造) | 簡易 |

**Role 関数方式は実機制御と同じ構造**なので、ファームウェアに近い制御を検証できます。

---

## 8. 次のステップ

| 拡張 | 方法 |
|---|---|
| **MwSteam 層を使う** | `F_StartBoilerHeater` / `F_CalculateSteamPID` を実装 |
| **MwGrill 層を使う** | `F_StartGrill` / `F_SetGrillDuty` を実装 |
| **PID ゲインの最適化** | `tools/verify_pid.py` で複数ゲイン比較 |
| **Role Snippets を使う** | GUI 右クリック → Register as Snippet で再利用 |
| **`.psblog` と組み合わせ** | Import Signal Log で実機ログと比較 (v1.10.0+) |

---

## 参考

- `examples/demos/oven_control_statable.psbproj` — Gating 方式の完成形
- `examples/demos/README.md` — 全デモ一覧
- `tools/verify_statable_psbproj.py` — 定量検証ツール
- `tools/edit_role_function.py` — role 関数編集 (CLI)
- `docs/SPEC_ROLE_SNIPPETS.md` — Role Snippets 仕様
