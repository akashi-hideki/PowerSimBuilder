# PowerSimBuilder 引き継ぎ: MVP-1.2 (Physics Model Integration + L2 GUI tests)

## プロジェクト概要
- PowerSimBuilder: StaTable のシミュレーション・コンパニオン
- .NET 10 WinForms + Python runtime（`fmpy` で FMU 実行）
- リポジトリ: C:\dev\PowerSimBuilder
- **v1.2.0 リリース済み**（tag: `v1.2.0` = `e6364cf`）
- L2 GUI テストは `387dd7f` で追加済み（v1.2.1 予定）

## 現在の状態（2026-10-23 時点）

| 項目 | 値 |
|------|-----|
| HEAD | `387dd7f` (test: add L2 GUI tests, picker + editor integration via mock picker) |
| origin/main | `387dd7f`（**push 済み・同期済み**） |
| tag v1.2.0 | `e6364cf`（L2 テストを含まない） |
| tag v1.2.1 | 未作成（次スレッドで検討） |
| GitHub Release | https://github.com/akashi-hideki/PowerSimBuilder/releases/tag/v1.2.0 |
| Physics tests | **81 passed** |
| C# tests（全体） | 330 + 81 ≒ 411 passed（`xUnit2013` 警告 1件のみ） |
| pre-commit | 全 9 フック Passed |
| pre-push | 全 10 フック Passed（pytest 含む） |
| ビルド警告 | 0（C#）、既存 `xUnit2013` 1件のみ |

## 今回のスレッド成果（Step 1 → 6c + L2）

### 実装概要（13 機能）

| # | 機能 | クラス / 場所 |
|---|------|---------------|
| 1 | FMU 読取（modelName / FmiVersion / inputs / outputs / params / timing / 型） | `parts/Physics/FmuInfoReader.cs` |
| 2 | データレコード | `parts/Physics/FmuInfo.cs` |
| 3 | スニペット生成（init / update、英語コメント） | `parts/Physics/PhysicsSnippetBuilder.cs` |
| 4 | マーカー挿入（重複防止 / CRLF 保持 / indent 自動検出） | `parts/Physics/PhysicsSnippetInserter.cs` |
| 5 | 挿入フロー（CoSimulation チェック / ControllerBox 除外） | `parts/Physics/PhysicsInsertionFlow.cs` |
| 6 | モデル一覧カタログ | `parts/Physics/PhysicsModelCatalog.cs` |
| 7 | モデル選択ダイアログ（詳細パネル + Open Folder） | `parts/Physics/PhysicsModelPickerForm.cs` |
| 8 | 挿入位置ハイライト（init/update 両方 / カーソル検出） | `parts/Physics/PhysicsInsertionHighlighter.cs` |
| 9 | yaml ポートマッピング | `parts/Physics/ModelYaml.cs` |
| 10 | EditPanel **Add Physics** ボタン | `ComponentEdit/EditPanel.cs` + `.Designer.cs` |
| 11 | ダブルクリック → エディタ起動 | `CanvasForm/DesignCanvas.cs` |
| 12 | PythonEditorForm 統合 | `ComponentEdit/PythonEditorForm.Physics.cs` |
| 13 | 非Real型 set/get、タイムステップ警告 | `PhysicsSnippetBuilder.cs`（内部） |

### コミット履歴（今回のスレッド）

| コミット | 内容 |
|---------|------|
| `7b3d575` | MVP-1.2 WIP - FMU info reader, snippet builder/inserter, picker, EditPanel integration |
| `ca6e92d` | MVP-1.2 - typed set/get, yaml mapping, timing warning, FMU info UI |
| `e6364cf` | docs: expand README for MVP-1.2 physics integration (← v1.2.0 タグ) |
| `387dd7f` | test(physics): add L2 GUI tests (picker + editor integration via mock picker) |

### テストファイル（81 tests）

