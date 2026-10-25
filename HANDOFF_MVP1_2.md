# PowerSimBuilder 引き継ぎ: MVP-1.2 → v1.2.3 (L3 FlaUI 完全解決)

## プロジェクト概要
- PowerSimBuilder: StaTable のシミュレーション・コンパニオン
- .NET 10 WinForms + Python runtime（`fmpy` で FMU 実行）
- リポジトリ: C:\dev\PowerSimBuilder
- **v1.2.3 リリース予定**（L3 FlaUI 全 3 テスト緑）

## プロジェクト構成（重要）

> **罠**: メインの `.csproj` は **ルート直下**（`src\` 配下ではない）。
> `src/` ディレクトリは存在するが csproj を含まない（レガシー / 実験用）。

### ソリューション / プロジェクト

| パス | TFM | 役割 |
|------|-----|------|
| `PowerSimBuilder.sln` | - | ソリューション（唯一） |
| `PowerSimBuilder.csproj` | net10.0-windows | メインアプリ（WinForms） |
| `PowerSimBuilder.Tests\PowerSimBuilder.Tests.csproj` | net10.0-windows | L1/L2 xUnit テスト |
| `tests\PowerSimBuilder.UITests\PowerSimBuilder.UITests.csproj` | net10.0-windows | L3 FlaUI スモークテスト |

### ルートディレクトリ（C# ソース）

| ディレクトリ | .cs 数 | 内容 |
|-------------|-------|------|
| `CanvasForm/` | 34 | DesignCanvas, MainForm, CanvasManager |
| `ComponentEdit/` | 13 | EditPanel, PythonEditorForm, ComponentManager |
| `parts/` | 36 | シミュレーション部品。`parts/Physics/` に Physics レイヤー |
| `Services/` | 5 | AppServices 等 |
| `SimPartsManege/` | 2 | 部品管理（※スペル "Manege"） |
| `SimulationGenerate/` | 2 | ComponentCodeGenerator |
| `StaTableIntegration/` | 7 | StaTable 取り込み |
| `Resources/` | 0 | i18n JSON（`en.json` / `zh.json`） |

### よく使うパス

| 用途 | パス |
|------|------|
| メイン exe | `bin\Debug\net10.0-windows\PowerSimBuilder.exe` |
| L1/L2 テスト | `PowerSimBuilder.Tests\` |
| L3 UI テスト | `tests\PowerSimBuilder.UITests\` |
| UI テストフィクスチャ | `tests\PowerSimBuilder.UITests\Fixtures\` |
| Physics ソース | `parts\Physics\` |
| 参照 FMU | `tmp\Reference-FMUs\2.0\` |

## 現在の状態（v1.2.3 時点）

| 項目 | 値 |
|------|-----|
| **最新タグ** | **v1.2.3**（予定、L3 全 3 テスト緑） |
| v1.2.2 | L3 smoke tests (Test 1 のみ緑、Test 2/3 Skip) |
| v1.2.1 | L2 GUI tests + handoff |
| v1.2.0 | Physics Model Integration (MVP-1.2) |
| Physics tests | 81 passed |
| C# tests（全体） | 330 + 81 ≒ 411 passed |
| **L3 UI tests** | **3 passed（全緑）** |
| pre-commit | 全 9 フック Passed |
| ビルド警告 | 0（C#） |

## 公開 11/22（土）延期決定 — v1.7.0/v1.8.0/v1.8.1 ロードマップ

**2026-10-25 決定。旧計画（11/01 公開）を破棄し、11/22 公開へ延期。**

### 決定理由

| 観点 | 旧計画（11/01） | 新計画（11/22） |
|------|--------------|----------------|
| シミュレーション実演 | 未検証前提 | E2E 検証期間を確保 |
| ストーリー | "Real simulation test" が実態と乖離するリスク | 実測ベースで判断可能 |
| 罠 #42 再発 | リスクあり | 構造的に回避 |

### 実装状態（2026-10-25 実測）

罠 #42 の教訓に従い `git grep` でコードベースを実測:

| 機能 | 状態 | 根拠 |
|------|------|------|
| FMU パース | 実装済 | `PhysicsImportForm.ParseFmu` |
| fmpy スニペット生成 | 実装 + テスト済 | `PhysicsSnippetInserterTests` |
| Python 実行エンジン | 実装済 | `SimulationGenerate\PythonExecutor.cs` |
| Run ボタン | 実装済 | `PythonEditorForm.btnRun` → `_executor.Run()` |
| PNG 生成 | 実装済 | `PythonSimulationMainBuilder.cs:827` |

**→ MVP-2 は「実装済み、E2E 未検証」の状態**（設計段階ではない）。

### 2 つの独立パス（重要区別）

| パス | 内容 | E2E 証明 |
|------|------|:---:|
| **A** | StaTable XML → ControllerBox → シミュレーション → PNG | 10/25 証明済（`generated/demo_main.py`） |
| **B** | Component + FMU → fmpy load_fmu → シミュレーション → PNG | 未証明（スニペット挿入のみテスト済） |

**公開ストーリーの判断は #1（10/29）の結果待ち。**

### ロードマップ（小刻みリリース）

| バージョン | 内容 | 時期 | 公開 |
|-----------|------|:---:|:---:|
| **v1.7.0** | `tools/handoff_editor.py`（3 モード GUI） | 10/28 | 内部 |
| **v1.8.0** | パスB E2E 証明 + デモシナリオ強化 | 11/13 | 内部 |
| **v1.8.1** | E2E 回帰 + 修正 | 11/20 | — |
| **公開** | v1.8.1 タグで公開 | **11/22（土）** | ✅ |

### v1.8.0 タスク

| # | タスク | 検証内容 | 時期 |
|---|--------|---------|:---:|
| 1 | FMU E2E 手動テスト | Component → Add Physics → FMU → Run → PNG | 10/29 |
| 2 | 発見バグ修正 | パスB が動かない場合 | 11/1–11/10 |
| 3 | デモシナリオ強化 | ステップ入力 + 60秒以上 + 複数信号 | 11/8–11/10 |
| 4 | デモ用 StaTable XML 準備 | 公開時に使える制御系 | 11/10 |
| 5 | README に実演手順追記 | パスA + パスB の 2 デモ | 11/11 |

### 公開ストーリー 3 案（#1 の結果で確定）

| 案 | 条件 | ストーリー |
|----|------|-----------|
| **A** | パスA のみ証明 | 「StaTable 設計 → シミュレーション生成 → 波形出力」 |
| **B**（推奨） | パスA + パスB 証明 | 「StaTable 設計 + 物理モデル（FMU）統合 → 閉ループ検証」 |
| **C** | パスB 失敗 | v1.8.0 の FMU 修正を 11/22 以降に延期、v1.7.0 で公開 |

### 保留事項の再整理

| # | 項目 | 更新 |
|---|------|------|
| 1 | リポジトリ public 化 | **11/22 公開と同時**（従来: シミュレーションサンプルテスト完了後） |
| 2 | カタログ公開配布の再開 | 上記と連動 |
| 3 | `LAWYER_CONSULTATION.md` | 変化なし（ローカルのみ） |

### 罠 #42 再発防止の運用

今後、「実装済み」と文書に書く際は以下を必須とする:

- `Select-String -Path "*.cs","**\*.cs" -Pattern "<機能名>"` でコード実測
- `Select-String -Path "*.cs","**\*.cs" -Pattern "<関連テスト名>"` でテスト実測
- 可能なら E2E の最小パスを実行検証

「テストがある」「UI がある」だけで「実装済み」と書かず、**E2E 検証の有無を明記**する。

## 2026-10-25-fmu-e2e 完了

**パス B（FMU 統合 E2E）の基盤完成。本日の実測で 4 バグを発見、#1/#4 を修正。`outputs["x"]` に Dahlquist の減衰値が出力されることを E2E 実証。**

### 発見した 4 バグ

| # | バグ | 対処 | 状態 |
|---|------|------|:---:|
| 1 | `from fmpy import load_fmu` は PyFMI の API（FMPy に存在しない） | `src/python_runtime/fmu_wrapper.py` 新設（FMPy `FMU2Slave` ラッパー）+ C# import 変更 | ✅ 修正済 (`88a8701` + `d9ba1b3` 系) |
| 2 | `demo_main.py` が `python_runtime.components` を import（生成 FMU 版を使わない） | 未着手（v1.8.0 タスク） | ⚠️ |
| 3 | Create Simulation 再実行で FMU 注入が消える（4800→2940 bytes） | 再現せず。別操作（パレット再配置等）が原因と判明 | ✅ 未発生 |
| 4 | FMU コードが try/except フォールバック `Component`（インデント 8）に注入され、`FirstOrderLag` 本体（インデント 4）が空のまま | `PhysicsSnippetInserter.FindClassMethodDef` 新設。トップレベルクラス直下のみ対象化 | ✅ 修正済 (`d9ba1b3`) |

### バグ #4 の詳細

`FindInitDefLine` / `FindUpdateDefLine` が `TrimStart()` + `StartsWith("def __init__")` の単純検索だったため、生成コード冒頭の `try: from component_base import Component / except ImportError: class Component:` 内の最初の `def __init__`（インデント 8）を誤選択していた。

修正後は `IsTopLevelClassMethod` で「インデント 0 の `class` 直下」のみを対象化。フォールバッククラスを自動スキップ。

### パス B E2E 実証結果

`tmp/test_first_order_lag_e2e.py` の実行結果:

has dahlquist attr: True
fmu type: _FmuWrapper
=== update loop ===
t=0.0 outputs={'output': 0.0, 'x': 0.9}
t=0.1 outputs={'output': 0.0, 'x': 0.81}
t=0.2 outputs={'output': 0.0, 'x': 0.729}
t=0.3 outputs={'output': 0.0, 'x': 0.6561}
t=0.4 outputs={'output': 0.0, 'x': 0.59049}
=== DONE ===

Dahlquist（`der(x) = -k*x`, k=1）の Euler 積分が FMPy `FMU2Slave.doStep()` 経由で正しく動作。

**残る問題（バグ #5）**: FMU 出力名 `x` と標準出力名 `output` の不一致。`PhysicsPortMapping.Identity`（デフォルト）が変換しないため `outputs["output"]` は 0.0 のまま。`outputs["x"]` に FMU 値が入る。

### python_runtime 構造訂正（罠 #42 応用: 文書と実装の実測乖離）

旧記載「`src/` はレガシー」は誤り。実測:

| 旧記載 | 実測 |
|--------|------|
| `python_runtime` はルート直下 | **`src/python_runtime/` が実体**（`pip install -e` で `site-packages` にリンク） |
| `components.py`（単一ファイル） | `components/` パッケージ（`first_order_lag.py` 等） |
| `src/` はレガシー | **実行時モジュールの実体。触るときは要警戒** |

`python_runtime.__file__` = `src\python_runtime\__init__.py` で確認済み。

### 明日（10/26）のタスク

| # | タスク | 規模 | 備考 |
|---|--------|:---:|------|
| 5 | `PhysicsPortMapping` の `Identity` 時 auto-fallback（単一出力なら自動マップ） | S | `parts/Physics/ModelYaml.cs` |
| 2 | `demo_main.py` import 戦略（`generated.<Name>` 優先、なければ `python_runtime.components`） | M | `SimulationGenerate/*.cs` |
| 2b | パス B 完全 E2E（Create Simulation → demo_main.py 実行 → PNG 生成） | S | 2 完了後 |

### 新規罠候補（#46）

**Python コードを PowerShell に直接貼ると `from` が予約語エラーになる**。`python -c "..."` は引用符エスケープ問題（罠 #1 系）もある。→ **Python スクリプトは必ず `.py` ファイルに保存して `python ファイル.py` で実行**。

本日、`_patch_v180_bug4.py` の内容を PowerShell プロンプトに貼ってしまい、大量の ParserError を誘発（ファイルは無傷、時間損失のみ）。

## 2026-10-25-gui-e2e 完了

**GUI 起点のパス B E2E 完全動作。減衰波形がウィンドウ表示 + simulation_signals.png 保存。**

### 今夜解決したバグ

| # | バグ | 修正 | コミット |
|---|------|------|---------|
| #5 | FMU 出力 x → 標準 output の不一致 | PhysicsSnippetBuilder に auto-fallback（model.yaml の self-map も対象） | 8461c99 |
| #2 | demo_main.py が python_runtime.components を import | PythonSimulationMainBuilder.IsFmuEnhancedComponent で generated/<Type>.py を優先 | 08bd436 |
| #6 | plot_config 残骸 + plt.show() 順序 | 手動パッチで視覚確認（C# 恒久修正は明日） | — |

### GUI E2E 実証フロー

GUI: FirstOrderLag 配置 → Add Physics → Dahlquist → Insert
  → Create Simulation
  → demo_main.py: from FirstOrderLag import FirstOrderLag（FMU 版）
  → python demo_main.py
  → 減衰波形ウィンドウ表示 + simulation_signals.png 保存

出力値: outputs['output'] = outputs['x'] = Dahlquist Euler 積分（1.0 → 0.005）

### 残課題

| # | タスク | 規模 |
|---|--------|:---:|
| 6 | plot_config 自動生成（実コンポーネント列挙） | M |
| 6b | plt.show() を plt.savefig() の後ろに生成（C# テンプレート） | S |
| 7 | GUI Run 経路 | S |
| 10 | FMU stepSize 警告（dt 0.001 vs 0.1） | S |
| 11 | plt.savefig に facecolor='white' 明示 | S |

### 今夜の学び（罠 #47/#48 候補）

- 罠 #47: plt.show() は対話 backend で figure を破棄する。plt.savefig() の前に置くと空 PNG になる
- 罠 #48: plot_config が存在しないコンポーネントを参照しても .get() で例外が出ず、白紙 PNG になる
- 罠 #46 再発: Python スクリプト断片を PowerShell に貼ると ParserError。GUI エディタ経由を徹底

## 2026-10-25-task7 完了

**GUI 完全 E2E 達成。btnRun 経路は既存実装で動作（タスク 7 = 完了）。**

### タスク 7 の結論

`MainForm.cs:830` で既に `new PythonEditorForm(file_path, true)`（`enableRunButtons=true`）で開かれていた。以前「btnRun 起動できない」と見えたのは、以下 3 バグの複合：

| バグ | 影響 |
|------|------|
| #2 | `demo_main.py` が標準版 `FirstOrderLag` を import → FMU 実行されない |
| #6a | `plot_config` が存在しない `engine`/`generator` 参照 → 白 PNG |
| #6b | `plt.show()` が `plt.savefig()` より前 → 空 PNG 保存 |

`08bd436`（#2）+ `fd990e8`（6a/6b）修正で、既存の `btnRun` 経路が「目覚めて」正常動作。

### GUI 完全 E2E 実証

GUI: FirstOrderLag 配置 → Add Physics → Dahlquist → Insert → Create Simulation → PythonEditorForm 起動 → btnRun 押下 → 減衰波形 PNG 生成（視覚確認済み）。

### BooleanNetwork1 混入事象（解消）

過去セッションで `FirstOrderLag` に BooleanNetwork1 を Add Physics した形跡が残り、`FirstOrderLag.py:100` に `self.booleannetwork1 = load_fmu(...)` が混入。エラーダイアログの traceback で判明。

**解消手順**:
1. `generated/FirstOrderLag.py` + `generated/demo_main.py` 削除
2. GUI で新規プロジェクト（Ctrl+N）から
3. `FirstOrderLag` 配置 → Add Physics → Dahlquist のみ Insert
4. Create Simulation

`user_physics_models/BooleanNetwork1` は存在するが、選択しなければ混入しない。

### 残タスク

| # | タスク | 規模 | 優先 |
|---|--------|:---:|:---:|
| 10 | FMU stepSize 警告（dt 0.001 vs 0.1） | S | 低 |
| — | `.gitignore` の `generated/` 方針整理 | S | 低 |
| — | デモシナリオ強化（公開用） | S | 中 |
| — | 11/22 公開 | — | 高 |

### 今日の学び

- タスク 7 は「新規実装」ではなく「既存実装の動作確認」だった。罠 #42 の逆パターン（実装はあるが他バグで見えなかった）
- BooleanNetwork1 混入のような「過去セッションのキャッシュ残留」は、新規プロジェクトから始めることで回避可能
- GUI からのエラーは MessageBox を Ctrl+C で全文コピーできる（PowerShell 経由不要、罠 #46 回避）

## v1.7.0 完了

INSERT_BODY_START
**HANDOFF 更新を GUI で完結させるツール。罠 #35 / #44 / #45 の再発防止を構造的に担保。**

### tools/handoff_editor.py（3 モード GUI）

| モード | 役割 | 罠対策 |
|--------|------|--------|
| Section insert | 最上位バージョンセクションの上に新規挿入 | 手作業の section 並べ替えミス防止 |
| Trap row | 罠テーブル末尾に自動 ID で追記 | 罠 #35 をセクション限定 + 行頭アンカーで回避 |
| Free edit | 全文 textarea 編集 | 一括置換・整形用 |

### 設計

| 層 | 内容 |
|----|------|
| HandoffDocument | 純粋ロジック。tkinter 非依存。headless テスト可能 |
| run_gui() | tkinter 依存はここだけ。--dry-run で GUI なし検証 |

### 罠対策の構造化

| 罠 | 従来 | handoff_editor.py |
|----|------|-------------------|
| #35 | 手作業 grep、都度ミス | 学んだ罠セクション限定 + 行頭アンカー正規表現 |
| #20 | PowerShell 貼り付け | Path I/O のみ、シェル経由なし |
| #19 | Resolve-Path / Join-Path | Path(__file__).parent.parent で絶対パス |
| #45 | 未対策 | read_text(encoding='utf-8') 強制 |

### テスト（11 tests）

全 11 テスト緑。`test_trap_table_isolation` が罠 #35 の回帰テスト（次タスク表の `| 1 |` を誤検出しないこと）。

### 実装時に検出・修正したバグ

- `insert_section` の `v` プレフィックス二重付与（v1.8.0 → vv1.8.0）→ テストで検出・修正済み

### ドッグフーディング実績

- この v1.7.0 セクション自体を `handoff_editor.py` の Section insert モードで追加
- 罠 #45 を Trap row モードで追加（このリリース内で）

### メトリクス

| 項目 | 値 |
|------|-----|
| Python テスト | 42 → 53（+11） |
| C# テスト | 496 passed / 2 skipped（変化なし） |
| tools/ スクリプト | 4 → 5 |
END_INSERT_BODY

## v1.6.1 完了（docs/ip + 死コード削除 + NuGet スリム化）

**2026-10-25 リリース。タグ `v1.6.1`（`fa09edc`）。README 更新 `290ce29`（21,041 → 22,552 bytes）。**

### リリースメタデータ

| 項目 | 値 |
|------|-----|
| タグ | `v1.6.1`（`fa09edc`） |
| リリースノート | Documentation 6 + Chores 2 |
| `--with-catalog` | なし（v1.6.0 の Release asset を継続使用） |
| README 更新 | `290ce29`（badge / What's New v1.6.0+v1.6.1 / test count / docs table） |

### 含まれる変更（v1.5.1 タグ以降）

| コミット | 内容 |
|---------|------|
| `9c77187` | 死コード削除 9 ファイル / 2,072 行（`parts/` 39 → 30） |
| `094912f` | NuGet 5 → 1（`csvhelper` / `Pythonnet` / `Costura.Fody` / `Fody` 削除） |
| `fa09edc` | licenses_full.csv 23 → 19 行、THIRD-PARTY-NOTICES.md 生成 |
| `0183306` | HANDOFF 更新（v1.6.0 以降 + 罠 #38〜#43） |
| `6bd1157`〜`46cae6f` | docs/ip 整備（13 ファイル） |
| `290ce29` | README 更新（v1.6.0 / v1.6.1 反映） |

### 最終メトリクス

| 項目 | 値 |
|------|-----|
| Python テスト | 42 passed |
| C# テスト | 496 passed / 2 skipped |
| `parts/` | 30 ファイル |
| NuGet | 1（NCalc のみ） |
| `docs/ip/` | 13 ファイル（12 公開 + LAWYER_CONSULTATION.md ローカルのみ） |
| 罠の記録 | #1〜#44 |
| GUI | 実機確認済み（二重起動 / save-reload / Catalog タブ 226 models） |

### 補足: 単一 EXE 配布

`FodyWeavers.xml` は削除。将来の単一 EXE 配布は **公式の `dotnet publish -p:PublishSingleFile=true`** を使用（.NET 10 公式サポート、Fody/Costura 不要）。


**docs/ip/ 整備 + 死コード削除。v1.6.1 タグは未作成（v1.6.1 リリース時にまとめる）。**

### docs/ip/ 整備（13 ファイル）

StaTable の `docs/ip/` と同等の構成に到達。

| # | ファイル | Git | 内容 |
|---|---------|:---:|------|
| 1 | `FAQ.md` | ✅ | 想定質問と回答（StaTable 連携、Windows 専用、シミュレーション開発中） |
| 2 | `OSS_COMPLIANCE.md` | ✅ | Apache-2.0、GPL/AGPL 混入なし、環境分離注記 |
| 3 | `WHITE_PAPER.md` | ✅ | 技術白書（骨格） |
| 4 | `licenses_full.csv` | ✅ | 23 ランタイム依存（NuGet 5 + fmpy 18） |
| 5 | `licenses_nuget.csv` | ✅ | NuGet 内訳 |
| 6 | `licenses_python_fmpy.csv` | ✅ | fmpy 内訳 |
| 7 | `nuget_packages.json` | ✅ | 生データ |
| 8 | `git_log.txt` | ✅ | 全履歴 346 件（`--all`） |
| 9 | `git_branches.txt` | ✅ | 4 ブランチ |
| 10 | `git_tags.txt` | ✅ | 31 タグ（peeled commit ハッシュ） |
| 11 | `git_stats.txt` | ✅ | 346 commits / 2025-08-07〜2026-10-25 / 31 tags |
| 12 | `LAWYER_CANDIDATES.md` | ✅ | 弁護士候補（StaTable 参照ベース） |
| 13 | `LAWYER_CONSULTATION.md` | ❌ **ローカルのみ** | 知財相談書面（会社名含む、`.gitignore` で除外） |

### 死コード削除（9 ファイル / 2,072 行）

`scan_parts_deadcode.ps1` で参照カウント → `check_delete_candidates.ps1` で外部参照判定 → 1 ファイルずつ削除。

| # | ファイル | 型 |
|---|---------|-----|
| 1 | `parts/HtmlViewer.cs` | `HtmlViewer` |
| 2 | `parts/SearchComboBox.cs` | `SearchComboBox` |
| 3 | `parts/RegistryManager.cs` | `RegistryManager` |
| 4 | `parts/ListDictionaryConverter.cs` | `CustomConverter`, `ListDictionaryConverter`, `MappingTable`, `Person`, `PropertyConversion`, `Employee`（サンプルコード） |
| 5 | `parts/CsvFolderReader.cs` | `CsvFolderReader` |
| 6 | `parts/TextTabs.cs` | `TextTabs` |
| 7 | `parts/FormulaCalculator.cs` | `FormulaCalculator` |
| 8 | `parts/SingleInstanceApp.cs` | `SingleInstanceApp` |
| 9 | `parts/DictionaryFile.cs` | `DictionaryFile`, nested `SerializableDictionary`, `SerializableKeyValuePair` |

**KEEP**: `parts/DataGridViewGeneric.cs`（`TabControlManager.cs` から 11 箇所使用）。

**`parts/` のファイル数**: 39 → **30**。

### 実証済み事項（削除後の GUI 実機確認）

| 項目 | 結果 |
|------|------|
| `dotnet build` | succeeded（既知の警告 4 件のみ） |
| `dotnet test` | **496 passed / 2 skipped / 0 failed** |
| 二重起動 | **2 プロセス**（`SingleInstanceApp` 未使用を実証） |
| プロジェクト保存 → 再読込 | OK（`DictionaryFile.cs` 削除の影響なし） |
| Import Physics Model / Catalog タブ | OK（3 タブ / ComboBox 5 項目 / 226 models） |

### 保留事項

| # | 項目 | トリガー |
|---|------|---------|
| 1 | **リポジトリ public 化** | IP 問題解決 + シミュレーションサンプルテスト完了後 |
| 2 | **カタログ公開配布の再開** | 上記と連動 |
| 3 | **`LAWYER_CONSULTATION.md` はローカルのみ** | 弁護士送付時は直接送付（GitHub 経由不可） |

### v1.6.1 タスク

| # | 内容 | 規模 |
|---|------|------|
| 1 | HANDOFF 更新（このセクション + 罠 #38〜#43） | S |
| 2 | `python tools/release.py --auto patch` で v1.6.1 リリース | S |

### v1.7.0 候補

| # | 内容 | 規模 |
|---|------|------|
| 1 | `tools/handoff_editor.py`（GUI 化、3 モード） | M |
| 2 | **MVP-2: シミュレーション実行**（`fmpy` 統合） | L |

## v1.6.0 完了（Catalog Sources dropdown + Release asset）

**Catalog タブに Sources ドロップダウンを追加。`release.py --with-catalog` でカタログを Release asset として自動配布。**

### Task A: FMI 2.0 / win64 優先ソート（`523c3d8`）

| 変更 | 内容 |
|------|------|
| `build_cross_check_catalog.py` | `_sort_key()` 追加。FMI 2.0 > 3.0 > 1.0、win64 > linux64 > darwin64 > c-code の順 |
| tests | 3 tests 追加（`test_sort_key_handles_short_paths` 等）。9 passed |

**実証**: 先頭 10 件がすべて `FMI 2.0 / cs / win64`（旧: FMI 1.0 / c-code が先頭）

### Task B: `release.py --with-catalog`（`523c3d8`）

| 変更 | 内容 |
|------|------|
| `release.py` | `--with-catalog` フラグ追加。カタログビルド → `gh release upload` で asset 添付 |
| dry-run 対応 | `[DRY] gh release upload v1.6.0 Resources\generated_catalog.json` を表示 |

### Task C: Release asset URL 登録（`523c3d8`）

| 変更 | 内容 |
|------|------|
| `catalog_sources.json` | `fmi-cross-check-release` 追加 |
| URL | `https://github.com/akashi-hideki/PowerSimBuilder/releases/latest/download/generated_catalog.json` |

### Task D: Sources ドロップダウン UI（`2507628`）

| 変更 | 内容 |
|------|------|
| `CatalogTab.cs` | `lblSource`（Label）→ `_cmbCatalogSource`（ComboBox） |
| `LoadSourceList()` | `Resources/catalog_sources.json` を読んで ComboBox を populate |
| `OnCatalogSourceChanged()` | 選択された source の URL を `_txtCatalogUrl` に自動セット。Custom 選択時は空文字でクリア |
| `CatalogSourceItem` | ネストクラス（Url + Label） |
| `csproj` | `catalog_sources.json` を `Content` + `CopyToOutputDirectory=PreserveNewest` で出力にコピー |
| i18n | `PhysicsCatalog.SourceCustom`（en: "Custom URL" / zh: "自定义 URL"）。346 keys |

### 動作確認済み

| 項目 | 結果 |
|------|------|
| ComboBox 5 項目表示（Custom + 4 sources） | OK |
| source 選択で URL 自動セット | OK |
| Custom 選択で URL クリア | OK |
| `dotnet build` | 成功 |
| Python テスト | 42 passed |

### カタログ公開配布: 保留中

`fmi-cross-check-release` の URL（`releases/latest/download/generated_catalog.json`）は、
**リポジトリが private のため未認証アクセスで 404** になる（GitHub 仕様）。

公開配布の再開条件:
1. IP 問題の解決（コード公開前の知財確認）
2. シミュレーションサンプルテスト完了
3. リポジトリを public に変更

それまでの間、GUI 動作確認は `local-dev-cross-check` source
（`http://localhost:8765/Resources/generated_catalog.json`）で実施可能。

### v1.6.0 リリースコマンド

```powershell
python tools\release.py --auto minor --with-catalog
```

これで v1.5.1 以来の全コミット（v1.5.2 + v1.6.0 A/B/C/D）が含まれ、
`generated_catalog.json` が Release asset として自動添付される。

## v1.5.2 完了（FMU カタログビルダー）

**実 URL カタログの生成ツール 3 本 + テスト + 罠 #31 恒久対策**。
GUI Catalog タブで FMI Cross Check の 50 モデル表示 → Download → File タブ切替まで**実 URL で実証済み**。

### 追加ファイル配置

| パス | 役割 | tests |
|------|------|-------|
| `tools/build_cross_check_catalog.py` | FMI Cross Check から `.fmu` を抽出（`git ls-tree` 方式、API 不要） | 6 |
| `tools/fmu_catalog_builder.py` | 汎用 GitHub リポジトリから `.fmu` を抽出（Trees API + Contents フォールバック） | 7 |
| `tools/setup_github_token.py` | GUI ダイアログでトークン入力 → `~/.powersimbuilder/github_token` に保存 | - |
| `tools/verify_catalog_gui.ps1` | カタログ疎通レポート（HEAD チェック + レポート出力） | - |
| `tests/python/tools/test_build_cross_check_catalog.py` | ls-tree 方式のパス解析・重複排除 | - |
| `tests/python/tools/test_fmu_catalog_builder.py` | Trees API 方式の URL 生成 | - |
| `Resources/generated_catalog.json` | 生成物（**gitignore**、`build_cross_check_catalog.py` で再生成） | - |
| `user_physics_models/` | GUI の Download 先（**gitignore**、ユーザーデータ） | - |

### ツール使い分け

| ツール | 用途 | API/レート制限 | 推奨 |
|--------|------|---------------|------|
| `build_cross_check_catalog.py` | FMI Cross Check 専用 | **なし**（git clone --filter=blob:none） | ✅ 標準 |
| `fmu_catalog_builder.py` | 任意の GitHub リポジトリ | あり（60 req/h / token で 5000 req/h） | 汎用 |

### 実証結果（2026-10-24）

| 項目 | 結果 |
|------|------|
| `.fmu` 検出（`modelica/fmi-cross-check`） | **1638 件** |
| 生成カタログ | **50 モデル**（FMI 1.0 / c-code + win64） |
| URL HEAD チェック | **3/3 OK** |
| GUI Catalog タブ | **50 models 表示** |
| Download → NetworkAccessDialog | **OK** |
| File タブ自動切替 | **OK** |

### 罠 #31 恒久対策

- `pyproject.toml` の `[tool.pytest.ini_options]` に `--basetemp=tmp/pytest` を追加
- `addopts = "-ra -q --basetemp=tmp/pytest"` に統合（既存設定は保持）
- 効果: `PermissionError: [WinError 5] pytest-current` が消え、`39 passed in 0.38s` で安定

### 次の課題（v1.6.0）

| # | 内容 | 規模 |
|---|------|------|
| 1 | `release.py` に `build_cross_check_catalog.py` 呼び出し追加 → Release asset として `generated_catalog.json` を自動添付 | S |
| 2 | `catalog_sources.json` に Release asset URL 登録（`releases/latest/download/generated_catalog.json`） | S |
| 3 | Catalog タブに Sources ドロップダウン実装 | M |
| 4 | FMI 2.0 / win64 優先ソート（現在は FMI 1.0 が先頭） | S |

**方針**: 別ツール維持 + リリース経由配布（Option C）。アプリ本体にカタログ生成ロジックは持たせない（git 依存回避）。

**タグ**: v1.5.2 はタグ未作成（v1.6.0 と同時リリース予定）

## v1.5.1 完了（catalog_probe.py + catalog_sources.json）

**手動 CLI プローブ + ホワイトリスト型ソースレジストリ**を追加。アプリ起動時の自動通信は**しない**（企業プロキシ配慮）。

### 追加ファイル

| ファイル | 役割 |
|---------|------|
| `Resources/catalog_sources.json` | ホワイトリスト型ソースレジストリ（local / github / ghproxy） |
| `tools/catalog_probe.py` | 手動 CLI プローブ（stdlib のみ、GUI 非依存） |
| `tests/python/tools/test_catalog_probe.py` | 7 tests（urllib モック、オフライン実行可） |

### 実測結果（2026-10-24）

| source | 結果 | 備考 |
|--------|------|------|
| `local-demo` | [NG] | HTTP サーバー未起動（正常） |
| `github-reference-fmus` | [OK] 200 / 0 model(s) | Reference-FMUs は zip のみ |
| `ghproxy-reference-fmus` | [OK] 200 / 0 model(s) | CN フォールバック、この環境では生存 |

**2/3 reachable** — 到達性と内容有無を分離表示できることを実証。

### 次の課題（v1.6.0）

- Reference-FMUs の zip 問題 → `FmuDownloader` zip 展開対応（解決策 B）
- Catalog タブ Sources ドロップダウン（解決策 D）

## v1.5.0 完了（MVP-γ Catalog タブ）

**JSON カタログ / GitHub リポジトリの自動判別 → モデル一覧 → DL** が完成。

### Phase 1: ロジック層（`81c5bb2`）

| クラス | 役割 | tests |
|--------|------|-------|
| `PhysicsCatalogRegistry` | JSON カタログパーサー + URL 取得 | 8 |
| `GitHubReleaseClient` | GitHub Releases API 統合（.fmu asset 抽出） | 8 |
| `PhysicsCatalogSource` | URL からソース種別を自動判別 | 10 |
| `PhysicsCatalogModel` | `CatalogItem` / `CatalogLoadResult` / `CatalogSourceInfo` | - |

### Phase 2: Catalog タブ UI（今回）

| 機能 | 内容 |
|------|------|
| Catalog タブ | 3 タブ目として追加（File / URL / Catalog） |
| URL 自動判別 | `.json` → JSON catalog、`github.com/...` → GitHub API |
| ListView | Name / Size / License / Description |
| エンドツーエンド | 選択 → URL タブ切替 → NetworkAccessDialog → FmuDownloader → File タブ切替 → ParseFmu |
| i18n | 18 キー × 2 言語（en / zh） |

### GUI テスト結果（v1.5.0 時点）

| テスト | 結果 |
|--------|------|
| 3 タブ表示（File / URL / Catalog） | OK |
| Catalog UI（URL / Load / ListView / Download） | OK |
| エラーケース（空 URL / 不正 URL / GitHub zip のみ） | OK |
| JSON カタログ E2E（local HTTP server） | OK |
| 中国語モード（`文件` / `URL` / `目录`） | OK |

### テスト数

| スイート | 結果 |
|---------|------|
| L2（`Category!=UI`） | **496 passed** |
| L3（FlaUI） | 5 passed |

## MVP-β Phase 1 + i18n ツール完了（v1.3.2 予定）

### Phase 1: AppSettings 拡張 + FmuDownloader（`902e72e`）

| 項目 | 内容 |
|------|------|
| `AppSettings` | `DownloadTimeoutSeconds` / `ProxyHost` / `ProxyPort` / `AllowedUrls`（+2 tests） |
| `FmuDownloader` | HTTP DL + タイムアウト + プロキシ + 進捗 + キャンセル（6 tests） |
| テスト | 454 → **462 passed**（+8） |

### i18n ツール統合（`40ffd44`）

| 項目 | 内容 |
|------|------|
| `tools/i18n_check.py` | JSON 検証 + カンマ自動修復（6 tests） |
| pre-commit | `i18n-check` を**最優先フック**として登録 + `check-json` 追加 |
| 検出・修復 | カンマ欠落、末尾カンマ（**5 回再発した罠 #24 を完全封止**） |
| 検出のみ | en/zh キー不一致、重複キー（`check-json` 経由） |

### 現在の i18n 状態

- **325 keys** in both en.json / zh.json（完全一致）
- `NetworkAccess.*` / `UrlTab.*` 17 キー追加済み（Phase 2 準備完了）
- 重複キー `Common.MsgTitleWarning`（en.json line 160）を削除

### 未完了（Phase 2b〜4）

| Step | 内容 | 状態 |
|------|------|------|
| 2a | i18n キー追加（17 keys × 2） | ✅ 完了 |
| 2b | `NetworkAccessDialog.cs` + 4 tests | ⏳ VSCode で作成待ち |
| 3 | `PhysicsImportForm` に TabControl（File + URL） | ⏳ |
| 4 | 手動確認 + v1.4.0 リリース | ⏳ |

### 次スレッドの開始手順

```powershell
# 1. 状態確認
git log --oneline -3
python tools/i18n_check.py

# 2. NetworkAccessDialog.cs を作成（VSCode）
code "parts\Physics\NetworkAccessDialog.cs"

# 3. NetworkAccessDialogTests.cs を作成（VSCode）
code "PowerSimBuilder.Tests\Physics\NetworkAccessDialogTests.cs"
```

## v1.3.1 完了（MVP-1.3 物理モデル削除 UI）

| Phase | 内容 | コミット |
|-------|------|---------|
| 1+2 | `PhysicsBlockDetector` + `PhysicsBlockRemover`（14 tests） | `eabb52a` |
| 3 | `PhysicsBlockPickerForm` + i18n 14 キー（6 tests） | `dcd999f` |
| 4 | 削除 UI 統合（右クリック + `Ctrl+Shift+P`）+ ControllerBox UX（3 パス） | `cfe1b4b` |
| Fix | `_psb_*` Role Functions 除外 + `#nullable enable` 追加 | 同 `cfe1b4b` |
| tag | v1.3.1 | - |

### 完成した物理モデルライフサイクル

| 操作 | UI | 実装 |
|------|-----|------|
| **Insert** | Add Physics → Picker → FMU 選択 | `PhysicsInsertionFlow.TryInsert` |
| **Edit** | エディタで直接編集 | - |
| **Remove** | 右クリック / `Ctrl+Shift+P` → Picker → 削除 | `PhysicsBlockRemover.Remove` |

### ControllerBox の `Add Physics` 非表示（3 パス）

| パス | 修正箇所 |
|------|---------|
| StaTable Import（新規） | `StaTableImportForm.OpenGeneratedInEditor` |
| パレット再追加 | `DesignCanvas.OpenPythonEditor` |
| XML/プロジェクトロード後ダブルクリック | `DesignCanvas.OpenPythonEditor`（同上） |

## 現在の状態（v1.3.0 時点）

| 項目 | 値 |
|------|-----|
| **HEAD** | `7f4425f`（v1.3.0 タグ後の .gitignore 更新） |
| **origin/main** | `7f4425f`（同期済み） |
| **tag v1.3.0** | `070a9ea`（Full i18n 対応） |
| tag v1.2.5 | `68a396c`（行単位編集 + release.py） |
| L2 tests | **431 passed**（+ 2 skipped） |
| L3 tests（FlaUI） | **5 passed** |
| Python tests | 13 passed（tools/release.py） |
| CI | build-and-test 緑、ui-tests は continue-on-error |

## L3 FlaUI スモークテスト完全解決（v1.2.3）

**全 3 テスト Pass 達成**（`Passed: 3, Skipped: 0`）。

| テスト | 状態 |
|--------|------|
| `LaunchApp_ShowsMainWindow` | ✅ Pass |
| `DoubleClick_OpensEditor` | ✅ Pass |
| `AddPhysics_InsertsCode` | ✅ Pass |

### 解決策: Win32 `EnumWindows` フォールバック

WinForms の `ShowDialog()` で開くモーダルダイアログは既定で `ShowInTaskbar=false` のため、**UIA3 のデスクトップツリーに現れない**（`GetDesktop().FindAllChildren()` に含まれない）。これを `EnumWindows` + `Automation.FromHandle` で解決。

```csharp
IntPtr found = IntPtr.Zero;
EnumWindows((hwnd, lParam) => {
    uint pid;
    GetWindowThreadProcessId(hwnd, out pid);
    if (pid != (uint)processId) return true;
    if (!IsWindowVisible(hwnd)) return true;
    var sb = new StringBuilder(512);
    GetWindowText(hwnd, sb, 512);
    var title = sb.ToString();
    if (!string.IsNullOrEmpty(title) && title.Contains(titleContains)) {
        found = hwnd;
        return false;
    }
    return true;
}, IntPtr.Zero);

if (found != IntPtr.Zero) {
    var el = automation.FromHandle(found);
    return el?.AsWindow();
}
```

### アプリ側の追加（テスト専用フック、既存挙動不変）

| ファイル | 追加内容 |
|---------|---------|
| `CanvasForm/MainForm.cs` | `Test_OpenEditorForInstance(instanceName)` + `--test-open-editor <name>` 引数 + `PowerSimBuilder_TestLoad.log` 出力 |
| `CanvasForm/DesignCanvas.cs` | `Test_OpenEditorForComponent(component)` |
| `CanvasForm/CanvasManager.cs` | `LoadModel` で `component.Name = component.InstanceName` |
| `CanvasForm/ComponentControl.cs` | `InstanceName` setter で `Name` + `AccessibleName` 設定、`CreateAccessibilityInstance()` override |
| `ComponentEdit/PythonEditorForm.Designer.cs` | `textEditor.Name = "TextBoxCode"` |
| `parts/Physics/PhysicsModelPickerForm.cs` | `Name = "PhysicsModelPickerForm"` + `_list.Name = "ListViewModels"` + `_btnInsert.Name = "BtnInsert"` |
| `tests/PowerSimBuilder.UITests/SmokeTests.cs` | 3 テスト（`WaitForEditor` / `WaitForWindowByTitleContaining` ヘルパー含む） |
| `tests/PowerSimBuilder.UITests/TestPaths.cs` | exe パス解決 |
| `tests/PowerSimBuilder.UITests/Fixtures/with-component.json` | FirstOrderLag 1 個のフィクスチャ |

### テストコマンド

```powershell
# 全 UI テスト
dotnet test tests\PowerSimBuilder.UITests --filter "Category=UI"
# -> Passed: 3, Skipped: 0

# Physics テスト
dotnet test --filter "FullyQualifiedName~Physics"
# -> 81 passed
```

## i18n 完全対応（v1.3.0）

英語 / 中国語の両言語で全 UI が表示される。日本語は明示的スコープ外（`DetectOsLanguage` が `ja` を返さない設計）。

| 画面 / 機能 | キー数 | 状態 |
|-------------|--------|------|
| MainForm ツールバー + Tools メニュー | 3 | 完了 |
| PhysicsModelPickerForm | 13 | 完了 |
| PhysicsImportForm | 28 | 完了 |
| PythonEditor 右クリックメニュー | 7 | 完了 |
| KeyboardShortcutsForm（F1） | 15 | 完了 |
| zh.json 翻訳漏れ修正 | 7 | 完了 |
| **合計** | **約 73** | - |

### UI 改善（同時実施）

- Picker ボタン重なり解消: `rightPanel.Width` 460 → **720**、`ClientSize` 820 → **920**
- ツールチップ追加: Import Model / Open Model Folder にホバー説明
- zh.json 翻訳漏れ修正: `EditPanel.BtnPhysics` → `添加物理` 等

### 言語切替方法

`%APPDATA%\PowerSimBuilder\settings.json` の `"language"` を `"en"` または `"zh"` に設定 → アプリ再起動。

## CI 設定（v1.2.4）

`.github/workflows/ci.yml` の構成:

| ジョブ | トリガー | 内容 | 結果 |
|--------|---------|------|------|
| `build-and-test` | push/PR | `Category!=UI`（421 passed） | OK |
| `code-quality` | push/PR | 警告エラー化ビルド | OK |
| `ui-tests` | main push のみ | `Category=UI` + `continue-on-error: true` | CI: Test 3 失敗を許容、Local: 3/3 |
| `python-test` | python-tests.yml | pytest 4 versions | OK |

### GitHub Actions Windows runner の制約

| 罠 | 内容 | 対策 |
|----|------|------|
| **#21** | Session 0（非対話セッション）で FlaUI GUI テストが不安定 | `continue-on-error: true` でワークフローを緑に保つ。ローカル 3/3 Pass で品質担保 |
| **#22** | `dorny/test-reporter` デフォルト `fail-on-error: true` | `fail-on-error: false` を明示 |
| **#23** | CI で `AddPhysics_InsertsCode` のみ失敗（Test 1/2 は Pass） | 要調査: Release build の `user_physics_models` cwd 解決 or ListView 遅延読み込み |

### ローカル実行コマンド

```powershell
# build-and-test 相当（CI デフォルト）
dotnet test --filter "Category!=UI"

# ui-tests 相当（main push のみ）
dotnet test tests\PowerSimBuilder.UITests --filter "Category=UI"
```

## 学んだ罠（累積）

| # | 罠 | 対策 |
|---|-----|------|
| 1 | `@"` verbatim 文字列が PowerShell here-string で誤解釈 | C# 内では `"..." + "..."` 連結で書く |
| 2 | 複数行アンカーの改行コード不一致 | 単一行アンカー + `Get-Nl()` で自動検出 |
| 3 | メソッド重複定義（CS0111） | パッチ前に `Select-String` で既存定義を確認 |
| 4 | pre-commit auto-fix 後の再 add 忘れ | `gcommit.ps1` 経由で必ずコミット |
| 5 | PowerShell 5.1 のエンコーディング | スクリプトは ASCII のみ |
| 6 | `@'...'@` 内の JSON 挿入 | `LastIndexOf('}')` で最終波括弧探索 |
| 7 | 既存 EditPanel に Tab が無い | 4 ボタン構成（`btnPhysics` 追加済み） |
| 8 | **FlaUI 5.0.0 は .NET Framework 専用** → net10.0 から参照で NU1701 | **FlaUI 4.0.0**（net6.0-windows 同梱）を使う |
| 9 | **メイン csproj の glob 罠**: `tests/` 追加で `**/*.cs` に拾われ CS0579 | csproj に `<Compile Remove="tests\**\*.cs" />` 等を追加 |
| 10 | **PowerShell 5.1 は式中の `if` 式非対応** | `$nl = if (...) { "a" } else { "b" }` は OK、式中は NG |
| 11 | **`ByName` vs `ByAutomationId`** | `ByName` = UIA `Name`(=WinForms `Text`)、`ByAutomationId` = `Control.Name` |
| 12 | **UserControl は UIA3 に自動露出しない** | `AccessibleName` + `CreateAccessibilityInstance` + `AccessibleRole.Pane` |
| 13 | **`Application` 名前衝突** | `FlaUI.Core.Application` 完全修飾 |
| 14 | **`dotnet new xunit` の既定 TFM は `net10.0`** | `net10.0-windows` + `UseWindowsForms` に変更 |
| 15 | **UIA3 はモーダル `ShowDialog()` を検出しない**（`ShowInTaskbar=false`） | Win32 `EnumWindows` + `GetWindowThreadProcessId` + `IsWindowVisible` + `GetWindowText` + `FromHandle` |
| 16 | **`#32770` クラスは他プロセスも含む** | `GetWindowThreadProcessId` で自プロセスに限定 |
| 17 | **`Application.OpenForms` は UI スレッドのみ** | `Timer.Tick` or UI スレッドで参照 |
| 18 | **`ShowDialog` を `OnLoad` 内で呼ぶとブロック** | `BeginInvoke` + `Thread.Sleep(500)` |
| 19 | **`[System.IO.File]::WriteAllText` は CWD が system32** | 既存ファイル: `(Resolve-Path).Path`、新規: `Join-Path $PWD.Path` |
| 20 | **HANDOFF 等の長い here-string は貼り付けで壊れる** | VSCode 等で直接編集、または小さく分割 |
| 21 | Session 0（非対話セッション）で FlaUI GUI テストが不安定（詳細は CI 設定章） | `continue-on-error: true` |
| 22 | `dorny/test-reporter` デフォルト `fail-on-error: true`（詳細は CI 設定章） | `fail-on-error: false` を明示 |
| 23 | CI で `AddPhysics_InsertsCode` のみ失敗（詳細は CI 設定章） | 要調査: Release build の cwd 解決 |
| 24 | **i18n JSON 追加時、末尾キーにカンマを付け忘れる**（**5 回再発**） | **`python tools/i18n_check.py --fix`** で自動修復。pre-commit hook が commit 時に自動実行 |
| 25 | `release.py` の pre-flight が uncommitted changes で停止 | リリース前に `git status --short` でクリーン確認 |
| 26 | `dotnet test` ソリューションレベルで `Category!=UI` を実行すると exit -1 | プロジェクト指定（`PowerSimBuilder.Tests/PowerSimBuilder.Tests.csproj`）で実行 |
| 27 | **新規 Form 追加時に I18n.T() を忘れる** | `I18nComplianceTests.T3_AllForms_UseI18nForTitle` が予算超過で検出。新規 Form は必ず `Text = I18n.T("...Title")` を設定 |
| 28 | **`dotnet build \| Select-String "error CS"` がマッチ0で exit 1** | フル出力を `Select-Object -Last` で確認、または `$LASTEXITCODE` を直接確認 |
| 29 | **ビルド成功でも起動中アプリは古い DLL のまま** | ビルド前に `Get-Process PowerSimBuilder \| Stop-Process -Force` |
| 30 | **`#nullable enable` 追加時、`ReadAllText` → `WriteAllText` で先頭1文字消失**（`using` の `u` が消えた事例） | `ReadAllLines` / `WriteAllLines` で行単位操作 |
| 31 | **Windows で pytest cleanup が `PermissionError`**（`pytest-of-user\pytest-current` がロック） | `--basetemp=tmp/pytest` でプロジェクト内 temp を使用。pre-commit 設定は対応済み |
| 32 | **`check-json` が重複キーを検出**（Python `json.load` は後勝ちで見逃す） | 重複キーは手動削除。`Common.MsgTitleWarning` 事例あり |
| 33 | **PowerShell パッチのインデント想定が外れる**（4 スペース vs 8 スペース） | 行番号ベース（`ReadAllLines` + brace depth 追跡）、または `Get-Content` で事前に実ファイル確認 |
| 34 | **Python 3.9 未満で `tuple[int, bytes]` / `str \| None` が syntax error**（`from __future__ import annotations` では不十分） | 型ヒントは素の `tuple` / 注釈なしに後退。3.9+ 必須なら環境依存を明記 |
| 35 | **`$text.Contains('\| NN \|')` で罠 ID を判定すると、本文中の表（ファイル数・テスト数）に誤マッチ**（`\| CanvasForm/ \| 34 \|` 等） | 行頭一致 `-match '^\| NN \|'` で判定、または罠の固有文言を併用 |
| 36 | **PowerShell の `@()` 内でパイプライン式を書くと結果がフラット化**され、verify メッセージが空欄になる | パイプライン結果を一度変数に受け、`[bool]` で明示的に boolean 化してから `@()` に格納 |
| 37 | **PS 5.1 は BOM なし UTF-8 を CP932 として誤認**し、日本語文字列がクォート/括弧を壊す（`finalize_v150.ps1` / `finalize_v152_handoff.ps1` で再発） | VSCode 保存時に「Save with Encoding → UTF-8 with BOM」を選択。保存後 `[System.IO.File]::ReadAllBytes($p)[0..2]` で `EF BB BF` を確認 |
| 38 | **PS 5.1 の `>` / `Out-File` で git 出力が行損失**（346 → 304 行に減少、`-Width` 折り返しと UTF-16 変換が原因） | `git log --output=<file>` で git 自身に書かせる。または `@(git ...)` でキャプチャ → `[System.IO.File]::WriteAllLines` で絶対パス書き込み |
| 39 | **`git for-each-ref` は `--output=<file>` 非対応**（`git log` 専用オプション） | `$raw = @(git for-each-ref ...)` で変数キャプチャ → `[System.IO.File]::WriteAllLines` で書き込み |
| 40 | **`%(objectname)` は annotated tag の場合タグオブジェクトのハッシュ**（`git tag -a` で作成したタグ） | `%(*objectname)`（peeled）で実コミットを取得。lightweight は `%(if)%(*objectname)%(then)%(*objectname:short)%(else)%(objectname:short)%(end)` で分岐 |
| 41 | **公開リポジトリの `.gitignore` を後から設定しても既コミットは履歴に残る** | 機密ファイルは最初から `.gitignore`。既存追跡は `git rm --cached` + `git filter-repo`（履歴書き換え）が必要 |
| 42 | **弁護士相談文書で「実装済み」と「設計中」を混同**すると信頼性低下・特許相談の前提崩壊（例: fmpy は将来の依存、シミュレーションは未実装） | 文書作成前に `Select-String` でコードベースを実測。`fmpy` / `Simulate` / `ParseFmu` の有無を確認 |
| 43 | **同名クラスの誤検知**（`SerializableKeyValuePair` が `DictionaryFile.cs` の nested と `PalletteDataDefine.cs` の top-level に存在） | `grep '^\s+public class'` でインデント（nested vs top-level）を確認。完全修飾 `Namespace.OuterClass.InnerClass` で検索。あるいは `Type.FullName` ベースのスキャンに切替 |
| 44 | **PowerShell の関数内で `$var = ...` はローカル変数を作成**し、スクリプトスコープの同名変数を更新しない（`Try-Replace` 関数内で `$content.Replace(...)` を書いたが呼び出し元に反映されなかった） | 親スコープを更新するには `$script:var = ...`。読み取りは自動で親スコープにフォールバック（`$var` のままで OK）。関数内での代入は要確認 |
| 45 | Path.read_text() / write_text() を encoding 指定なしで呼ぶと、Windows では locale (cp932) で読み書きされ、日本語を含む HANDOFF で UnicodeDecodeError | Path.read_text(encoding='utf-8') / write_text(encoding='utf-8') を必ず明示。Python 3.15 の UTF-8 default 化までは必須 |
| 46 | Python コードを PowerShell プロンプトに直接貼ると from / if / OLD / NEW 等が ParserError になる。python -c "..." は引用符エスケープ問題も重なる | Python スクリプトは必ず .py ファイルに保存して python ファイル.py で実行。python -c の長い式は避ける |
| 47 | plt.show() は対話 backend で figure を破棄する。plt.savefig() の前に置くと空 PNG が保存される | plt.savefig() を必ず plt.show() より先に実行、または plt.show() を削除 |
| 48 | plot_config が存在しないコンポーネントを参照しても .get() で例外が出ず、白紙 PNG になる | plt.savefig() を必ず plt.show() より先に実行、または plt.show() を削除plot_config が存在しないコンポーネントを参照しても .get() で例外が出ず、白紙 PNG になる |

