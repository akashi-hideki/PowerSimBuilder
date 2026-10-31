# THREAD HANDOFF — PowerSimBuilder 現状スナップショット

> **用途**: 新スレッド開始時にこのファイル 1 つ読めば即座に作業再開できる「現在の状態」を記録する。
> **更新**: スレッド移動時に上書き。
> **累積履歴**: `HANDOFF_MVP1_2.md`（バージョン別・罠 #1〜#51）を参照。
> **最終更新**: 2026-10-31 (UI fixes committed `3a2428d`)

---

## 📍 TL;DR

| 項目 | 値 |
|------|-----|
| **origin/main** | `3a2428d` |
| **最新タグ** | `v1.7.0` |
| **C# テスト** | 496 + 5（E2E）= 501 passed |
| **Python テスト** | 53 passed |
| **次回着手** | 公開デモプロジェクト生成 or HANDOFF 更新 |
| **公開予定** | **11/15（土）**（11/08 GO/NO-GO 判定） |

---

## 🎯 2026-10-31 の UI 修正（本日完了）

GUI ワークフローの全問題を解決:

| 問題 | 修正 | コミット |
|------|------|---------|
| Create Simulation の 2 段階ダイアログ | `BuildPythonSimulationAutoSave` で自動保存化 | `3a2428d` |
| PythonEditorForm がモーダルで MainForm ブロック | `ShowDialog → Show`（非モーダル） | `3a2428d` |
| Run でグラフウィンドウが出ない | `ForceHeadlessBackend`（`MPLBACKEND` を `SilentMode` から分離） | `3a2428d` |
| Run の完了フィードバックなし | `Completed` イベント + `lblStatus` 更新 | `3a2428d` |
| **Save 2 回目がグレーで押せない** | **`Save As...` ボタン追加（常時有効）** | `3a2428d` |

### GUI 完全ワークフロー（実証済み）

```
Ctrl+N → PID + FOL 配置 → 2配線 → setpoint=1
  → Create Simulation（非モーダル、即開く）
  → btnRun（Running... → matplotlib グラフウィンドウ表示）
  → Save（1回目、ダイアログ）
  → Save As...（2回目、名前変更保存）
```

---

## 🐛 追加された罠（#50, #51）

| # | 罠 | 対策 |
|---|-----|------|
| **#50** | `ShowDialog` は親 Form をモーダル無効化する → 他ボタン（Save 等）が押せない | 非モーダルで開くなら `Show(owner)`。`FormClosed` で `owner.Activate()` |
| **#51** | `MPLBACKEND=Agg` を `SilentMode` と連動させると GUI でもグラフウィンドウが消える（no-op） | `ForceHeadlessBackend` を分離。GUI は backend 指定なし（対話）、xUnit は `Agg` |

---

## 🔧 追加/変更された API

### PythonExecutor.cs

| メンバー | 用途 |
|---------|------|
| `SilentMode` | MessageBox 抑制 |
| `ForceHeadlessBackend` | `MPLBACKEND=Agg` 強制（xUnit/CLI 専用） |
| `WaitForExit(ms)` | プロセス終了待ち |
| `LastOutput` | stdout+stderr キャプチャ |
| `Completed` イベント | `Action<bool, int, string>` — 成功/exitCode/stderr |

### MainForm.cs

| メソッド | 用途 |
|---------|------|
| `BuildPythonSimulationAutoSave(manager)` | `generated/demo_main.py` 自動保存 |
| `saveButton_Click` | IsModified 時のみ有効 |
| `saveAsButton_Click` | 常時有効、名前変更保存 |

### PythonEditorForm.cs

| 変更 | 内容 |
|------|------|
| `BtnRun_Click` | `lblStatus = "Running..."` 即更新 |
| `Executor` property | `Completed` イベント購読 |
| `OnExecutorCompleted` | `lblStatus` 更新（`BeginInvoke`） |

### MainForm.Designer.cs

| 追加 | 内容 |
|------|------|
| `saveAsButton` | `ToolStripButton`、`Save` の右隣、常時有効 |

---

## 📋 次タスク（優先度順）

| 優先 | タスク | 規模 |
|:---:|--------|:---:|
| 🔴 | HANDOFF 更新（本日の UI 修正 + 罠 #50/#51） | S |
| 🔴 | 公開デモプロジェクト生成（`pid_closed_loop.psbproj`） | S |
| 🟠 | グラフ波形の目視確認（PID 応答が正しいか） | S |
| 🟠 | `generated/demo_oven.py` の追跡整理（`.gitignore` 確認） | S |
| 🟡 | 品質保証（E2E 全シナリオ、中国語 UI） | M |
| 🟡 | 告知文面ドラフト（WeChat、中国語 + 英語併記） | M |

---

## 🌅 次スレッド開始手順

```powershell
cd C:\dev\PowerSimBuilder
git status --short
git log --oneline -3
dotnet build
dotnet test tests\PowerSimBuilder.UITests --filter "FullyQualifiedName~ProjectRoundTripTests"
```

**期待**: clean, 5 passed, 0 errors。

---

## 📚 参考資料

| ファイル | 用途 |
|---------|------|
| `HANDOFF_MVP1_2.md` | 累積履歴（バージョン別・罠 #1〜#51） |
| `examples/demos/` | デモ 2 種（PID standalone + StaTable runtime） |
| `examples/demos/README.md` | デモ説明 |
| `tmp/fmu_wrapper.py` | FMPy 経由 FMU 実行 |
| `tests/PowerSimBuilder.UITests/ProjectRoundTripTests.cs` | 完全 E2E 5 テスト |

---

*最終更新: 2026-10-31 / UI fixes committed `3a2428d`*
