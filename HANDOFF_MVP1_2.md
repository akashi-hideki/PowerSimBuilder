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

*Apache 2.0. Windows-only. Companion to StaTable.*