## 次タスク候補

### 🔴 最優先（このスレッドの残作業）

| # | タスク | 内容 |
|---|--------|------|
| 1 | **HANDOFF 更新コミット** | この内容で `HANDOFF_MVP1_2.md` を更新 |
| 2 | **v1.2.3 タグ + Release** | L3 全緑を記念 |
| 3 | **CI 設定** | `Category!=UI` をデフォルト、`Category=UI` は main push のみ |

### 🟠 中優先（MVP-1.3 候補）

| # | タスク | 規模 |
|---|--------|------|
| 4 | 物理モデル削除 UI | M |
| 5 | 既存ファイルを開いた時の `_physicsCache` 復元 | S |
| 6 | 挿入前の確認ダイアログ | S |
| 7 | Picker に検索/フィルタ | S |

### 🟡 低優先

| # | タスク |
|---|--------|
| 8 | ハイライト色の設定化 |
| 9 | `model.yaml` エディタ UI |
| 10 | `AppServices.MessageBox` のモック化（`IMessageBox`） |

### 🔴 高優先（MVP-2 / MVP-3）

| # | タスク |
|---|--------|
| 11 | `.whl` / `.zip`（Python パッケージ取り込み）→ MVP-2 |
| 12 | 非 Real 型 FMU の完全対応 → MVP-2 |
| 13 | Modelica `.mo` ソース取り込み → MVP-3 |
| 14 | サブプロセス分離（FMU 実行）→ MVP-4 |

