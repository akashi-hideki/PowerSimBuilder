# THREAD HANDOFF — PowerSimBuilder 現状スナップショット

> **用途**: 新スレッド開始時にこのファイル 1 つ読めば即座に作業再開できる「現在の状態」を記録する。
> **累積履歴**: `HANDOFF_MVP1_2.md`（バージョン別・罠 #1〜#113）を参照。
> **最終更新**: 2026-11-21（v1.10.1 public 公開 + New Project コミット + ポート選択 SPEC コミット）

---

## 🎉 公開完了（2026-11-21）

**PowerSimBuilder v1.10.1 が GitHub で public 公開されました。**

| 項目 | 値 |
|---|---|
| Repository | `https://github.com/akashi-hideki/PowerSimBuilder`（**PUBLIC**） |
| 最新タグ | `v1.10.1` |
| 最新リリース | `v1.10.1`（Release Notes 付き） |
| CI | ✅ 全 Green（run `44819365326`, success） |
| HEAD | `15786bf`（docs: add port selection spec + update handoff） |
| コミット数 | 540+ |
| タグ数 | 37 |
| テスト | C# 608 + Python 58 + E2E 19 |

### 公開されたコンテンツ

| カテゴリ | 内容 |
|---|---|
| コード | C# (.NET 10) + Python 3.10-3.13 |
| チュートリアル | `docs/TUTORIAL_STATABLE_DEMO.{ja,en,zh}.md`（3 言語） |
| IP ドキュメント | `docs/ip/`（13 ファイル、LAWYER_CONSULTATION は除く） |
| ライセンス | Apache 2.0（商用利用可） |

---

## 本セッション（2026-11-15 〜 11-21）の成果

### v1.10.1 リリース（2026-11-14）

4 つのコードバグを修正:

| # | バグ | 修正 |
|:---:|---|---|
| P2 | `RecordedSignal` の `__init__.py` export 漏れ | `4d6a328` |
| P4 | `plot_config` の port 名が `'output'` ハードコード | `0c6db67` |
| P5 | `oscillating` 分類器なし（三角波検出不可） | `0c6db67` |
| P6 | 巨大 ramp（`g_system_tick`）が自動チェックされ Y 軸支配 | `0c6db67` |

### チュートリアル 3 言語公開（2026-11-15）

| コミット | 内容 |
|---|---|
| `c758c48` | ja/en チュートリアル |
| `0951c57` | zh チュートリアル |

### `code` ファイル誤コミット事故（2026-11-15）

`code code tmp\...` 誤タイプで `code` ファイルが作成・コミットされる。`b353cd6` で削除。

### Public 化（2026-11-21）

- `gh repo edit --visibility public` 実行 → **PUBLIC** 完了
- CI 復活・全 Green 確認
- 2FA 認証（Microsoft Authenticator）も完了

### New Project 機能追加（2026-11-21）

`tmp\patch_new_project.py` 適用後、ビルド・コミット・Push 完了:

| 機能 | 状態 |
|---|---|
| New Project ボタン（ツールバー左端） | ✅ 追加済み・コミット済み |
| `Ctrl+N` ショートカット | ✅ 追加済み・コミット済み |
| 未保存変更確認ダイアログ | ✅ 実装済み |
| i18n 3 キー × 2 言語 | ✅ 追加済み・コミット済み（400 キー en/zh パリティ） |
| **コミット** | ✅ **完了**（`15786bf`） |
| ビルド | ✅ 0 エラー |
| pre-commit | ✅ 全 Passed（mixed-line-ending 自動修正含む） |

### ポート選択 SPEC 策定（2026-11-21）

| 成果物 | 状態 |
|---|---|
| `docs/SPEC_PORT_SELECTION.md` | ✅ 作成・コミット済み（`15786bf`、10,923 bytes） |
| P1-P7 フェーズ計画（~6.5h） | ✅ 策定済み |

---

## 🚧 未解決の課題: ControllerBox ポート選択（Trap #64）

### 問題

`cooking_heater_controller.xml` をロードすると **3 ポートしか出ない**。

**原因**: XML の `do` / `action` が空のため、Stage 1（`self.globals[...]` 抽出）で参照が検出されず、Timer の `g_system_tick` 程度しか抽出されない。

**旧バージョンでは 20〜22 ポート**あったのは、当時の XML の role 関数が充実していたため（または全 globals ポート化の旧実装）。

