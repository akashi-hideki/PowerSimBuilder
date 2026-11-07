# THREAD HANDOFF — PowerSimBuilder 現状スナップショット

> **用途**: 新スレッド開始時にこのファイル 1 つ読めば即座に作業再開できる「現在の状態」を記録する。
> **累積履歴**: `HANDOFF_MVP1_2.md`（バージョン別・罠 #1〜#57）を参照。
> **最終更新**: 2026-11-07（v1.9.0 リリース + QA snapshot 1 アクション化 + Trap #80-#88）

---


**🚀 GO 判定済み（2026-11-08）** — 11/22 公開が確定。
- 判定資料: `docs/qa/2026-11-08-gonogo/DECISION.md`
- 承認者: Hideki Akashi
- 定量証跡: 200.681 / 200.680 / 1.45 / 1.45 / gating PASS（2 経路一致）
- 品質: 1〜7 全 PASS
- Legal: 問題発生時対応方針（公開ブロッカーではない）

---

## 2026-11-07 更新サマリ（Role Snippets B-3 完了）

| 項目 | 値 |
|--------|------------------------------|
| origin/main | `97f536e`（tag `v1.9.0`） |
| C# テスト | 599 passed / 2 skipped |
| Python テスト | 457 passed |
| E2E (UITests) | 15 passed / 1 skip |

### 完了機能: Role Snippets B（Register）

| 機能 | 状態 |
|---|---|
| `RegisterSnippetDialog`（右クリック + ツールバー導線） | ✅ |
| `STABLE_USER_CODE_START` マーカー存在判定（コンポーネント型非依存） | ✅ |
| 上書き確認 + メタデータ（Layer/Func/Description） | ✅ |
| i18n 12 キー追加（374 keys） | ✅ |
| B-3c: `SetPythonCode` / `LoadFile` 後の自動再評価 + `menuItemRoleSnippets` マーカー判定統一 | ✅ |

### テスト自動化（29 本）

| テストクラス | 件数 | 内容 |
|---|---:|---|
| `RoleSnippetVisibilityTests` | 15 | L1: `ContainsRoleMarkers` Theory 9 / L2: `Form_*` + `SetPythonCode` + `MenuItemRoleSnippets` Fact 6 |
| `RoleSnippetRegistrarTests` | 8 | Extract / Register / バリデーション |
| `RoleSnippetStorageTests` | 6 | index.json / .py ファイル I/O |

### 追加された罠（Trap #80-#88）

| # | 罠 |
|---|---|
| 80 | **✅ 恒久化完了**: `gcommit.ps1` 冒頭で `Stop-Process PowerSimBuilder`（`error (CS\|MSB)` 検出も併用） → `"error (CS\|MSB)"` + `Stop-Process` |
| 81 | `<ClassName>Test.cs` / `<ClassName>Tests.cs` で CS0101+CS0111 → **恒久化完了**: `tools/check_known_traps.py` で重複 public 非 partial クラスを検出（pre-commit + verify_all [0/6] の 2 段階） |
| 82 | `<ItemGroup>` ネスト → MSB4232 → **恒久化完了**: `tools/check_known_traps.py` で `xml.etree` により構造検証（pre-commit + verify_all [0/6]） |
| 83 | **✅ テスト実装済み**: WinForms `Control.Visible` は親 Form 非表示時 **常に false** → 内部フラグ `_registerSnippetAvailable` で意図観測 |
| 84 | C# verbatim 文字列内の `@"^\\s*..."`（二重バックスラッシュ）は regex 不正 → 純粋関数 `ContainsRoleMarkers` に切り出し |
| 85 | **✅ 恒久化完了**: `verify_all.ps1` `[4/6]` FlaUI flaky → Start-Sleep 3 + 1 回リトライ（retry-once） |
| 86 | **✅ 恒久化完了**: WinForms `TextBox.Name` は UIA で AutomationId を返す（値は `AsTextBox().Text`）+ `ShowInTaskbar=false` modal は UIA3 desktop tree に出ない → Win32 `EnumWindows` フォールバック必要（Trap #15 再発） |
| 87 | **✅ 恒久化完了**: `release.py` は部分成功で非冪等（タグ Push 後 `gh` 認証切れ → 再実行不可で `v1.9.0` リリース手動復旧）→ `tag_exists_remote` / `release_exists` / `--force` / resume モード追加で構造的解消 |
| 88 | `demo_main.py` に `PSB_DUMP_CSV` 非対応 → **恒久化完了**: `PythonSimulationMainBuilder` 生成テンプレートに組み込み |
| 89 | `dump_csv` は 145MB（dt=0.001 × 全列） → **恒久化完了**: `PSB_DUMP_CSV_HZ` / `PSB_DUMP_CSV_COLUMNS` で書き込み時に 1Hz / 13 列へフィルタ（**145MB → 53KB / 99.96% 削減**）。`downsample_csv.py` は後方互換のため残置 |
| 90 | `& script.ps1 2>&1` は `Write-Host` 出力をキャプチャしない（PS 5.1 仕様）→ **恒久化完了**: `qa_snapshot.ps1` の `*>&1` で全ストリーム捕捉（491B → 3.9KB） |
| 91 | `refresh_ip_docs.py` の WHITE_PAPER status regex は `[^\n]*` で行末まで match する必要あり（`[^*]*\*\*` だと閉じ `**` で止まり ` (StaTable...)` サフィックスが重複 → 非冪等） |
| 92 | `qa_snapshot.ps1` は既存 `generated/demo_main.py` を無条件実行 → 別プロジェクト由来（FirstOrderLag）だと 0MB CSV。UITests（Dahlquist）が `demo_main.py` を上書き → 次回実行で壊れる。**恒久化完了**: `[0/8]` 事前検証 + `[8/8]` 前後で `demo_main.py` preserve/restore |

