# UI Test Manual (Multi-Select + Group Save/Load)

> **対象**: PowerSimBuilder v1.9.0 以降
> **目的**: 自動化できない UI 項目の手動チェックリスト
> **最終更新**: 2026-11-03

---

## 自動化済み（参考）

| テスト | 内容 |
|------|--------|
| `DesignCanvasMultiSelectTests` (6) | SelectedComponents API / SelectComponent / ClearSelection |
| `DesignCanvasCtrlClickTests` (2) | additive トグル / 既選択の保持 |
| `GroupManagerTests` (4) | Extract / Save / Load / 相対座標 |
| `GroupManagerLoadTests` (2) | LoadAndAppend / 重複名 _2 付与 |
| `GroupMultiSelectE2ETests` (1) | 複数選択 → Save → Load → 接続再マッピング |

---

## 手動チェックリスト

### 準備

```powershell
dotnet run --project PowerSimBuilder.csproj
```

1. Open → `examples\demos\oven_control_statable.psbproj`
2. 3 コンポーネント（Controller Box / PID Controller / OvenChamber）が表示されることを確認

---

### M1: Save Group ダイアログ

| # | 操作 | 期待 | 結果 |
|----|------|------|------|
| M1-1 | コンポーネント A をクリック | 赤枠で選択される | [ ] |
| M1-2 | **Ctrl+クリック** で B | A + B が赤枠 | [ ] |
| M1-3 | Ctrl+クリック で C | A + B + C が赤枠 | [ ] |
| M1-4 | ツールバー「Save Group...」クリック | SaveFileDialog が開く | [ ] |
| M1-5 | `test_group.psbgroup` で保存 | 完了メッセージ表示 | [ ] |
| M1-6 | ファイルが生成される | サイズ > 0 | [ ] |

### M2: Load Group ダイアログ

| # | 操作 | 期待 | 結果 |
|----|------|------|------|
| M2-1 | メニュー「Edit → Clear Canvas」or 全削除 | キャンバスが空 | [ ] |
| M2-2 | ツールバー「Load Group...」クリック | OpenFileDialog が開く | [ ] |
| M2-3 | `test_group.psbgroup` を選択 | 3 コンポーネントが (50,50) オフセットで追加 | [ ] |
| M2-4 | 接続線も復元される | 4 本の接続線 | [ ] |
| M2-5 | 完了メッセージ | 「X components, Y connections」表示 | [ ] |

### M3: 一括移動の視覚確認

| # | 操作 | 期待 | 結果 |
|----|------|------|------|
| M3-1 | 3 コンポーネントを複数選択 | 全て赤枠 | [ ] |
| M3-2 | いずれかを 50px ドラッグ | **3 つとも 50px 移動** | [ ] |
| M3-3 | 相対位置が崩れていないか | 元の配置が保たれる | [ ] |

### M4: 選択の視覚表示

| # | 操作 | 期待 | 結果 |
|----|------|------|------|
| M4-1 | 単一クリック | 対象のみ赤枠 | [ ] |
| M4-2 | Ctrl+クリック で追加 | 追加分も赤枠 | [ ] |
| M4-3 | Ctrl+クリック で解除 | 解除分の赤枠が消える | [ ] |
| M4-4 | 空キャンバスクリック | 全選択解除 | [ ] |

### M5: 多言語文言

| # | 操作 | 期待 | 結果 |
|----|------|------|------|
| M5-1 | 言語「English」 | 「Save Group...」/「Load Group...」 | [ ] |
| M5-2 | 言語「中文」 | 「保存组...」/「加载组...」 | [ ] |
| M5-3 | 選択なしで Save Group | 「Select one or more components first.」/「请先选择一个或多个组件。」 | [ ] |

### M6: シミュレーション実行（統合確認）

| # | 操作 | 期待 | 結果 |
|----|------|------|------|
| M6-1 | M2 でロードした状態で Create Simulation | `demo_main.py` 生成 | [ ] |
| M6-2 | Run ボタン | シミュレーション成功 | [ ] |
| M6-3 | 波形確認 | 温度が 200°C 付近で整定 | [ ] |

---

## 既知の制限

- Save/Open ダイアログは OS ネイティブのため FlaUI 自動化困難
- ドラッグ操作は FlaUI のマウスエミュレーション不安定のため手動推奨
- 多言語の切替はアプリ再起動が必要（既存仕様）

## 関連テストファイル

| ファイル | 内容 |
|------|--------|
| `PowerSimBuilder.Tests/Gui/DesignCanvasMultiSelectTests.cs` | 複数選択 API |
| `PowerSimBuilder.Tests/Gui/DesignCanvasCtrlClickTests.cs` | Ctrl+クリック |
| `PowerSimBuilder.Tests/Manege/GroupManagerTests.cs` | Extract/Save/Load |
| `PowerSimBuilder.Tests/Manege/GroupManagerLoadTests.cs` | LoadAndAppend |
| `PowerSimBuilder.Tests/Manege/GroupMultiSelectE2ETests.cs` | E2E 統合 |


## FMU Catalog / Import (v1.11.0)

These checks cover the Catalog → URL → File flow. Use a test repository or FMU that is approved for development testing.

| ID | Operation | Expected result |
|---|---|---|
| FMU-01 | Open Physics Model Import and select Catalog | Catalog source controls and model list are displayed |
| FMU-02 | Load `github:modelica/fmi-cross-check|2.0|cs|win64` | Only `.fmu` entries (one per Tool/ToolVersion/Model) are listed when the GitHub source is reachable; other file types are not shown |
| FMU-03 | Enter a GitHub API token and click Load again with a `github:` source | The token is used for the GitHub repository API requests and is saved to application settings when **Load** is executed (entering it alone does not save it) |
| FMU-04 | Select a catalog row and click Download | The URL tab is selected and network-access confirmation is shown |
| FMU-05 | Cancel network-access confirmation | The download does not proceed |
| FMU-06 | Approve a valid Co-Simulation FMU download | The FMU is downloaded, parsed, and shown in the File tab |
| FMU-07 | Inspect the variable grid | Only `input` and `output` causality variables are offered |
| FMU-08 | Attempt to import a Model Exchange FMU | The importer rejects the unsupported FMI type |
| FMU-09 | Select ports and click Import | The model is installed under the user physics model directory: `model.yaml` is generated, the original FMU is copied to `source/`, and `README.md` is created only if absent |
| FMU-10 | In the network-access dialog select **Always allow**, then download the same URL again | The confirmation dialog is shown again (Always allow only stores the URL; it does not skip the confirmation) |
| FMU-11 | Open the imported model folder after Import | `model.yaml`, `README.md` and `source/<original-fmu-file>.fmu` exist; `model.yaml` has `source`, `interface` (selected ports, `parameters: {}`), `units: {}` and `timing` (`fixed_step`, `dt_internal: 0.001`); `usage_example.py` is not created |
