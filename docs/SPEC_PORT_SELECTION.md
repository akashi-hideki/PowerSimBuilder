# SPEC: ControllerBox Port Selection

> **対象**: PowerSimBuilder v1.10.2 以降
> **目的**: XML ロード時 + ControllerBox 編集時の 2 箇所で、
>          `self.globals` から「ポートに割り当てる変数」をユーザーが選択できるようにする
> **背景**: Trap #64（Stage 1 の全 globals ポート化 or 過少抽出）
> **最終更新**: 2026-11-21

---

## 1. 背景と課題

### 1.1 Trap #64 の現状

`ControllerBoxDescriptorBuilder` の Stage 1 は、生成された `*_powersim.py` 内の
`self.globals[...]` 参照を抽出してポートを導出する。以下の 2 つの問題がある:

| 状況 | 結果 | 問題 |
|---|---|---|
| role 関数が充実（旧 XML） | 20〜22 ポート | **ポート爆発**（UI 上見切れる、未使用ポート混入） |
| role 関数が空スタブ（今回の XML） | **3 ポートのみ** | **ポート不足**（PID デモに必要なポートが欠落） |

### 1.2 あるべき設計

「全部ポート化」でも「自動抽出のみ」でもなく、**ユーザーが globals から選択**する。

- デフォルト: Auto-suggest ヒューリスティックで候補を提示
- ユーザーがチェックボックスで取捨選択
- 選択結果を `.psbproj` に保存（`selected_input_ports` / `selected_output_ports`）

---

## 2. 2 つの統合ポイント

### 2.1 Integration Point 1: `StaTableImportForm`（XML ロード画面）

**フロー**:
```
[Browse XML] → [Parse XML] → [PortSelectorControl 表示] → [OK] → ControllerBox 生成
```

**UX**:
```
┌─ StaTable XML Import ──────────────────────────────────┐
│  XML File: [C:\...\cooking_heater_controller.xml] [Browse...]│
│                                                        │
│  ┌─ Project ─────────────────────────────────────────┐ │
│  │ Name: CookingHeaterController                     │ │
│  │ Layers: 7 (DriverInput / ... / Application)       │ │
│  └───────────────────────────────────────────────────┘ │
│                                                        │
│  ┌─ Input Ports (globals から選択) ──────────────────┐ │
│  │ [Auto-suggest] [Select All] [Clear]               │ │
│  │ ☑ oven_current_temp      degC  (target?)          │ │
│  │ ☑ thermistor_top         degC                     │ │
│  │ ☐ thermistor_bottom      degC                     │ │
│  │ ☑ door_open              -                        │ │
│  │ ☑ heater_command         -                        │ │
│  │ ☐ ir_value               ADC                      │ │
│  │ ...                                               │ │
│  └───────────────────────────────────────────────────┘ │
│                                                        │
│  ┌─ Output Ports (globals から選択) ─────────────────┐ │
│  │ [Auto-suggest] [Select All] [Clear]               │ │
│  │ ☑ heater_top_duty        %                        │ │
│  │ ☑ heater_middle_duty     %                        │ │
│  │ ☑ heater_bottom_duty     %                        │ │
│  │ ☑ fan_duty               %                        │ │
│  │ ☑ error_code             -                        │ │
│  │ ☐ mw_power               W                        │ │
│  │ ...                                               │ │
│  └───────────────────────────────────────────────────┘ │
│                                                        │
│                                  [Cancel]  [OK]        │
└────────────────────────────────────────────────────────┘
```

**挙動**:
- XML パース直後に Stage 1（globals 抽出）を実行
- 抽出した globals を Input/Output 両方の候補として表示
- Auto-suggest で初期チェック状態を設定
- [OK] で `ControllerBoxDescriptorBuilder.Build(... selectedPorts)` を呼び、選択に基づく `ComponentDescriptor` を生成

### 2.2 Integration Point 2: ControllerBox Edit（既存 ControllerBox の編集）

**フロー**:
```
ControllerBox 右クリック → 「Select Ports...」 → PortSelectorControl → [OK] → 再ビルド
```

**UX**: Integration Point 1 と同様だが、以下が異なる:
- 既存の `selected_input_ports` / `selected_output_ports` を初期値に
- 既存ポートに接続線がある場合、「削除警告」を表示

**挙動**:
- 変更後 `_canvasManager.RebuildControllerBoxDescriptor(controllerBox)` を呼ぶ
- 削除されたポートに接続線があれば、その接続線も削除（要確認）
- `.psbproj` の `parameters` を更新

---

## 3. データモデル

### 3.1 `.psbproj` の `parameters`

```json
"Controller Box": {
  "parameters": {
    "generated_path": "generated/CookingHeaterController_powersim.py",
    "auto_events": "demo_oven",
    "gate_layer": "MwOven",
    "gate_states": "PidHeating",
    "gate_input_port": "heater_command",
    "gate_output_port": "heater_top_duty",
    "selected_input_ports":  "oven_current_temp,thermistor_top,door_open,heater_command",
    "selected_output_ports": "heater_top_duty,heater_middle_duty,heater_bottom_duty,fan_duty,error_code",
    "port_selection_version": "1"
  }
}
```

- **CSV 形式**（カンマ区切り）
- **空文字列**: 旧挙動（全 globals をポート化）にフォールバック
- **`port_selection_version`**: 将来のフォーマット変更用

### 3.2 `ControllerBoxDescriptorBuilder` の変更