### verify_all.ps1 統合

`[4c] RoleSnippets (Register Snippet visibility)` を `[4b]` 直後に追加。
`verify_all.ps1` 一発で B-3a〜c の回帰検出が可能。

### 未着手（次セッション候補）

| # | 内容 | 規模 |
|:---:|---|:---:|
| 1 | **B-5 FlaUI E2E**: `Test_ShowRegisterSnippetDialog` 経由でダイアログ自動化 | ✅ 完了 `7650d34` |
| 2 | `gcommit.ps1` 冒頭で `PowerSimBuilder.exe` kill（Trap #80 恒久対策） | ✅ 完了 `535357b` |
| 2b | `verify_all.ps1` `[4/6]` FlaUI flaky 対策（Start-Sleep 3 + 1 回リトライ、Trap #85） | ✅ 完了 `acde867` |
| 2c | `release.py` の冪等化（Trap #87：タグ既存 + gh 失敗後の再実行可能化） | ✅ 完了 `da10873` |
| 3 | 11/08 GO/NO-GO 準備（README / 告知文面） | M |
| 4 | v1.9.0 リリース準備（CHANGELOG） | ✅ 完了 `28f383d` |

---


## QA Snapshot の使い方（2026-11-07 追加）

GO/NO-GO・週次・リリース前の定量証跡を **1 コマンド** で `docs/qa/<Label>/` に生成する。
145MB の full CSV は `tmp/qa_full/` に退避（gitignore）、1Hz / 主要 13 列のみ追跡。

### クイックスタート

```powershell
# GUI で oven_control_statable.psbproj を Open → Create Simulation 実行済みが前提

# GO/NO-GO フル証跡（~3 分、verify_all 含む）
.\tools\qa_snapshot.ps1 -Label 2026-11-08-gonogo

# 高速（verify_all スキップ、~30 秒）
.\tools\qa_snapshot.ps1 -SkipVerifyAll
```

### パラメータ

| 引数 | 既定値 | 用途 |
|---|---|---|
| `-Label` | `yyyy-MM-dd`（今日） | `docs/qa/<Label>/` のサブディレクトリ名 |
| `-Project` | `examples\demos\oven_control_statable.psbproj` | 対象 `.psbproj` |
| `-Target` | `ovenchamber.outputs.oven_current_temp=200` | `analyze_sim_csv.py --target` |
| `-CsvHz` | `1.0` | ダウンサンプル周波数 |
| `-SkipVerifyAll` | off | `verify_all.ps1` をスキップ（高速モード） |
| `-Force` | off | 既存 `docs/qa/<Label>/` を上書き |
| `-DryRun` | off | 実行内容の表示のみ（ファイル書き込みなし） |

### ユースケース

```powershell
.\tools\qa_snapshot.ps1 -DryRun                                  # 計画確認（実行なし）
.\tools\qa_snapshot.ps1 -SkipVerifyAll                           # 高速チェック（~30 秒）
.\tools\qa_snapshot.ps1                                          # 週次（今日の日付）
.\tools\qa_snapshot.ps1 -Label 2026-11-08-gonogo -Force          # GO/NO-GO 証跡
```

### 生成される 8 ファイル

| ファイル | 内容 | 所要 |
|---|---|---:|
| `environment.json` | commit / tag / Python / .NET / OS / dirty | <1s |
| `statable_psbproj.txt` | `.psbproj` 直接指標（ss / ovs / settle） | 5s |
| `metrics.txt` | CSV 再計算指標（ss / ovs / settle / IAE） | <1s |
| `controller_box_gating.txt` | gating 双方向検証（pass + block） | <1s |
| `simulation_signals_1hz.csv` | 301 行 / 13 列 / ~54KB | <1s |
| `simulation_signals.png` | 波形画像 | （生成済みコピー） |
| `verify_all.log` | 全 6 セクションのテストログ | ~3 分 |
| `README.md` | 要約 + 再生成コマンド | <1s |

### 内部ステップ（8 段階）