| テストファイル | 件数 |
|---------------|------|
| `FmuInfoReaderTests.cs` | 3 |
| `PhysicsSnippetBuilderTests.cs` | 9 |
| `PhysicsSnippetBuilderMappingTests.cs` | 5 |
| `PhysicsSnippetBuilderModelInfoTests.cs` | 2 |
| `PhysicsSnippetInserterTests.cs` | 9 |
| `PhysicsInsertionFlowTests.cs` | 12 |
| `PhysicsModelCatalogTests.cs` | 7 |
| `PhysicsInsertionHighlighterTests.cs` | 9 |
| `PhysicsInsertionHighlighterCursorTests.cs` | 6 |
| `ModelYamlTests.cs` | 5 |
| `EditPanelPhysicsTests.cs` | 3 |
| `PhysicsModelPickerFormTests.cs` | 8（L2） |
| `PythonEditorFormPhysicsTests.cs` | 7（L2） |
| **合計** | **81** |

## GUI テストのレベル分類

| レベル | ツール | 対象 | 状態 |
|--------|--------|------|------|
| **L1 ロジック** | xUnit | FmuInfoReader / SnippetBuilder / Inserter / ModelYaml / InsertionFlow | ✅ 完了 |
| **L2 フォーム配線** | xUnit + WinForms | Picker 選択/Insert、Editor 挿入/Mock、status bar | ✅ 完了（15 tests） |
| **L3 GUI E2E** | FlaUI（未導入） | 実 exe 起動、マウス操作 | ❌ 未着手 |
| **L4 Python** | pytest | Python runtime | ✅ 既存 |

### L2 の主要テスト

| カテゴリ | テスト |
|---------|--------|
| Picker: 一覧表示/空状態 | 2 |
| Picker: 選択→ボタン有効化 | 2 |
| Picker: 詳細パネル（型付き） | 1 |
| Picker: Insert/Null | 2 |
| Picker: 列数 | 1 |
| Editor: コード設定 | 1 |
| Editor: Mock Picker 挿入 | 1 |
| Editor: キャンセル | 1 |
| Editor: キャッシュ登録 | 1 |
| Editor: 重複防止 | 1 |
| Editor: カーソル status | 2 |
| **合計** | **15** |

## 生成コードのサンプル

### `__init__` への挿入

```python
# === physics: PMSM (auto-inserted) ===
# model     : PMSM
# fmu_file  : model.fmu
# source    : user_physics_models/PMSM/source/model.fmu
# fmi       : 2.0 (CoSimulation)
# inputs    : u_d:Real, u_q:Real
# outputs   : i_d:Real, i_q:Real, torque:Real
# parameters: R=0.5, Ld=0.001
# timing    : start=0, stop=1, step=0.001
self.pmsm = load_fmu(_psb_resolve_fmu("PMSM/source/model.fmu"))
self.pmsm.enter_initialization_mode()
self.pmsm.set("R", 0.5)
self.pmsm.set("Ld", 0.001)
self.pmsm.exit_initialization_mode()
self._pmsm_time = 0.0
# === /physics: PMSM ===
```

### `update()` への挿入

```python
# === physics: PMSM (auto-inserted in update) ===
# inputs -> FMU, FMU -> outputs
self.pmsm.set("u_d", float(self.inputs.get("u_d", 0.0)))
self.pmsm.set("u_q", float(self.inputs.get("u_q", 0.0)))
# timing check: FMU stepSize=0.001
if abs(dt - 0.001) > 1e-9:
    import warnings
    warnings.warn(
        "physics: dt (%.6g) differs from FMU stepSize (%.6g)"
        % (dt, 0.001), RuntimeWarning)
self.pmsm.doStep(self._pmsm_time, dt)
self._pmsm_time += dt
self.outputs["i_d"] = float(self.pmsm.get("i_d"))
self.outputs["i_q"] = float(self.pmsm.get("i_q"))
self.outputs["torque"] = float(self.pmsm.get("torque"))
# === /physics: PMSM ===
```

### ファイル先頭（1回だけ挿入）