### 解決策: `PortSelectorControl` を 2 箇所に追加

**詳細設計**: `docs/SPEC_PORT_SELECTION.md` を参照。

| 統合ポイント | ファイル | フロー |
|---|---|---|
| **Point 1** | `parts/StaTableImportForm.cs` | XML ロード → PortSelector → ControllerBox 生成 |
| **Point 2** | ControllerBox 右クリックメニュー | 既存 ControllerBox のポート編集 |

### 実装フェーズ（~6.5h）

| Phase | 内容 | 規模 |
|:---:|---|---:|
| P1 | `PortSelectorControl`（UserControl） | 2h |
| P2 | `StaTableImportForm` 統合 | 1h |
| P3 | ControllerBox 右クリック + 編集画面 | 1h |
| P4 | `ControllerBoxDescriptorBuilder.Build` 拡張 | 30m |
| P5 | Auto-suggest ヒューリスティック | 30m |
| P6 | `.psbproj` 保存/読込 | 30m |
| P7 | 単体テスト + FlaUI E2E | 1h |

### 緊急回避策（ポート選択実装までの暫定）

**`oven_control_statable.psbproj` は既に「StaTable XML から作られた簡略版デモ」**として完成している（22/20 ポート、ss=200.68 / ovs=1.45% / settle=64.6s で検証済み）。

```powershell
Copy-Item examples\demos\oven_control_statable.psbproj examples\demos\oven_control_simplified.psbproj
```

これだけで `oven_control_simplified.psbproj` が完成し、バイナリ作成 → リリース → 告知の流れに進める。

---

## 次タスク候補（優先順）

| # | タスク | 規模 | 期日 |
|:---:|---|---:|:---:|
| 1 | **P1: `PortSelectorControl` 実装**（`docs/SPEC_PORT_SELECTION.md` 参照） | M (2h) | — |
| 2 | P5: Auto-suggest ヒューリスティック | S (30m) | — |
| 3 | P4: `ControllerBoxDescriptorBuilder.Build` 拡張 | S (30m) | — |
| 4 | P2: `StaTableImportForm` 統合 | S (1h) | — |
| 5 | P3: ControllerBox 右クリック統合 | S (1h) | — |
| 6 | P6: `.psbproj` 保存/読込 | S (30m) | — |
| 7 | P7: 単体テスト + FlaUI E2E | M (1h) | — |
| 8 | `oven_control_simplified.psbproj` 作成（Copy-Item で即時 or PortSelector で新規作成） | XS〜S | — |
| 9 | バイナリ作成（`dotnet publish -p:PublishSingleFile=true`） | S | — |
| 10 | v1.10.2 リリース（ポート選択 + New Project + デモ + バイナリ） | S | — |
| 11 | 告知文面作成（WeChat 中英併記） | M | — |

---

## セッション開始時の確認コマンド

```powershell
cd C:\dev\PowerSimBuilder

# 1. Git 状態
git status --short
git log --oneline -5
git tag -l "v1.10*"

# 2. New Project ボタンのコミット状況
Select-String -Path CanvasForm\MainForm.cs -Pattern "newProjectButton" | Select-Object -First 3

# 3. 公開状態
gh repo view akashi-hideki/PowerSimBuilder --json visibility

# 4. CI 状態
gh run list --limit 3

# 5. ビルド健全性
dotnet build 2>&1 | Select-Object -Last 3
```

---

## 罠候補（本セッション追加分）