```
[1/8] demo_main.py に PSB_DUMP_CSV 対応追加（Trap #88、冪等）
[2/8] capture_env.py → environment.json
[3/8] MPLBACKEND=Agg + PSB_DUMP_CSV で demo_main.py 実行 → full CSV
[4/8] downsample_csv.py → 1Hz / 主要列のみ
[5/8] verify_statable_psbproj.py → statable_psbproj.txt
[6/8] analyze_sim_csv.py → metrics.txt
[7/8] verify_controller_box.py → controller_box_gating.txt
[8/8] verify_all.ps1 → verify_all.log（-SkipVerifyAll で省略可）
```

### 期待値（oven_control_statable.psbproj）

| 指標 | 期待値 |
|---|---:|
| `ss` | 200.68°C |
| `ovs` | 1.45% |
| `settle` | 64.6s |
| gating | `MwOven:PidHeating` PASS |

### 終了コード

| code | 意味 |
|:---:|---|
| 0 | 成功（verify_all 失敗も WARN として記録され exit 0） |
| 1 | 前提不足 / ステップ失敗（project 不在、demo_main.py 不在、python エラー等） |

### トラブルシュート

| 症状 | 原因 | 対処 |
|---|---|---|
| `[ERR] project not found` | `-Project` パス誤り | GUI で Open したプロジェクトのパスを指定 |
| `[ERR] generated\demo_main.py not found` | Create Simulation 未実行 | GUI で Open → Create Simulation |
| `[ERR] ... already exists` | 既存 `docs/qa/<Label>/` あり | `-Force` または別 `-Label` |
| `exit code: -1` / `NativeCommandError` | PS 5.1 の native stderr 扱い | 修正版 `qa_snapshot.ps1` の `Invoke-PythonChecked` で回避済み |
| `tag=(no tag)` | dirty working tree | 正常動作（commit を残すため） |
| `simulation_signals.png` が小さい（114KB） | `[8/8] verify_all` 内 `[3.5/6]` が上書き | 内容は同じ波形。`-SkipVerifyAll` で回避可 |

### 前提ファイル（既存）

| ファイル | 役割 |
|---|---|
| `tools/capture_env.py` | 環境情報 JSON 生成 |
| `tools/patch_demo_main_csv.py` | `demo_main.py` に `PSB_DUMP_CSV` 対応（冪等） |
| `tools/downsample_csv.py` | 145MB CSV → 1Hz / KEY_COLUMNS 13 列 |
| `tools/verify_statable_psbproj.py` | `.psbproj` 直接シミュレーション + 指標 |
| `tools/analyze_sim_csv.py` | CSV から ss / ovs / settle / IAE 再計算 |
| `tools/verify_controller_box.py` | gating pass/block 検証 |
| `tools/verify_all.ps1` | 全テストスイート |

### 関連 Trap

| # | 罠 | 対応 |
|---|---|---|
| 88 | `demo_main.py` の `PSB_DUMP_CSV` 対応が毎回手動 | **恒久化完了**: `PythonSimulationMainBuilder` テンプレート組込（`d699001`） |
| 89 | 145MB CSV がデフォルト出力 | `downsample_csv.py` で 1Hz / 13 列に削減、`tmp/qa_full/` に退避 |

---

### 公開方針: 弁護士相談のタイミング（2026-11-07 決定）

- **公開前の必須条件ではない**。OSS 公開（11/22）は先に進める
- **問題発生時（クレーム・侵害指摘等）に速やかに弁護士相談**
- 根拠: ソロ開発・OSS（Apache 2.0）・既存類似プロジェクト（StaTable）が公開済み
- Legal リスクは GO/NO-GO のブロッカーから除外（Medium として記録）

---

## IP ドキュメント更新（2026-11-07 追加）

`docs/ip/` の 9 ファイルを **1 コマンド** で最新化する。リリース時・公開前に実行。

### クイックスタート

```powershell
python tools/refresh_ip_docs.py --dry-run
python tools/refresh_ip_docs.py --version v1.9.0 --test-counts 599,457,16,30
python tools/refresh_ip_docs.py
```

### パラメータ

| 引数 | 既定値 | 用途 |
|---|---|---|
| `--version` | `git describe --tags --abbrev=0` | タグ。WHITE_PAPER/OSS_COMPLIANCE に反映 |
| `--test-counts` | WHITE_PAPER.md から抽出 | `C#,Python,UITests,RoleSnippet` |
| `--dry-run` | off | 変更予定のみ表示 |

### 更新される 9 ファイル

| ファイル | 生成元 |
|---|---|
| `git_branches.txt` | `git for-each-ref refs/heads refs/remotes` |
| `git_log.txt` | `git log --all -1000` |
| `git_stats.txt` | コミット数 / タグ数 / 開発者 |
| `git_tags.txt` | `git for-each-ref refs/tags` |
| `nuget_packages.json` | `dotnet list --include-transitive --format json` |
| `licenses_nuget.csv` | 同上（MIT マッピング） |
| `licenses_full.csv` | nuget + python_fmpy 結合 |
| `WHITE_PAPER.md` | version / date / status（test counts） |
| `OSS_COMPLIANCE.md` | header / conclusion（version） |