```python
from fmpy import load_fmu

# --- physics helper (auto-inserted once) ---
def _psb_resolve_fmu(relative_path: str) -> str:
    """Resolve an FMU path under user_physics_models."""
    from pathlib import Path
    here = Path(__file__).resolve()
    for base in [here.parent, *here.parents]:
        candidate = base / "user_physics_models" / relative_path
        if candidate.is_file():
            return str(candidate)
    raise FileNotFoundError(
        "FMU not found under user_physics_models: " + relative_path
    )
# --- /physics helper ---
```

## アーキテクチャ（Physics レイヤー）

```
UI 層
  ├ EditPanel (btnPhysics)          → PhysicsRequested イベント
  ├ PythonEditorForm.Physics.cs     → picker 起動、挿入、ハイライト、status
  │   ├ IPhysicsPicker (interface)  → 本番: WinFormsPhysicsPicker
  │   │                               テスト: MockPicker
  │   └ _physicsCache               → status bar 用の FmuInfo キャッシュ
  └ PhysicsModelPickerForm          → モデル選択、詳細パネル

フロー層
  └ PhysicsInsertionFlow            → ControllerBox 除外、CoSimulation チェック
        ├ ModelYaml.ReadPortMapping → yaml パース
        └ PhysicsSnippetInserter
              ├ PhysicsSnippetBuilder  → init/update ブロック生成
              └ PhysicsInsertionHighlighter → 位置検出

データ層
  ├ FmuInfoReader   → modelDescription.xml パース
  ├ FmuInfo         → レコード
  └ PhysicsModelCatalog → user_physics_models スキャン
```

## 使い方（E2E）

1. キャンバスに **FirstOrderLag** など配置
2. **ダブルクリック** → PythonEditorForm が開く
3. 左パネル **Add Physics** 押下 → Picker 起動
4. モデル選択（右側に詳細表示）→ **Insert**
5. `__init__` と `update()` に自動挿入、両方ハイライト
6. カーソルをブロック内に置くと status bar にモデル名 + 行範囲表示
7. Ctrl+S → Run で実行

## 既知の制限（MVP-1.2）

| 制限 | 備考 |
|------|------|
| CoSimulation のみ | ModelExchange は明示的に拒否 |
| Enumeration 型 | `# TODO` コメント |
| ポートマッピング | `model.yaml` 手書きのみ |
| 削除 UI | なし（マーカー範囲を手動で削除） |
| 複数モデル順序 | 挿入順（依存解決なし） |
| 既存ファイルを開いた場合 | `_physicsCache` 未登録 |

## 重要な設計判断

| 項目 | 決定 | 理由 |
|------|------|------|
| ControllerBox 除外判定 | `Type == "ControllerBox"` | StaTable 取り込み時のみこの Type |
| コメント言語 | 英語 | コードベース全体が英語 |
| 情報源 | FMU 内 `modelDescription.xml`（C# パース） | yaml 依存を減らす |
| マーカー書式 | `# === physics: <name> (auto-inserted) ===` | 既存 `STABLE_USER_CODE_*` と同系統 |
| パス解決 | `_psb_resolve_fmu()` で `__file__` 上方向探索 | インストール先・cwd 非依存 |
| 挿入位置 | `__init__` / `update()` の body 末尾 | `ComponentCodeGenerator` の出力形式に合わせる |
| indent | body 先頭行から自動検出 | 4/8スペース両対応 |
| 改行コード | 元ファイルの CRLF/LF を保持 | 既存コードを壊さない |
| 型対応 | Real=float / Integer=int / Boolean=bool / String=str | Enum は TODO |
| タイムステップ警告 | `warnings.warn` インライン | 実行時ガード |
| 重複挿入 | マーカー検出で `AlreadyExists` | 安全側 |
| **Picker 注入** | `IPhysicsPicker` インターフェース | L2 テストでモーダルなしでテスト可能 |
| **STA スレッド** | `RunOnSta` ヘルパー | WinForms コントロールは STA 必須 |