| # | 罠 | 対策 |
|:---:|---|---|
| 112 | `PSB_PSBLOG_IMPORT` 等のテスト用環境変数が PowerShell セッションに残ると、次回起動時に自動ダイアログと手動ダイアログの 2 重モーダル化 | `PSB_E2E_TEST=1` ガードを v1.10.1 で追加済み |
| 113 | `code code file.py` 誤タイプで `code` ファイルが作成・コミットされる | `gcommit.ps1` ホワイトリスト化（v1.10.2 候補）、または `.gitignore` に `/code` |
| 114 | Stage 1 の `self.globals[...]` 抽出は role 関数が空スタブだと過少（3 ポート等）。Trap #64 の別側面 | `PortSelectorControl` でユーザー選択（本 SPEC で解決予定） |
| 115 | 長い Markdown / Python スクリプトをチャット描画ビューからコピーすると、`*-`→`--` 変換や末尾欠落が起きる | VSCode で**直接 .md として保存**（`code <file>` + 貼り付け + Ctrl+S）。Python スクリプト経由は避ける |
| 115 | パッチ適用後 `dotnet build` を忘れ、旧バイナリで GUI 検証して「変更が反映されない」と誤認（バイナリ `8:17:24` / ソース `8:28:34` のタイムスタンプ乖離で判明、解決に 30 分） | パッチ後は必ず `dotnet build` → `Get-Item .\bin\Debug\net10.0-windows\PowerSimBuilder.exe \| Select LastWriteTime` で時刻確認 → GUI 起動 |
| 116 | `PortSelectorControl.ItemCheck` 内の `BeginInvoke` が handle 未生成時（`ShowDialog` 前の `SetItemChecked`）に `InvalidOperationException` を投げ、`catch` で握り潰されて無音で PythonEditor へ進む | `if (IsHandleCreated)` ガードで解決（`patch_p2a_fix.py`）。catch は `MessageBox.Show` に変更し可視化（文言整理は v1.10.2 で） |
| 117 | SPEC 記載（`parts/StaTableImportForm.cs`）と実体（`StaTableIntegration/StaTableImportForm.cs`）の乖離、および「XML パース直後にポート選択」の記述が実装（Save → 選択）と不整合 | SPEC 修正は v1.10.2 リリース時にまとめて対応 |
| 0 | **P2b: MainForm 統合** — `CanvasForm\MainForm.cs:1695` の `Build(...)` に `SelectedInputPorts` / `SelectedOutputPorts` を渡す | S (30m) | 即時 |
| 123 | `ComponentControl.Parameters` は `Descriptor.Parameters` から初期化されるため、Descriptor に無いキー（`selected_*_ports`）はロード時の `ContainsKey` ガードで無視される | Descriptor=「型」、Instance=「実体」。Instance 独自キーは無条件復元に変更（P6） |
| 124 | `Build` の戻り値 Descriptor に永続化対象パラメータを追加しないと、XML 取り込み直後の Save で消失（右クリック編集時のみ保存される罠） | `Build` で `ParameterDescriptor` として明示追加（P4.2） |
| 125 | Python パッチの `OLD` 文字列でインデント（8 vs 12 スペース）を誤ると `anchor not found` で停止 | インデント非依存の `re` + 波括弧マッチング方式（v2）を標準化 |
---

## 参考ファイル

| ファイル | 用途 |
|---|---|
| `docs/TUTORIAL_STATABLE_DEMO.{ja,en,zh}.md` | 3 言語チュートリアル（公開済み） |
| `docs/SPEC_PORT_SELECTION.md` | ポート選択仕様（本セッション新規、10,923 bytes） |
| `docs/SPEC_ROLE_SNIPPETS.md` | Role Snippets 仕様 |
| `docs/SPEC_PSBLOG_IMPORT.md` | psblog Import 仕様 |
| `HANDOFF_MVP1_2.md` | 累積履歴（罠 #1〜#113） |
| `examples/demos/oven_control_statable.psbproj` | 既存完成デモ（22/20 ポート） |
| `tests/data/scenarios/cooking_heater/cooking_heater_controller.xml` | 対象 XML |

---

## 🎉 P2 完了（2026-11-21）

| Phase | 状態 | コミット |
|---|---|---|
| P1 PortSelectorControl | ✅ | de114d9 |
| P4 Build 拡張 | ✅ | 6a10b76 |
| P2a ダイアログ統合 | ✅ | de114d9 |
| P2a fix（BeginInvoke ガード） | ✅ | add2582 |
| **P2b MainForm 統合** | ✅ | `<新ハッシュ>` |
## 🎉 P2 完了（2026-11-21）

| Phase | 状態 | コミット |
|---|---|---|
| P1 PortSelectorControl | ✅ | de114d9 |
| P4 Build 拡張 | ✅ | 6a10b76 |
| P2a ダイアログ統合 | ✅ | de114d9 |
| P2a fix（BeginInvoke ガード） | ✅ | add2582 |
| **P2b MainForm 統合** | ✅ | `<新ハッシュ>` |
| **P5 Auto-suggest（_current 末尾対応）** | ✅ | `<新ハッシュ>` |
| **P7.1 定量テスト（61 PASS）** | ✅ | `<新ハッシュ>` |

### 定量テスト結果（P7.1）

