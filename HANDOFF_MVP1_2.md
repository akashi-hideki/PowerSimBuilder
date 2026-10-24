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