### 次スレッド最優先: MVP-β（モデルダウンロード補助）

**v1.4.0 目標**。詳細仕様: `docs/PHYSICS_MODEL_DOWNLOAD.md` の MVP-β 章。

#### 4 フェーズ計画

| Phase | 内容 | 規模 |
|-------|------|------|
| **1** | `AppSettings` 拡張 + `FmuDownloader` + 5 tests（HTTP モック） | M |
| **2** | `NetworkAccessDialog`（ダウンロード前の明示同意）+ 3 tests | S |
| **3** | `PhysicsImportForm` に `TabControl`（File + URL）統合 | M |
| **4** | i18n キー追加 + 手動確認 + v1.4.0 リリース | S |

#### MVP-β スコープ（中国企業環境への配慮）

| 機能 | MVP-β | MVP-γ |
|------|-------|-------|
| URL 入力 | OK | - |
| ネットワーク確認ダイアログ | OK | - |
| タイムアウト設定 | OK | - |
| プロキシ設定（手動） | OK | - |
| コミュニティミラー | - | OK |
| カタログ（同梱 JSON） | - | OK |
| 企業プロキシ自動検出 | - | OK |

#### ダウンロード前の必須 UI（コンプライアンス）

- **全 URL ダウンロードで `NetworkAccessDialog` を必ず表示**
- 「常に許可」チェック時のみ `AppSettings.AllowedUrls` に追加
- 設定画面で `AllowedUrls` を個別削除可能
- レジストリに `sha256` がある場合、検証失敗で中止