| カテゴリ | ケース数 | 結果 |
|---|---|---|
| SuggestAsInput（SPEC §4.1） | 17 | ✅ |
| SuggestAsOutput（SPEC §4.2） | 12 | ✅ |
| IsExcluded（SPEC §4.3） | 12 | ✅ |
| 相互排他性 | 6 | ✅ |
| 除外 = Input/Output 両方 false | 5 | ✅ |
| Build の explicit/null/empty/片側null | 6 | ✅ |
| ExtractGlobalsForSelection | 3 | ✅ |
| **合計** | **61** | **61/61** |
## 🎉 P3 完了（2026-11-21）

ControllerBox 右クリック → 「Select Ports...」メニュー実装。既存 ControllerBox のポートを UI から編集可能に。

| Phase | 状態 | コミット |
|---|---|---|
| **P3 ControllerBox 右クリック統合** | ✅ | `<新ハッシュ>` |

### 実装内容

| 機能 | 実装 | 場所 |
|---|---|---|
| 右クリックメニュー「Select Ports...」 | ✅ `Items[3]`（末尾追加、既存 index 0-2 不変） | `DesignCanvas.cs:3507` |
| 表示条件 | ControllerBox 選択時のみ（`Items.Count > 3` ガード） | `DesignCanvas.cs:3669` |
| 既存選択の復元 | `Parameters["selected_input_ports"]` から CSV パース | `ParseCsvParamOrNull` |
| 削除警告 | 削除ポート検出 → `MessageBox` 確認（OK/Cancel） | `DiffList` |
| Descriptor インプレース更新 | `InputPorts`/`OutputPorts` 差し替え | `d.InputPorts = rebuilt.InputPorts` |
| Parameters メモリ更新 | `selected_*_ports` 更新 | `SelectedComponent.Parameters[...]` |

### 既知の制限（v1.10.2 リリースノートに明記）

| # | 制限 | 対応予定 |
|:---:|---|---|
| 1 | 削除ポートへの接続線は**手動削除**（警告ダイアログで明示） | v1.10.3 |
| 2 | `.psbproj` 未永続化（再起動で `selected_*_ports` 消失） | **P6 で解決** |
| 3 | パレット Descriptor 未同期（キャンバスインスタンスのみ更新） | 将来検討 |

## 🎉 P4.2 + P6 + P7.1 完了（2026-11-22）

`.psbproj` の `selected_*_ports` 完全永続化を定量証明。

### 2 つの修正

| # | 変更 | 場所 |
|:---:|---|---|
| **P4.2** | Build の戻り値 `ParameterDescriptor` に `selected_input_ports` / `selected_output_ports` / `port_selection_version` を追加（初回保存対応） | `ControllerBoxDescriptorBuilder.cs:132-163` |
| **P6** | ロード時の `ContainsKey` ガード削除 → 無条件復元（Descriptor に無いキーも復元） | `CanvasManager.cs:381-386, 485-490` |

### 定量検証結果

| 項目 | 結果 |
|---|---|
| ビルド | ✅ 0 エラー |
| 単体テスト | ✅ **64/64 PASS**（既存 61 + 新規 3） |
| `project.json` の `selected_*_ports` | ✅ **保存確認済み** |
| 永続化キー | `generated_path` / `auto_events` / `selected_input_ports` / `selected_output_ports` / `port_selection_version` |

### 検証ログ（実測値）
nstance = CookingHeaterController
all keys: [generated_path, auto_events, selected_input_ports, selected_output_ports, port_selection_version]
selected_input_ports = [door_open,oven_current_temp,oven_target_temp]
selected_output_ports = [heater_top_duty,heater_top_on]
port_selection_version = [1]

### P3 既知制限の解消

| # | 制限 | 状態 |
|:---:|---|---|
| P3-2 | `.psbproj` 未永続化 | ✅ **解消** |

### 次タスク
- **v1.10.2 リリース準備**（バイナリ → タグ → リリースノート → 告知）

## 🎉 Palette Import 連携完了（2026-11-22）
`.psblog` Import で取り込んだ RecordedSignal をパレットに自動登録し、他プロジェクトで再利用可能に。