### 更新されない（手動管理）

| ファイル | 理由 |
|---|---|
| `licenses_python_fmpy.csv` | fmpy バージョン固定 |
| `FAQ.md` | 内容レビューが必要 |
| `LAWYER_CANDIDATES.md` | 法務判断 |
| `LAWYER_CONSULTATION.md` | **ローカルのみ（tracked なら警告）** |

### 冪等性

- `git_*` / `nuget_packages.json` / `licenses_*.csv`: 毎回同一内容で再生成
- `WHITE_PAPER.md` / `OSS_COMPLIANCE.md`: **2 回目は `(no change)`**
  - Trap #91: status regex は `[^\n]*` で行末まで match（`[^*]*\*\*` だとサフィックス重複）

### リリースフロー

```powershell
python tools/refresh_ip_docs.py --version v1.9.x --test-counts 599,457,16,30
git diff docs/ip/
.\tools\gcommit.ps1 "docs(ip): refresh for vX.Y.Z" -Push
```

### トラブルシュート

| 症状 | 対処 |
|---|---|
| `no git tag found` | `--version vX.Y.Z` を明示 |
| `[WARN] dotnet list failed` | .NET SDK 確認 |
| `[WARN] LAWYER_CONSULTATION.md is TRACKED` | `git rm --cached docs/ip/LAWYER_CONSULTATION.md` |
| dry-run で `WHITE_PAPER.md (status)` が毎回出る | Trap #91 確認 |

