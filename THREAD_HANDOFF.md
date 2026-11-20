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
| 115 | パッチ適用後 `dotnet build` を忘れ、旧バイナリで GUI 検証して「変更が反映されない」と誤認（バイナリ `8:17:24` / ソース `8:28:34` のタイムスタンプ乖離で判明、解決に 30 分） | パッチ後は必ず `dotnet build` → `Get-Item .\bin\Debug\net10.0-windows\PowerSimBuilder.exe \| Select LastWriteTime` で時刻確認 → GUI 起動 |
| 116 | `PortSelectorControl.ItemCheck` 内の `BeginInvoke` が handle 未生成時（`ShowDialog` 前の `SetItemChecked`）に `InvalidOperationException` を投げ、`catch` で握り潰されて無音で PythonEditor へ進む | `if (IsHandleCreated)` ガードで解決（`patch_p2a_fix.py`）。catch は `MessageBox.Show` に変更し可視化（文言整理は v1.10.2 で） |
| 117 | SPEC 記載（`parts/StaTableImportForm.cs`）と実体（`StaTableIntegration/StaTableImportForm.cs`）の乖離、および「XML パース直後にポート選択」の記述が実装（Save → 選択）と不整合 | SPEC 修正は v1.10.2 リリース時にまとめて対応 |
| 0 | **P2b: MainForm 統合** — `CanvasForm\MainForm.cs:1695` の `Build(...)` に `SelectedInputPorts` / `SelectedOutputPorts` を渡す | S (30m) | 即時 |
## 🎉 P2 完了（2026-11-21）

| Phase | 状態 | コミット |
|---|---|---|
| P1 PortSelectorControl | ✅ | de114d9 |
| P4 Build 拡張 | ✅ | 6a10b76 |
| P2a ダイアログ統合 | ✅ | de114d9 |
| P2a fix（BeginInvoke ガード） | ✅ | add2582 |
| **P2b MainForm 統合** | ✅ | `<新ハッシュ>` |

*最終更新: 2026-11-21 / v1.10.1 public 公開完了 + New Project コミット済み（15786bf）+ ポート選択 SPEC コミット済み*