### 変更内容（パッチ `tmp/patch_palette_import.py` 適用）
| # | 変更 | 場所 |
|:---:|---|---|
| A1 | `PsblogImportResult.AddToPalette` フィールド追加 | `parts/PsblogImportForm.cs` |
| A2 | `_chkAddToPalette` チェックボックス（既定 ON）宣言 | 同上 |
| A3 | `_chkAddToPalette` 初期化ブロック追加 | 同上 |
| A4 | `bottomPanel.Controls.AddRange` に `_chkAddToPalette` 追加 | 同上 |
| A5 | `OnAddToCanvas` で `Result.AddToPalette` をセット | 同上 |
| B1 | `importPsblogButton_Click` にパレット一括登録ブロック挿入 | `CanvasForm/MainForm.cs` |
| C-en | `PsblogImport.AddToPalette` = "Also add to palette" | `Resources/i18n/en.json` |
| C-zh | `PsblogImport.AddToPalette` = "也添加到调色板" | `Resources/i18n/zh.json` |

### 設計
- **Category**: 新規 `"Signal Log"` カテゴリ（既存 `"Source"` とは別）
- **重複 Name 戦略**: `RecordedSignal [var]` → `(2)`, `(3)` ... サフィックス自動付与
- **upsert パターン**: `staTableImportButton_Click` の単純追加版を踏襲（既存チェック → AddComponent → SaveDescriptors）
- **パッチ適用ツール**: `tmp/patch_palette_import.py`（Python 製・冪等・バックアップ付き）

### 検証結果
| 項目 | 結果 |
|---|---|
| パッチ適用 | 8/8 APPLIED |
| ビルド | 0 エラー / 9 警告 |
| 回帰テスト | 599 passed / 2 skipped / 0 failed（ベースライン同等） |
| 手動確認 | パレットに `Signal Log` カテゴリ表示、`RecordedSignal [var]` 追加、再起動後も永続化、新規プロジェクトでドラッグ＆ドロップ可能 |


*最終更新: 2026-11-22 / v1.10.2 リリース（psblog Import のパレット連携追加）*

🎉 物理モデル管理UI統合完了（2026-11-22）
Python エディタ左パネルに、挿入済み物理モデルの一覧表示・削除機能を統合。
既存の `OnPhysicsRequested` フロー（`PythonEditorForm.Physics.cs`）を流用し、
追加ボタンは正式な `PhysicsModelPickerForm` を開くように統一。

変更内容
| #|変更|場所|
| ---|---|---|
| A1|`PhysicsModelRemoveRequested` イベント定義|ComponentEdit/EditPanel.cs|
| A2|`InitializePhysicsManagementUI()` で ListBox + Button を追加|EditPanel.cs|
| A3|`RefreshPhysicsModelsList(currentCode)` で一覧更新|EditPanel.cs|
| A4|`PhysicsModelRemoveRequested` の購読|PythonEditorForm.cs WireEditPanel|
| A5|`OnPhysicsModelRemoveRequested` ハンドラ実装|PythonEditorForm.cs|
| A6|`textEditor_TextChanged` でリスト同期|PythonEditorForm.cs|
| A7|正規表現から `^` を除去しインデント許容|PhysicsBlockManager.cs|

設計上の重要ポイント
- **既存フローの尊重**: `PythonEditorForm.Physics.cs` 内の `OnPhysicsRequested` が
  既に `PhysicsModelPickerForm` を開く実装を持っているため、
  左パネルの [Physics] ボタンも同じ経路 (`editPanel.PhysicsRequested`) を経由する。
- **削除ロジック**: `PhysicsBlockManager.RemovePhysicsBlock` は
  `# === physics: <name> (auto-inserted) ===` 〜 `# === /physics: <name> ===`
  のマーカー間を削除する。正規表現はインデントを許容する。
- **リアルタイム同期**: `textEditor_TextChanged` で
  `editPanel.RefreshPhysicsModelsList(textEditor.Text)` を呼び、
  コード編集直後に左パネルへ反映。

定量検証結果
| 項目|結果|
| ---|---|
| ビルド|✅ 0 エラー|
| `PhysicsBlockManagerTests`|✅ 4/4 PASS|
| 手動確認|✅ 追加→一覧表示→削除の一連フローが動作|

関連ファイル
| ファイル|用途|
| ---|---|
| ComponentEdit/EditPanel.cs|左パネル UI + イベント定義|
| ComponentEdit/PythonEditorForm.cs|イベント購読 + 削除ハンドラ|
| ComponentEdit/PythonEditorForm.Physics.cs|既存の追加フロー（流用）|
| ComponentEdit/PhysicsBlockManager.cs|マーカー検出・削除ロジック|
| parts/Physics/PhysicsModelPickerForm.cs|モデル選択ダイアログ|