### MVP-β / MVP-γ（v1.3.0 で計画化）

| フェーズ | 内容 | 規模 |
|---------|------|------|
| **MVP-β**（v1.4.0） | PhysicsImportForm に TabControl（File + URL）追加 + NetworkAccessDialog | M |
| **MVP-γ**（v1.5.0） | Catalog タブ + DownloadSourceSettingsForm + ミラー/プロキシ設定 | M |

設計方針: GitHub 公式を優先、企業プロキシ / コミュニティミラーはオプション。中国企業環境では Session 0 / ネットワークポリシー考慮。

### 新規問題（別スレッド予定）: プロジェクト JSON 保存欠落

v1.3.1 検証中に発見。MVP-β / MVP-γ 完了後に対処予定。

#### 症状

- プロジェクト保存（`system.json` 等）に **ControllerBox** や **物理モデル** の情報が保存されない
- 保存 → 再オープンで情報欠落の可能性

#### 調査対象

| # | 対象 | ファイル |
|---|------|---------|
| 1 | `SaveModel` / `LoadModel` | `CanvasForm/CanvasManager.cs` |
| 2 | `ComponentModel.Descriptor` シリアライズ | 同上 |
| 3 | `ComponentDescriptor.PhysicsModelPath` / `FmiType` の `[JsonIgnore]` 有無 | `CanvasForm/PalletteDataDefine.cs` |
| 4 | `RepairGeneratedPaths` の往復 | `CanvasForm/MainForm.cs` |
| 5 | `PointJsonConverter` の往復 | `CanvasForm/PointJsonConverter.cs` |