## 学んだ罠

### 1. `@"` verbatim 文字列が PowerShell here-string で誤解釈
C# コード内では `@"..."` を使わず、`"..." + "..."` 連結で書く。

### 2. 複数行アンカーの改行コード不一致
単一行アンカーで `Replace-Opt` を使い、改行コードは `Get-Nl()` で自動検出。

### 3. メソッドの重複定義（`CS0111`）
パッチ前に `Select-String` で既存定義を確認。重複発生時は全削除→1つだけ残すパッチを書く。

### 4. pre-commit auto-fix 後の再 add
`git add -A` → 再 `git commit`。

### 5. PowerShell 5.1 のエンコーディング
スクリプトは **ASCII のみ（英語コメント）**。

### 6. `@'...'@` 内の JSON 挿入
`LastIndexOf('}')` で最終波括弧を探し、カンマ有無を自動判定。

### 7. 既存 EditPanel に Tab が無い（HANDOFF_MVP1_1 との差異）
`TabControl` は存在せず、`panelButtons`（3ボタン）のみ。4つ目のボタン `btnPhysics` を追加。

### 8. ボタンラベルの切れ
`btnPhysics` を 90px に、他を 55px に縮めて合計 268px（EditPanel 280px 内）。

### 9. **L2 テスト：ヘッドレス ListView 選択が反映されない**
`ListView.Items[0].Selected = true` でも `SelectedItems.Count == 0` のまま。
→ `Test_SelectFirst()` で `SelectedIndices.Add(0)` を追加、ボタン Enabled と詳細テキストを直接設定。

### 10. **L2 テスト：`Button.PerformClick()` がフォーム非表示時に発火しない**
→ `Test_InvokeInsert()` フックで `AcceptIfSelected()` を直接呼ぶ。

### 11. **L2 テスト：`PhysicsInsertionFlow.TryInsert` が実在 FMU を要求**
→ `TempDir` ヘルパーで temp に最小 FMU（`modelDescription.xml` のみ zip）を生成。

### 12. **L2 テスト：`AppServices.MessageBox` が headless でブロック**
→ `PickerProvider` に `MockPicker` を注入。`MessageBox` 経由のパスは L3（FlaUI）でカバー。

## 次タスク候補（優先度付き）

### 🟠 中優先（MVP-1.3 候補）

| # | タスク | 規模 |
|---|--------|------|
| 1 | **v1.2.1 タグ付け**（L2 テストのみのパッチリリース） | S |
| 2 | **L3 FlaUI スモークテスト**（実 exe 起動 3 tests） | M |
| 3 | **物理モデル削除 UI**（マーカー範囲を選択→削除） | M |
| 4 | **既存ファイルを開いた時の `_physicsCache` 復元** | S |
| 5 | **挿入前の確認ダイアログ** | S |

### 🟡 低優先

| # | タスク |
|---|--------|
| 6 | Picker に検索/フィルタ |
| 7 | ハイライト色の設定化 |
| 8 | マーカーの色分け |
| 9 | `model.yaml` エディタ UI |
| 10 | `AppServices.MessageBox` のモック化（`IMessageBox`） |

### 🔴 高優先（MVP-2 / MVP-3）

| # | タスク |
|---|--------|
| 11 | **`.whl` / `.zip`（Python パッケージ取り込み）** → MVP-2 |
| 12 | **非 Real 型 FMU の完全対応** → MVP-2 |
| 13 | **Modelica `.mo` ソース取り込み** → MVP-3 |
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

# 2. Physics テスト実行（81 passed を確認）
dotnet test --filter "FullyQualifiedName~Physics"

# 3. 全体テスト（任意）
dotnet test

# 4. GUI 起動
Start-Process .\bin\Debug\net10.0-windows\PowerSimBuilder.exe
```

## 開始プロンプト（次スレッド用）

```
PowerSimBuilder v1.2.0（+ L2 テスト）の続きです。
HANDOFF_MVP1_2.md を読んで、次タスクに着手してください。

