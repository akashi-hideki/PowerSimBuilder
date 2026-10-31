# THREAD HANDOFF — PowerSimBuilder 現状スナップショット

> **用途**: 新スレッド開始時にこのファイル 1 つ読めば即座に作業再開できる「現在の状態」を記録する。
> **累積履歴**: `HANDOFF_MVP1_2.md`（バージョン別・罠 #1〜#55）を参照。
> **最終更新**: 2026-11-03（QA Day 1-2 完了、OvenChamber + 回帰ガード）

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

## 🎯 2026-11-03 の成果（QA Day 1-2）

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

### QA Day 2: UI/UX

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
| `tests/PowerSimBuilder.UITests/ProjectRoundTripTests.cs` | E2E 5 テスト |

---

*最終更新: 2026-11-03 / QA Day 1-2 完了（`f9a9653`）*