---
```

## 2026-11-03 更新サマリ

| 項目 | 値 |
|--------|------------------------------|
| origin/main | `1a20211` + 本コミット |
| C# テスト | 567 passed / 2 skipped |
| Python テスト | 457 passed |
| E2E (UITests) | 12 passed / 1 skip |

### 完了機能

#### 複数選択 (Multi-Select)
- `DesignCanvas.SelectedComponents` (IReadOnlyList)
- `SelectComponent(c, additive)` / `SelectComponents` / `ClearSelection` / `IsComponentSelected`
- Ctrl+click トグル、一括移動 (multi-drag)
- `_suppressSelectionChanged` ガードで再帰防止

#### グループ保存/ロード
- `SimPartsManege/GroupManager.cs` (Extract / Save / Load / LoadAndAppend)
- `.psbgroup` (JSON, 相対座標, 接続再マッピング, 重複名 `_2` 付与)
- ツールバー「Save Group...」「Load Group...」(en/zh i18n)

#### ControllerBox 編集修正
- `JsonElement` 対応 (`AsStringOrNull` ヘルパー) — psbproj ロード後の `raw as string` が null になる問題
- パレット ControllerBox: `*_powersim.py` をユーザー選択（ロール関数編集用）
- `EditDebugLog` (Debug のみ、`PSB_EDIT_DEBUG=1` で有効)

#### ツール標準化
- `tools/gcommit.ps1` = 標準コミットツール（pre-commit auto-fix を自動で再ステージ）
- `tools/finish.ps1` = 作業完了時の一括処理
- `tools/enable-utf8.ps1` = コンソール文字化け解消

### 追加された罠 (Trap #65-#74)

| # | 罠 |
|--------|------------------------------|
| 65 | `IsSelected` setter の `SelectedChanged` 再帰発火 → `_suppressSelectionChanged` |
| 66 | PS 5.1 で表セパレータ行がパースエラー → `('-' * N)` で動的生成 |
| 67 | `ComponentControl.OnMouseDown` と `HandleComponentMouseDown` の二重処理 → 一本化 |
| 68 | `mixed line ending` フック連続中断 → `gcommit.ps1` を使用 |
| 69 | `PowerSimBuilder.I18n` クラスと namespace 衝突 → `I18nKeys` に変更 |
| 70 | `git rm` に `-ErrorAction` → git は PS パラメータ非対応 |
| 71 | psbproj `Parameters` は `JsonElement` 型 → `AsStringOrNull` で変換 |
| 72 | パレット ControllerBox は `generated_path` 空 → ユーザー選択に変更 |
| 73 | 空行混入で `Replace` アンカー不一致 → `[regex]::Replace` + `(?s)` |
| 74 | `user_components/ControllerBox.py` はアダプタ（ロール関数なし）→ `*_powersim.py` を選ばせる |

### 追加テスト（21 本）

- `DesignCanvasMultiSelectTests` (6)
- `DesignCanvasCtrlClickTests` (2)
- `DesignCanvasDragTests` (2)
- `I18nGroupKeysTests` (8)
- `GroupManagerTests` (4)
- `GroupManagerLoadTests` (2)
- `GroupMultiSelectE2ETests` (1)
- `GroupManagerLoadErrorTests` (6)
- `ControllerBoxEditFallbackTests` (2、新 API 用に書き換え)


### Phase C 完了（FlaUI + ログ E2E）

- `ControllerBoxEditE2ETests` (2)
  - C1: 既存 ControllerBox → `OpenEditor` 直接（ログ検証）
  - C2: パレット ControllerBox → `PromptUserPickStart` + ESC キャンセル
- `EditLogReader` ヘルパー（JSON Lines パーサ + `WaitForEvent`）
- `Fixtures/with-controllerbox.json` 追加
- 実行時間: 22s → 5s（WorkingDirectory 修正）


- `verify_all.ps1` に `[4b]` C1/C2 検証を追加（明示的フィルタ）

### 追加罠（Trap #75-#76）

| # | 罠 |
|---|---|
| 75 | FlaUI `Application.Launch` の CWD はテスト実行ディレクトリ → 相対パス解決に `WorkingDirectory = ProjectRoot` 必須 |
| 76 | `Application` が `FlaUI.Core` と `System.Windows.Forms` で曖昧 → 完全修飾名 |

### テスト総数（2026-11-03 時点）

- C# 全テスト: 570 Pass / 2 Skip
- UITests: 13 Pass / 1 Skip
- Python: 463 Pass

### 2026-11-03 追加修正（実シミュレーション実証）

- `oven_control_statable.psbproj` で **GUI 経由の実シミュレーション**がグラフ表示まで成功
- `RepairGeneratedPaths` 修正:
  - `JsonElement` 対応（`AsStringOrNull` ヘルパー）
  - 相対パス解決（`ResolveRelativePath` でリポジトリルート探索）
- 修正前: `generated_path = ""`（glob フォールバック依存）
- 修正後: `generated_path = "generated/CookingHeaterController_powersim.py"`（明示的）

### 追加罠（Trap #78）

| # | 罠 |
|---|---|
| 78 | `RepairGeneratedPaths` の `JsonElement.ToString()` + CWD 依存 `File.Exists` → 相対パスが無効判定され `""` に上書きされる |

### 実シミュレーション検証結果（2026-11-03）

- Open → Create Simulation → Run（GUI）: ✅
- ControllerBox gating (`MwOven:PidHeating`): ✅ PID 出力を素通し
- PID + OvenChamber 制御: ✅ 200°C 整定
- 30 秒短縮実行: ✅ exit 0、波形生成

### 2026-11-03 GUI Run 実証（Trap #79 修正後）

- `oven_control_statable.psbproj` を GUI で Open → Create Simulation → Run で **300 秒シミュレーション完走**
- `generated_path` が絶対パス（`C:\dev\...\CookingHeaterController_powersim.py`）で出力
- `F_ReadThermistors` 実装（`thermistor_top = oven_current_temp + 5`）
- 波形 `simulation_signals.png` 更新（200°C 整定）

### 追加罠（Trap #79）

| # | 罠 |
|---|---|
| 79 | `generated_path` 相対パス + GUI Run の CWD=`generated/` → 二重 `generated/` で `FileNotFoundError`。`PythonSimulationMainBuilder` で絶対パス化して解決 |

### CLI vs GUI Run の差異（Trap #79 の背景）

| 項目 | CLI 実行 | GUI Run（PythonExecutor） |
|---|---|---|
| CWD | リポジトリルート | `generated/`（temp_*.py のあるフォルダ） |
| `generated_path` 相対パス | 解決可能 | **二重 generated/ で失敗** |
- 対策: `PythonSimulationMainBuilder` で `generated_path` を**絶対パス**で書き出し

### 2026-11-03 追加: Role Snippets SPEC

- `docs/SPEC_ROLE_SNIPPETS.md` 策定（198 行、実装未着手）
- 目的: 一般ユーザーが GUI でロール関数を編集・挿入できる仕組み
- 実装規模: 5〜6h（Phase 1-3 必須 = 3.5h、Phase 4-6 推奨）
- 後方互換: 完全（`PythonEditorForm` にボタン追加のみ）

### 未決事項（次回判断）

- **A**: 今日中に実装（5-6h、セッション延長）
- **B**: 次回実装（11/04、他のタスクと並行）
- **C**: 11/08 GO/NO-GO 優先（Role Snippets は v1.9.x）

- `tools/edit_role_function.py` は開発者向けとして完成済み（`--show` / `--body` / `--dry-run`）
- `F_ReadThermistors` は実装済み（`thermistor_top = oven_current_temp + 5`）

### 2026-11-03 セッション固有情報（引継ぎ補足）

> 通常セクションには載らない、作業プロセス・状態・申し送り。

#### セッション振り返り（プロセス）

| 項目 | 内容 |
|---|---|
| セッション長 | 11/03 午前〜夕方（長時間） |
| 主な失敗パターン | アンカー不一致 5 回 / PowerShell CRLF 問題 3 回 / フック中断 4 回 |
| 発見したツール | `gcommit.ps1` / `finish.ps1` / `enable-utf8.ps1`（セッション途中で発見） |
| 最大の学び | 「実ファイルを見てからパッチを書く」「コミットは `gcommit.ps1`」 |

#### 次回以降の鉄則（必ず守る）

1. **コミットは `.\tools\gcommit.ps1 "msg" -Push`** — `mixed line ending` 中断を自動解決
2. **パッチ前に実ファイルを `Get-Content` で確認** — アンカー不一致防止
3. **CRLF 混在の可能性は `[regex]::Replace` + `\r?\n`** — ヒアストリング CRLF 対策
4. **`String.Replace` は全置換**（Trap #77） — 単一置換は `[regex]::Replace(..., count=1)`
5. **PowerShell 再起動後は `.\tools\enable-utf8.ps1`** — 文字化け防止
6. **大量 diff は `git diff --ignore-all-space --stat`** — CRLF 全体変換の検出

#### ファイル状態スナップショット（2026-11-03 終了時点）

| ファイル | 状態 |
|---|---|
| `generated/CookingHeaterController_powersim.py` | `F_ReadThermistors` 実装済み、他 59 関数は `return 0` スタブ |
| `generated/demo_main.py` | `t_end=300` / `generated_path` 絶対パス / `auto_events=demo_oven` |
| `examples/demos/oven_control_statable.psbproj` | 動作確認済み（GUI Run 300 秒完走） |
| `examples/demos/oven_control_nocbox.psbproj` | テスト用（ControllerBox なし、PID 直接制御） |
| `examples/demos/oven_control_statable_v181.psbproj` | 自己完結版（`*_powersim.py` 同梱） |
| `tools/edit_role_function.py` | 正式採用（`--show` / `--body` / `--dry-run`） |

#### 未整理事項（次回対応）

| 項目 | 内容 | 優先 |
|---|---|---|
| `tmp/*.py`（26 ファイル） | デバッグスクリプト多数。`.gitignore` 対象化 or 削除 | 🟡 |
| `examples/demos/oven_control_nocbox.psbproj` | テスト成果物。gitignore 対象化を検討 | 🟡 |
| `message` イベント残り 5 箇所 | `EditLogEvent` へ変換（任意） | 🟢 |
| Role Snippets | SPEC 済み、実装は A/B/C 判断待ち | 🔴 |
| 全 60 ロール関数のスタブ | `F_ReadThermistors` 以外は `return 0` | 🟡 |

#### 次回再開時の確認コマンド

```powershell
cd C:\dev\PowerSimBuilder
.\tools\enable-utf8.ps1
git log --oneline -5
git status --short
.\tools\verify_all.ps1
```

#### Python 編集ワークフロー（確定）

```
GUI: StaTable XML -> ControllerBox 生成
  -> OvenChamber/PID 配置 + 配線 + パラメータ設定
  -> Save As .psbproj（generated_path は絶対パス、Trap #79）
  -> 閉じる
Script: python tools\edit_role_function.py <powersim.py> <Layer> <F_Func> --body "code"
  -> GUI: Open .psbproj -> 編集内容確認 -> Create Simulation -> Run
```
### 次タスク

#### 優先 1: ログベース自動テスト
- **Phase B**: ログパスを環境変数 `PSB_EDIT_LOG` で上書き可能に
- **Phase A**: 構造化ログ (JSON Lines、`event` + params)
- **Phase C**: FlaUI + ログで編集経路テスト

#### 優先 2: 11/08 GO/NO-GO 判定準備
- README トップ更新（multi-select / group の紹介）
- 告知文面ドラフト（WeChat、中英併記）

#### 優先 3: v1.9.0 リリース
- CHANGELOG 更新、GitHub Release

---
## 🆕 2026-11-01 更新サマリ

| 項目 | 値 |
|---|---|
| origin/main | `85d755c` |
| C# テスト | 526 passed / 2 skipped |
| E2E | 5 passed |
| Python | 246 passed |
| 最新タグ | `v1.7.0` |
| 公開予定 | 11/15（土）、11/08（日）GO/NO-GO |

### 完了コミット（8 件）

- `85d755c`: Trap #57（fmpy 0.3.32 API + unzip）
- `1ee6b2a`: controller_box legacy fixture 優先（テスト復旧）
- `4d0062a`: FmuAdapter 2 バグ修正
- `50ebbd5`: oven_control_from_design_xml.psbproj
- `36d8584`: v1.8.0 CHANGELOG + README + Trap #53〜#56
- `07d7750`: oven_control_statable.psbproj
- `579dbc2`: user_components/ untrack + pre_public_check
- `6da9266`: user_physics_models/ untrack

### 6 デモ

1. demo_pid_first_order_lag.py (y=0.9738)
2. demo_statable_runtime_loop.py (T=200.00)
3. pid_closed_loop.psbproj (ss=0.991, settle 3.9s)
4. oven_control.psbproj (ss=200.68, ovs=1.45%, settle=64.7s)
5. oven_control_statable.psbproj (StaTable gating)
6. oven_control_from_design_xml.psbproj (XML full pipeline)



### 次タスク

1. GUI Add Physics -> Dahlquist -> Run (FMU E2E)
2. StaTable GUI -> simulation (launch.ps1 -Mode statable)
3. 11/08 GO/NO-GO 判定

### 制約

- CI workflow 停止中（`tmp/ci_disabled/`）、public 化後に復元
- Public 化保留（`pre_public_check.ps1` 全 [OK]、いつでも可）

---

## 📍 TL;DR

| 項目 | 値 |
|------|-----|
| **origin/main** | `f9a9653` |
| **最新タグ** | `v1.7.0` |
| **C# テスト** | 517 + 5（E2E）= 522 passed |
| **Python テスト** | 53 passed |
| **公開予定** | **11/15（土）**（11/08 GO/NO-GO 判定） |

---

## 🎯 2026-10-31 の成果（QA Day 1-3）

### OvenChamber 追加 + 4 デモ定量合格

| 成果 | コミット | 内容 |
|------|---------|------|
| OvenChamber コンポーネント | `cf48b4c` | ThermalMass ラップ、パレット登録 |
| t_end 永続化 + oven_control.psbproj | `6692492` | metadata.tEnd round-trip |
| README セクション 3-4 | `b907773` | 再現手順 + 定量比較 |
| pid_closed_loop IMC | `ab38f4b` | ki=1.0 極零相殺（ss=0.991, settle 3.9s） |
| Shown ハンドラ | `bdaa25d` | 初回起動ボタン可視性 |
| 回帰テスト 3 層 | `f9a9653` | metadata + SimTimeOverride + i18n |

### 4 デモの定量結果

| デモ | 信号 | ss | err | ovs% | settle |
|---|---|---:|---:|---:|---:|
| demo_pid_first_order_lag.py | y | 0.9738 | 0.026 | — | — |
| demo_statable_runtime_loop.py | T | 200.00 | 0.00 | — | — |
| pid_closed_loop.psbproj | output | 0.991 | 0.009 | 0.00 | 3.9s |
| oven_control.psbproj | T | 200.679 | -0.679 | 1.45 | 64.7s |

### QA Day 2-3: UI/UX + Error Handling

- 中国語/英語 UI 全画面 OK
- Save As: 「另存为...」/「Save As...」+ ダイアログタイトル
- PythonEditorForm: btnRun/btnReload/btnOpenFolder 表示 OK（Shown 修正で根治）
- numSimTime: 直接入力 + 10 刻み + tEnd 復元 OK

### 追加された検証ツール

| ツール | 用途 |
|---|---|
| `tools/verify_all.ps1` | build + metadata + PID + E2E + git 一括検証 |
| `tools/verify_pid.py` | 6 候補の PID ゲイン定量比較 |
| `tools/verify_pid_ascii.py` | 波形の ASCII 可視化 |
| `tools/inspect_psbproj.py` | `.psbproj` の中身検査 |
| `tools/verify_psbproj.py` | `.psbproj` の定量シミュレーション |

### 追加された罠候補（HANDOFF_MVP1_2.md 未記載）

| # | 罠 | 対策 |
|---|---|---|
| 53 | 新規コンポーネントを Dictionary に追加してもパレットに出ない | `Pallets/ComponentDescriptors.xml` 削除 → GUI 再起動 |
| 54 | `[IO.File]` は `Environment.CurrentDirectory` を見る | `(Resolve-Path).Path` で絶対パス化 |
| 55 | `@'...'@` ヒアストリングは貼り付け中断で壊れる | 行配列 `@('行1','行2')` で構築 |
| 56 | 非モーダル `Show()` + `AutoScaleMode.Font` で初回レイアウト未確定 | `Shown` イベントで Visible 再適用 + `PerformLayout()` |

---

## 🌅 次スレッド開始手順

```powershell
cd C:\dev\PowerSimBuilder
.\tools\verify_all.ps1
```

**期待**: HEAD=`f9a9653` 系、clean、E2E 5 passed、4 デモファイル表示。

---

## 📋 次タスク（優先度順）

| 優先 | タスク | 期日 |
|:---:|---|:---:|
| 🔴 | QA Day 3: エラーハンドリング + 回帰テスト | 11/04 |
| 🔴 | 11/15 公開準備（README トップ / 告知文面） | 11/08 GO/NO-GO |
| 🟠 | 品質保証（E2E 全シナリオ、中国語 UI 残り） | 11/03〜11/08 |
| 🟡 | 告知文面ドラフト（WeChat、中英併記） | — |
| 🟡 | `generated/temp_*.py` の自動クリーンアップ | 任意 |

---

## 📚 参考資料

| ファイル | 用途 |
|---|---|
| `HANDOFF_MVP1_2.md` | 累積履歴（罠 #1〜#52、#53〜#56 は追記待ち） |
| `QA_LOG.md` | QA Day 1-2 の結果記録 |
| `examples/demos/` | デモ 4 種（standalone 2 + `.psbproj` 2） |
| `examples/demos/README.md` | デモ説明（StaTable 7層 + ハイブリッドパターン） |
| `tools/status_check.ps1` | セッション開始時 status dashboard |
| `tools/verify_all.ps1` | 一括検証（build + metadata + PID + E2E + git） |
| `tools/qa_snapshot.ps1` | 1 アクションで GO/NO-GO 定量データ一式を生成 |
| `tools/capture_env.py` | 環境情報 JSON 生成 |
| `tools/downsample_csv.py` | 145MB CSV → 1Hz / 主要列のみ |
| `tools/patch_demo_main_csv.py` | demo_main.py に PSB_DUMP_CSV 対応追加（Trap #88） |
| `tools/gen_decision.py` | QA snapshot から GO/NO-GO 判定シート（DECISION.md）を生成 |
| `tests/PowerSimBuilder.UITests/ProjectRoundTripTests.cs` | E2E 5 テスト |

---

*最終更新: 2026-10-31 / QA Day 1-3 完了（`52ffbac`）*

### 2026-11-01 追加成果（GUI FMU E2E）

- GUI E2E `AddPhysicsAndRun_Dahlquist_Completes` 追加（SmokeTests.cs）
- 修正ファイル:
  - `ComponentEdit/ComponentManager.cs`: `--test-project` 時 enableRunButtons=true
  - `ComponentEdit/PythonEditorForm.cs`: SuppressDialogs プロパティ / ResolveRunDirectory() / Executor setter で Completed 購読
  - `CanvasForm/MainForm.cs`: テスト時 SuppressDialogs=true
  - `CanvasForm/DesignCanvas.cs`: ControllerBox 分岐の enableRun テスト対応
- UITests: 12 テスト（11 pass / 1 skip）— 並列無効化で flakiness 解消
- 生成 temp: `generated/temp_*.py` に Dahlquist 実行結果

### 2026-11-01 プロダクト改善（GUI E2E 副産物）

`cbacbf5` の GUI E2E 対応で、以下の既存バグを修正（通常起動の挙動が改善）:

1. **Run 完了表示** (`ComponentEdit/PythonEditorForm.cs`)
   - 変更前: Run 後 `lblStatus` が `Running...` のまま
   - 変更後: `Completed` イベント購読で `MsgCompleted` / `MsgExitedWithCode` に更新
   - 影響: Create Simulation 経由でも Run 完了が可視化

2. **新規コンポーネント Run** (`ComponentEdit/PythonEditorForm.cs`)
   - 変更前: `_filePath=null` で `Path.GetDirectoryName(null)` → 例外 → MessageBox
   - 変更後: `ResolveRunDirectory()` が ProjectDir → SuggestedInitialDirectory → CWD → %TEMP% の順でフォールバック
   - 影響: 新規作成コンポーネントでも Run 可能

### 検証済み

- `PowerSimBuilder.Tests`: 528 / 526 Pass / 2 Skip / 0 Fail
- `PowerSimBuilder.UITests`: 12 / 11 Pass / 1 Skip / 0 Fail
- 通常起動手動確認: ダブルクリック編集 / Create Simulation / ControllerBox Edit — すべて OK

### Trap #63: verify_all.ps1 の E2E フィルタが新テストを漏らす

- 症状: 昨日追加した `AddPhysicsAndRun_Dahlquist_Completes` が `verify_all.ps1` で実行されず、E2E が 5/12 のみ
- 原因: `--filter "FullyQualifiedName~ProjectRoundTripTests"` が残っていた
- 対策: フィルタを外して全 UITests を実行（`xunit.runner.json` で並列無効化済みなので安定）
- 検出: `verify_all.ps1` の E2E 結果が `合計: 5` なら古い、`合計: 12`（11 pass / 1 skip）が正常

### Trap #64: ControllerBoxDescriptorBuilder のポート数が psbproj と一致しない

- 症状: GUI インポート後のポート = 22 入力 / 20 出力、psbproj = 4 入力 / 5 出力
- 原因: Stage 1 (`self.globals[...]` の読み書き抽出) が全参照変数を拾う。psbproj は手動キュレーション (gating で `heater_command` 追加)
- 設計判断: **A 案（現状維持）** を採用。全変数をポート化しても動作上の問題なし
- 固定: `ControllerBoxDescriptorBuilderTests.DesignXml_Builder_ProducesExpectedBasePorts` で 22/20 を完全一致アサート
- gating の `heater_command`: `gate_input_port` パラメータで動的追加されるため GLOBAL_VARIABLES には含まれない