#### 再現手順（次スレッドで実施）

1. ControllerBox を配置 → 保存 → JSON を開いて `generated_path` を確認
2. 物理モデル挿入 → 保存 → JSON を開いて `PhysicsModelPath` / マーカー情報を確認
3. 各ケースで欠落フィールドを特定

## スコープ外（当面やらない）

- 物理モデルの依存関係自動解決
- コメントの日本語化
- 挿入位置のユーザー選択
- `ModelExchange` 対応
- PythonEditorForm 以外の挿入先

## 開始手順（次スレッド）

```powershell
cd C:\dev\PowerSimBuilder

# 1. 状態確認
git status --short
git log --oneline -5
git tag -l "v1.2.*"

# 2. 全テスト実行
dotnet test --filter "FullyQualifiedName~Physics"
dotnet test tests\PowerSimBuilder.UITests --filter "Category=UI"

# 3. GUI 起動
Start-Process .\bin\Debug\net10.0-windows\PowerSimBuilder.exe
```

## 開始プロンプト（次スレッド用）

```
PowerSimBuilder v1.2.3（L3 FlaUI 全緑）の続きです。
HANDOFF_MVP1_2.md を読んで、次タスクに着手してください。

現状:
- L3 FlaUI スモークテスト 3/3 Pass（v1.2.3 予定）
- 解決策: Win32 EnumWindows + FromHandle（ShowInTaskbar=false 対策）
- Physics tests: 81 passed
- L3 UI tests: 3 passed

次タスク候補（優先度順）:
1. HANDOFF 更新コミット（この内容を反映）
2. v1.2.3 タグ + GitHub Release
3. CI 設定: Category!=UI デフォルト、Category=UI は main push のみ
4. MVP-1.3: 物理モデル削除 UI、既存ファイル _physicsCache 復元

パッチ運用の注意:
- PowerShell 5.1 では式中に if を書かない
- FlaUI は 4.0.0（5.0.0 は .NET Framework 専用）
- 長い here-string は貼り付けで壊れる → VSCode 直接編集推奨
- .NET 静的メソッドは絶対パス必須（Resolve-Path / Join-Path）

まず HANDOFF 更新コミット → v1.2.3 タグ付けから着手してください。
```