```csharp
// 旧:
public static ComponentDescriptor Build(string powersimPath) {
    var globals = ParseStage1(powersimPath);
    return new ComponentDescriptor {
        InputPorts  = globals.All,
        OutputPorts = globals.All,
    };
}

// 新:
public static ComponentDescriptor Build(
    string powersimPath,
    IReadOnlyList<string>? selectedInputs = null,
    IReadOnlyList<string>? selectedOutputs = null)
{
    var globals = ParseStage1(powersimPath);

    if (selectedInputs == null && selectedOutputs == null) {
        return BuildDefaultDescriptor(globals);
    }

    return new ComponentDescriptor {
        InputPorts  = (selectedInputs  ?? globals.All).ToArray(),
        OutputPorts = (selectedOutputs ?? globals.All).ToArray(),
    };
}
```

---

## 4. Auto-suggest ヒューリスティック

### 4.1 Input 候補（`_suggestAsInput(name) -> bool`）

| パターン | 例 | 理由 |
|---|---|---|
| `thermistor_*`, `*_sensor` | `thermistor_top`, `steam_sensor` | センサー |
| `*_current_*` | `oven_current_temp`, `mw_anode_current` | 計測値 |
| `door_*` | `door_open` | 外部入力 |
| `*_command`, `*_setpoint` | `heater_command` | 外部指令 |
| `*_target_*` | `oven_target_temp` | 目標値（外部から） |

### 4.2 Output 候補（`_suggestAsOutput(name) -> bool`）

| パターン | 例 | 理由 |
|---|---|---|
| `*_duty` | `heater_top_duty`, `fan_duty` | アクチュエータ出力 |
| `*_on` / `*_off` | `heater_top_on`, `pump_on` | フラグ出力 |
| `*_power` | `mw_power` | 出力電力 |
| `error_code`, `*_fault` | `error_code` | 診断出力 |
| `g_system_tick` | — | **除外**（Timer、ポート化しない） |

### 4.3 両方 unchecked（除外）

| パターン | 例 |
|---|---|
| `g_system_tick`, `*_count` | タイマー・カウンタ |
| `seq_*`, `stage_*` | 内部シーケンス状態 |
| `rx_buf` | UART 内部バッファ |

---

## 5. 実装フェーズ

| Phase | 内容 | 規模 | リリース |
|:---:|---|---:|:---:|
| **P1** | `PortSelectorControl`（UserControl）実装 | M (2h) | v1.10.2 |
| **P2** | `StaTableImportForm` 統合 | S (1h) | v1.10.2 |
| **P3** | ControllerBox 右クリックメニュー + 編集画面統合 | S (1h) | v1.10.2 |
| **P4** | `ControllerBoxDescriptorBuilder.Build` 拡張 | S (30m) | v1.10.2 |
| **P5** | Auto-suggest ヒューリスティック | S (30m) | v1.10.2 |
| **P6** | `.psbproj` 保存/読込対応 | S (30m) | v1.10.2 |
| **P7** | 単体テスト + FlaUI E2E | M (1h) | v1.10.2 |
| **合計** | | **~6.5h** | |

---

## 6. テスト計画

### 6.1 単体テスト（C#）

| # | テスト | 対象 |
|:---:|---|---|
| 1 | `PortSelector_AutoSuggest_PicksCorrectInputs` | ヒューリスティック |
| 2 | `PortSelector_AutoSuggest_PicksCorrectOutputs` | 同上 |
| 3 | `PortSelector_Empty_ReturnsAllGlobals` | 互換性 |
| 4 | `PortSelector_Csv_RoundTrip` | 保存/読込 |
| 5 | `DescriptorBuilder_UsesSelectedPorts` | Build 拡張 |
| 6 | `DescriptorBuilder_FallbackWhenEmpty` | 旧挙動 |

### 6.2 FlaUI E2E

| # | テスト | 対象 |
|:---:|---|---|
| 1 | `StaTableImportForm_ShowsPortSelector` | Integration 1 |
| 2 | `ControllerBox_RightClick_OpensPortSelector` | Integration 2 |
| 3 | `PortSelector_Apply_UpdatesPorts` | 適用 |
| 4 | `PortSelector_Cancel_NoChange` | キャンセル |

---

## 7. リスクと対策

| # | リスク | 対策 |
|:---:|---|---|
| 1 | 既存ポートに接続線がある場合の削除 | 削除警告 + 接続線も自動削除（要確認ダイアログ） |
| 2 | `selected_*_ports` の空文字列 vs 未設定 | 空文字列 = 旧挙動、未設定 = 新規（Auto-suggest 初期値） |
| 3 | `.psbproj` の後方互換性 | `port_selection_version` で判定、未設定は旧挙動 |
| 4 | 既存 `oven_control_statable.psbproj` 等の互換性 | `selected_*_ports` 未設定 → 旧挙動で 22/20 ポート維持 |
| 5 | ポート名の typo | `PortSelectorControl` は globals 抽出結果のみチェック可能 |

---

## 8. 次のアクション

1. Phase 1: `PortSelectorControl` 実装から開始
2. Phase 2: `StaTableImportForm` への統合（優先）
3. Phase 3: ControllerBox 右クリック統合
4. Phase 7: テスト
5. v1.10.2 リリース: ポート選択 + New Project ボタン + `oven_control_simplified.psbproj`

---

## 参考

- `StaTableIntegration/ControllerBoxDescriptorBuilder.cs` — Stage 1 実装
- `parts/StaTableImportForm.cs` — Integration Point 1 のベース
- `CanvasForm/DesignCanvas.cs` — 右クリックメニュー
- Trap #64（THREAD_HANDOFF.md / HANDOFF_MVP1_2.md）
- `docs/SPEC_ROLE_SNIPPETS.md` — Role Snippets 仕様（ストレージパターン参考）
