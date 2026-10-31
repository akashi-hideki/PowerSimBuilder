# THREAD HANDOFF — PowerSimBuilder 現状スナップショット

> **用途**: 新スレッド開始時にこのファイル 1 つ読めば即座に作業再開できる「現在の状態」を記録する。
> **累積履歴**: `HANDOFF_MVP1_2.md`（バージョン別・罠 #1〜#52）を参照。
> **最終更新**: 2026-10-31（デモプロジェクト完成）

---

## 📍 TL;DR

| 項目 | 値 |
|------|-----|
| **origin/main** | `159395a` |
| **最新タグ** | `v1.7.0` |
| **C# テスト** | 501 + 5（E2E）= 506 passed |
| **Python テスト** | 53 passed |
| **公開予定** | **11/15（土）**（11/08 GO/NO-GO 判定） |

---

## 🎯 2026-10-31 の成果

### デモプロジェクト完成（最終戦クローズ）

| 成果 | コミット | 内容 |
|------|---------|------|
| `pid_closed_loop.psbproj` 配置 | `5dc275e` | `examples/demos/` に Git 追跡下で配置（88,609 bytes） |
| README セクション 3 追記 | `159395a` | 再現手順 + 期待波形 + 中身検証（31 insertions） |
| GUI 波形確認 | — | setpoint=1 収束、オーバーシュート小、定常偏差なし |

### 罠の連番整理

| # | 罠 | コミット |
|---|-----|---------|
| #50 | ShowDialog のモーダル無効化 | `0ed6454` |
| #51 | MPLBACKEND=Agg と SilentMode の分離 | `0ed6454` |
| #52 | psbproj descriptor=PascalCase / connections=camelCase | `b48dfd2` |

### ツール追加

- `tools/status_check.ps1`（`a310dba`）: セッション開始時 status dashboard（UTF-8 BOM、罠 #37 対策）

---

## 🌅 次スレッド開始手順

起動コマンド:

    cd C:\dev\PowerSimBuilder
    powershell -ExecutionPolicy Bypass -File tools\status_check.ps1

期待: HEAD=`159395a`、working tree clean、E2E 5 passed、Section 4 に `pid_closed_loop.psbproj`。

---

## 📋 次タスク（優先度順）

| 優先 | タスク | 期日 |
|:---:|---|:---:|
| 🔴 | 11/15 公開準備（README / 告知文面 / 品質保証） | 11/08 GO/NO-GO |
| 🟠 | 品質保証（E2E 全シナリオ、中国語 UI） | 11/03〜11/08 |
| 🟡 | 告知文面ドラフト（WeChat、中英併記） | — |

---

## 📚 参考資料

| ファイル | 用途 |
|---------|------|
| `HANDOFF_MVP1_2.md` | 累積履歴（罠 #1〜#52） |
| `examples/demos/` | デモ 3 種（PID standalone + StaTable runtime + PID closed loop .psbproj） |
| `examples/demos/README.md` | デモ説明（セクション 3 = PID 閉ループ） |
| `tools/status_check.ps1` | セッション開始時 status dashboard |
| `tmp/_psbproj_read.py` | psbproj 標準リーダー（罠 #52 対策） |
| `tests/PowerSimBuilder.UITests/ProjectRoundTripTests.cs` | 完全 E2E 5 テスト |

---

*最終更新: 2026-10-31 / デモプロジェクト完成（`159395a`）*