---

## 付録G: i18n JSON 検証ツール (tools/i18n_check.py)

**目的**: i18n JSON のカンマ問題を **commit 時に自動修復** し、罠 #24 の再発を完全に封止する。

### 機能

| モード | 動作 |
|--------|------|
| （なし） | 検証のみ。不正なら exit 1 |
| `--fix` | カンマ欠落・末尾カンマを自動修復 → 再検証 |
| `--quiet` | 成功時の出力を抑制 |

### 検出・修復可能

- カンマ欠落（`"key": "value"\n"next": ...`）
- 末尾カンマ（`"key": "value",\n}`）

### 検出のみ

- en/zh キー集合の不一致
- 構文エラー全般（line:col 付き）

### 使い方

```powershell
python tools/i18n_check.py              # 検証
python tools/i18n_check.py --fix        # 自動修復
pre-commit run i18n-check --all-files   # pre-commit 経由
```

### テスト

`tests/python/tools/test_i18n_check.py`（6 tests）

### 関連コミット

- `40ffd44` feat(tools): add i18n_check.py + pre-commit hook

## 付録H: Catalog テスト用 JSON サンプル

ローカル HTTP サーバーで Catalog タブの E2E テストを行うためのサンプル。

### 1. HTTP サーバー起動

```powershell
cd C:\dev\PowerSimBuilder\tmp\Reference-FMUs\2.0
python -m http.server 8765
```