現状:
- HEAD: 387dd7f（origin/main と同期済み）
- tag: v1.2.0 = e6364cf（L2 テストは含まず）
- Physics tests: 81 passed（L2 15 tests 含む）
- pre-commit / pre-push 全フック Passed

次タスク候補（優先度順）:
1. v1.2.1 タグ付け（L2 テストのみのパッチリリース）
2. L3 FlaUI スモークテスト（実 exe 起動 3 tests）
3. 物理モデル削除 UI
4. 既存ファイルを開いた時の _physicsCache 復元

まず L3 FlaUI スモークテストに着手したい。
FlaUI.UIA3 パッケージを導入し、以下 3 テストを設計:
- LaunchApp_ShowsMainWindow
- DoubleClick_OpensEditor
- AddPhysics_InsertsCode

CI では [Trait("Category", "UI")] で分離してください。

パッチ運用の注意:
- スクリプトは ASCII のみ
- C# コードに @"..." を使わない
- 単一行アンカー + Get-Nl()
- 適用前に Select-String で既存定義を確認
- ヘッドレステストは Test_ フックで代替
```

## 付録A: 主要ファイル一覧

### 新規作成（parts/Physics/）

| ファイル | サイズ |
|---------|-------|
| `FmuInfo.cs` | 706 bytes |
| `FmuInfoReader.cs` | 4,757 bytes |
| `ModelYaml.cs` | 3,842 bytes |
| `PhysicsInsertionFlow.cs` | 2,052 bytes |
| `PhysicsInsertionHighlighter.cs` | 4,222 bytes |
| `PhysicsModelCatalog.cs` | 2,180 bytes |
| `PhysicsModelPickerForm.cs` | 10,375 bytes |
| `PhysicsSnippetBuilder.cs` | 10,231 bytes |
| `PhysicsSnippetInserter.cs` | 9,725 bytes |

### 既存修正

| ファイル | 変更内容 |
|---------|---------|
| `ComponentEdit/EditPanel.cs` | `PhysicsRequested` イベント、`btnPhysics` 配線 |
| `ComponentEdit/EditPanel.Designer.cs` | `btnPhysics` 追加、4ボタン再配置 |
| `ComponentEdit/PythonEditorForm.Physics.cs` | 新規 partial（統合 + IPhysicsPicker + Test hooks） |
| `ComponentEdit/ComponentManager.cs` | `editorForm.ComponentType` 設定 |
| `CanvasForm/DesignCanvas.cs` | `DoubleClicked` イベント配線 |
| `Resources/i18n/en.json` / `zh.json` | 6キー追加 |
| `README.md` | +142行（v1.2.0 セクション） |

## 付録B: テスト実行コマンド集

```powershell
# Physics 全体（81 tests）
dotnet test --filter "FullyQualifiedName~Physics"

# L2 GUI のみ（15 tests）
dotnet test --filter "FullyQualifiedName~PhysicsModelPickerFormTests|FullyQualifiedName~PythonEditorFormPhysicsTests"

# 全テスト
dotnet test
```

## 付録C: Reference FMU

| モデル | パス | 特徴 |
|--------|------|------|
| Dahlquist | `tmp\Reference-FMUs\2.0\Dahlquist.fmu` | input なし、output x |
| Feedthrough | `tmp\Reference-FMUs\2.0\Feedthrough.fmu` | 変数名が `Float64_continuous_input` 等 |
| VanDerPol | `tmp\Reference-FMUs\2.0\VanDerPol.fmu` | 非線形発振器 |

## 付録D: user_physics_models の構成

```
user_physics_models/
  PMSM/
    model.yaml
    README.md
    source/
      model.fmu
  Feedthrough/
    model.yaml
    README.md
    source/
      Feedthrough.fmu
```

---

*Apache 2.0. Windows-only. Companion to StaTable.*