### 2. test_catalog.json 配置

`tmp\Reference-FMUs\2.0\test_catalog.json`:

```json
{
  "version": 1,
  "models": [
    { "name": "Dahlquist", "url": "http://localhost:8765/Dahlquist.fmu", "sizeBytes": 184603, "license": "BSD-2-Clause", "description": "Simple test FMU" },
    { "name": "Feedthrough", "url": "http://localhost:8765/Feedthrough.fmu", "sizeBytes": 191904, "license": "BSD-2-Clause" },
    { "name": "VanDerPol", "url": "http://localhost:8765/VanDerPol.fmu", "sizeBytes": 192300, "license": "BSD-2-Clause" }
  ]
}
```

### 3. アプリ操作

1. Import Physics Model -> **Catalog** タブ
2. URL: `http://localhost:8765/test_catalog.json` -> **Load**
3. 3 models 表示 -> 選択 -> **Download**
4. URL タブ -> NetworkAccessDialog -> Allow -> 完了後 File タブへ自動切替

### 4. GitHub URL の注意

`https://github.com/modelica/Reference-FMUs` は **zip のみ配布**のため、Catalog タブで **0 models**（正常動作）。GitHub Releases に個別 `.fmu` asset がある repo のみ一覧表示されます。

*Apache 2.0. Windows-only. Companion to StaTable.*

## 付録F: リリースツール (tools/release.py, v1.2.5 で追加)

`tools/release.py` = **タグ付け + リリースノート生成 + GitHub Release** を 1 コマンドで実行。

### 使い方

```powershell
# 明示バージョン指定
python tools\release.py v1.2.6

# 最新タグから自動 bump (patch / minor / major)
python tools\release.py --auto minor

# 追加ノートをファイルから読み込み
python tools\release.py v1.2.6 --notes RELEASE_NOTES.md

# Dry run (変更せず計画表示)
python tools\release.py v1.2.6 --dry-run
```

### 動作 (5 ステップ)

| ステップ | 内容 |
|---------|------|
| 1. version | 引数 or 最新タグから自動 bump (既定: patch) |
| 2. pre-flight | uncommitted 変更なし / main ブランチ / origin/main と同期 を検証 |
| 3. notes | `<last-tag>..HEAD` のコミットを Conventional Commits で分類 |
| 4. tag | `git tag -a <version>` + `git push origin <version>` |
| 5. release | `gh release create --notes-file <temp>` |

### Conventional Commits 分類

| prefix | セクション |
|--------|-----------|
| `feat` | Features |
| `fix` | Bug Fixes |
| `docs` | Documentation |
| `test` | Tests |
| `ci` | CI |
| `refactor` | Refactoring |
| `perf` | Performance |
| `style` / `build` / `chore` | そのまま |
| その他 | Other |

### オプション

| オプション | 内容 |
|-----------|------|
| `--auto {major,minor,patch}` | version 省略時の bump 種別 (既定: patch) |
| `--notes <file>` | 追加の Markdown ノート |
| `--title <str>` | Release タイトル (既定: version) |
| `--dry-run` | 変更せず計画表示のみ |
| `--skip-checks` | pre-flight をスキップ (自動化用) |

### 注意事項

- **既存タグは上書きしない** (エラーで停止)
- **`--dry-run` 推奨** (実行前に計画確認)
- **notes の末尾**: `_Released on YYYY-MM-DD_` が自動付与
- **`gh release create` にはタグが origin に必要** (`push_tag` が先に実行される)
- **テスト**: `tests/python/tools/test_release.py` (13 tests)

### リリースフロー (推奨)

```powershell
# 1. 開発 → テスト → コミット → push
git push origin main

# 2. dry-run で確認
python tools\release.py --auto patch --dry-run

# 3. 実行 (タグ + Release が一発で完了)
python tools\release.py --auto patch
```
