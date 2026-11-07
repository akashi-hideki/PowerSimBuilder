# SPEC: Role Function Snippets (GUI 編集・挿入)

> **対象**: PowerSimBuilder v1.9.0 以降
> **目的**: 一般ユーザーがロール関数を GUI で編集・挿入できるようにする
> **最終更新**: 2026-11-03
> **状態**: 実装完了（B-1〜B-5 / 2026-11-07）— 詳細は §10 実装完了サマリ参照

---

## 1. 背景と課題

### 現状（開発者向け）

- StaTable 生成の `*_powersim.py` には **60 個のロール関数**が `# STABLE_USER_CODE_START/END` マーカー付きで存在
- 各関数の body は `return 0`（未実装スタブ）
- PowerSimBuilder はマーカー間のコードを **再インポート時も保持**する

### 課題

- 一般ユーザーは Python コードを書けない
- `tools/edit_role_function.py`（開発者向けスクリプト）は GUI ユーザーには使えない
- **サンプルコードを GUI から挿入・編集できる仕組み**が必要

### あるべき UX

```
PythonEditorForm（ControllerBox をダブルクリックで開く）
  → ツールバー「Role Snippets...」ボタン
  → RoleSnippetForm（一覧ダイアログ）
     - F_ReadThermistors  [サンプル: センサー読み取り]  [挿入]
     - F_SetTopHeaterDuty [サンプル: P 制御]           [挿入]
     - F_CheckOvenOverheat[サンプル: 過熱検知]          [挿入]
     - [新規作成] [編集] [削除]
  → 選択 → STABLE_USER_CODE 領域に自動挿入
  → エディタで微調整 → Save
```

---

## 2. 設計

### 2.1 スニペットストレージ

```
templates/role_snippets/
  ├── index.json                          # メタデータ（表示名・説明・層名）
  ├── MwOven.F_ReadThermistors.py        # サンプル本体
  ├── MwOven.F_SetTopHeaterDuty.py
  ├── MwOven.F_CheckOvenOverheat.py
  └── ...
```

`index.json` の例:

```json
{
  "snippets": [
    {
      "layer": "MwOven",
      "func": "F_ReadThermistors",
      "displayName": "センサー読み取り（オーブン）",
      "description": "oven_current_temp から thermistor_top/middle/bottom を計算",
      "file": "MwOven.F_ReadThermistors.py"
    }
  ]
}
```

### 2.2 UI コンポーネント: `RoleSnippetForm`

| 要素 | 機能 |
|----------|----------------------------------------|
| `ListBox lstSnippets` | スニペット一覧（`Layer.F_Func` + 表示名） |
| `TextBox txtCode` | 選択中スニペットのコード表示・編集 |
| `TextBox txtDescription` | 説明文の編集 |
| `Button btnInsert` | エディタの STABLE_USER_CODE 領域に挿入 |
| `Button btnSave` | スニペットを保存（新規/更新） |
| `Button btnDelete` | スニペット削除（確認ダイアログ） |
| `Button btnNew` | 新規スニペット作成 |
| `Button btnCancel` | 閉じる |

### 2.3 `PythonEditorForm` への統合

```csharp
// コンストラクタでツールバーに追加
var btnSnippets = new ToolStripButton
{
    Name = "btnRoleSnippets",
    Text = I18n.T("PythonEditor.BtnRoleSnippets"),  // "Role Snippets..." / "角色函数..."
    DisplayStyle = ToolStripItemDisplayStyle.Text,
};
btnSnippets.Click += (s, e) =>
{
    using var dlg = new RoleSnippetForm();
    if (dlg.ShowDialog(this) == DialogResult.OK)
    {
        InsertRoleSnippet(dlg.SelectedLayer, dlg.SelectedFunc, dlg.SelectedCode);
    }
};
panelTop.Controls.Add(btnSnippets);
```

### 2.4 挿入処理: `InsertRoleSnippet`

```csharp
private void InsertRoleSnippet(string layer, string func, string code)
{
    // 1) エディタ内で STABLE_USER_CODE_START/END を検索
    var startMarker = $"# STABLE_USER_CODE_START:{layer}.{func}";
    var endMarker = $"# STABLE_USER_CODE_END:{layer}.{func}";
    var text = textEditor.Text;
    var startIdx = text.IndexOf(startMarker);
    var endIdx = text.IndexOf(endMarker);
    if (startIdx < 0 || endIdx < 0)
    {
        MessageBox.Show($"Markers not found for {layer}.{func}");
        return;
    }

    // 2) マーカー間を code で置換（インデント 8 スペース維持）
    // ... (既存の tools/edit_role_function.py と同じロジック)

    // 3) エディタを更新
    textEditor.Text = newText;
    IsModified = true;
}
```

---

## 3. 初期サンプルスニペット

| Layer | Func | 説明 | 難易度 |
|----|------------------------------|--------------------|--------|
| MwOven | F_ReadThermistors | 温度センサー読み取り | 易 |
| MwOven | F_SetTopHeaterDuty | P 制御（ヒーター duty） | 中 |
| MwOven | F_CheckOvenOverheat | 250°C 超えで過熱発火 | 中 |
| MwOven | F_StartOvenPid | PID 制御開始（フラグ） | 易 |
| DriverInput | F_OnDoorOpen | ドアオープン検知 | 易 |
| Application | F_LogAppError | エラーログ | 易 |

**初期は 3 つ（`F_ReadThermistors`, `F_SetTopHeaterDuty`, `F_CheckOvenOverheat`）から開始**を推奨。

---

## 4. 実装フェーズ

| Phase | 内容 | 見積 | 優先度 |
|----|------------------------------|--------------------|--------|
| 1 | `RoleSnippetStorage`（読込/保存/一覧） | 1h | 🔴 |
| 2 | `RoleSnippetForm`（UI） | 2h | 🔴 |
| 3 | `PythonEditorForm` 統合 + `InsertRoleSnippet` | 30m | 🔴 |
| 4 | 初期サンプル 3 個の `.py` + `index.json` | 1h | 🟠 |
| 5 | テスト（Storage / Insertion） | 1h | 🟠 |
| 6 | i18n（en / zh） | 30m | 🟠 |

**合計見積: 5〜6h**

---

## 5. 後方互換性

| 項目 | 影響 |
|----------|----------------------------------------|
| `PythonEditorForm` の既存機能 | 変更なし（ボタン追加のみ） |
| 既存の `tools/edit_role_function.py` | そのまま利用可能（開発者向け） |
| `*_powersim.py` のマーカー形式 | 変更なし（`STABLE_USER_CODE_START/END`） |

---

## 6. リスクと対策

| リスク | 対策 |
|----------|----------------------------------------|
| ユーザーがマーカーを破壊する | 挿入時にマーカー存在確認、破壊時は警告 |
| スニペットのコードにバグ | 挿入時に Python 構文チェック（`ast.parse` 相当を C# で実装 or 挿入後に Python で検証） |
| スニペットの重複/バージョン管理 | `index.json` にバージョン番号、将来の配布パッケージ対応 |
| ユーザー固有スニペットの管理 | `templates/role_snippets/user/` サブディレクトリ（オプション） |

---

## 7. 未決事項

- スニペットの **バージョン管理**（配布物として更新可能にするか）
- **言語別スニペット**（日本語 / 英語 / 中国語コメント）
- **スニペットの検索・フィルタ**（60 個の関数から絞り込む）
- **コード補完**（`self.globals[...]` の候補表示）

---

## 8. 関連コミット・参考

- `tools/edit_role_function.py`（開発者向け、SPEC 実装の参照）
- `generated/CookingHeaterController_powersim.py`（60 ロール関数）
  - 例: `F_ReadThermistors` / `F_SetTopHeaterDuty` / `F_CheckOvenOverheat`
- `docs/UI_TEST_MANUAL.md`（手動テストリスト）
- `THREAD_HANDOFF.md`（Trap #65〜#79 に開発経緯）
---

## 9. Phase 2 拡張: Cross-insert + Register

> **状態**: 仕様確定 / 実装 A から着手
> **決定事項**: 下記「未決事項の解決」参照

### 9.1 機能 A: Cross-insert（任意スニペットをカーソル位置の関数へ）

| 状況 | 挿入先 |
|---|---|
| カーソルが STABLE_USER_CODE ブロック内 | カーソル位置の Layer.Func |
| カーソルがブロック外 | スニペット自身の Layer.Func |
| 挿入先 != スニペット名 | 確認ダイアログを表示 |

**UI**:
- ダイアログタイトル: `Role Snippets  ->  Insert into: {Layer}.{Func}`
- 上部に黄色ラベル: `Insert into: {Layer}.{Func}`
- 挿入先 != スニペット名時: `MsgConfirmCrossInsert`

**ロジック**:
- `DetectRoleFunctionAtCaret()`: カーソル前の最後の START と、カーソル後の最初の END を照合
- 両マーカーの Layer.Func が一致する場合のみ挿入先として採用

### 9.2 機能 B: Register（選択関数をスニペット登録）

カーソルが STABLE_USER_CODE ブロック内のとき、その body をスニペットとして保存する。

**UI**:
- メニュー: `Register Current Function as Snippet...`（ブロック外ではグレーアウト）
- ダイアログ項目:
  - Snippet ID（既定 = 現在の Layer.Func、カスタム名も許可）
  - Display name
  - Description
  - Overwrite if exists（既定 OFF）
- 保存先: `templates/role_snippets/user/`（gitignore 対象）

**衝突時**:
- Overwrite ON: 確認ダイアログ後、上書き
- Overwrite OFF: エラー `MsgRegisterSnippetAlreadyExists`

### 9.3 ストレージ拡張

```
templates/role_snippets/
├── index.json                    # builtin 3 サンプル（tracked）
├── MwOven.F_ReadThermistors.py
├── MwOven.F_SetTopHeaterDuty.py
├── MwOven.F_CheckOvenOverheat.py
└── user/                         # gitignore
    ├── index.user.json
    └── *.py
```

`LoadAll()` は builtin + user をマージ。**同名 Layer.Func は user 優先**。

### 9.4 追加メニュー項目

| メニュー | 有効条件 | 動作 |
|---|---|---|
| Role Snippets... | 常に | Cross-insert 対応ダイアログ |
| Register Current Function as Snippet... | カーソルがブロック内 | 登録ダイアログ |

### 9.5 追加 i18n キー（10 キー）

`MenuRegisterSnippet` / `RegisterSnippetTitle` / `RegisterSnippetId` / `RegisterSnippetDisplayName` / `RegisterSnippetDescription` / `RegisterSnippetOverwrite` / `MsgRegisterSnippetInvalidId` / `MsgRegisterSnippetAlreadyExists` / `MsgRegisterSnippetSaved` / `MsgConfirmCrossInsert`

### 9.6 実装フェーズ

| Phase | 内容 | 見積 |
|---|---|---|
| A1 | DetectRoleFunctionAtCaret + Cross-insert | 1.5h |
| A2 | Cross-insert GUI テスト | 1h |
| B1 | user/ 対応（LoadAll マージ） | 1h |
| B2 | RegisterSnippetForm | 2h |
| B3 | メニュー項目 + グレーアウト | 30m |
| B4 | Register GUI テスト | 1h |
| C | i18n 10 キー x 2 言語 | 30m |
| D | .gitignore + doc | 15m |

### 9.7 未決事項の解決（2026-11-07 確定）

| # | 項目 | 決定 |
|---|---|---|
| 1 | Register の既定 ID | 現在の Layer.Func |
| 2 | カスタム名 | 許可 |
| 3 | user/ の gitignore | 追加する |
| 4 | Cross-insert 確認 | 挿入先 != スニペット名のときのみ |
| 5 | 一覧での user 表示 | 先頭に `[user]` バッジ |
| 6 | 実装順序 | A -> B |


## 10. 実装完了サマリ（2026-11-07）

| サブタスク | コミット | 内容 |
|---|---|---|
| B-1 | `76c4986` / `cc75e12` | `RoleSnippetRegistrar` + `RoleSnippetStorage` 統合 |
| B-3a | `049826d` | `RegisterSnippetDialog` + 右クリック + i18n 11 キー |
| B-3b | `6f6ba80` | `STABLE_USER_CODE_START` マーカー存在判定（component-agnostic） |
| B-4 | `d092499` | テスト自動化 27 本 + `InternalsVisibleTo` |
| B-3c | `1e5d58e` | `SetPythonCode` / `LoadFile` 後の自動再評価 + `menuItemRoleSnippets` 統一 |
| B-5 | `7650d34` | FlaUI E2E（`RoleSnippetRegisterE2ETests`）+ `LogSnippet` + Trap #15 fallback |
| verify | `7354d8c` / `897478c` | `verify_all.ps1` `[4c]` / `[4d]` 統合 |

### テスト構成（30 本）

| テストクラス | 件数 | 内容 |
|---|---:|---|
| `RoleSnippetRegistrarTests` | 8 | Extract / Register / バリデーション |
| `RoleSnippetStorageTests` | 6 | index.json / .py ファイル I/O |
| `RoleSnippetVisibilityTests` | 15 | L1 `ContainsRoleMarkers` (9) + L2 `Form_*` (6) |
| `RoleSnippetRegisterE2ETests` | 1 | FlaUI E2E（ダイアログ → ファイル生成） |

### 関連 Trap

| # | 罠 |
|---|---|
| 80 | `dotnet build \| Select-String "error CS"` は MSB3027/3021 を見逃す → `"error (CS\|MSB)"` |
| 81 | `<ClassName>Test.cs` / `<ClassName>Tests.cs` で CS0101+CS0111 |
| 82 | `rfind("</ItemGroup>")` 直前挿入は既存 ItemGroup 内にネスト → MSB4232 |
| 83 | WinForms `Control.Visible` は親 Form 非表示時 false → 内部フラグで観測 |
| 84 | 文字列リテラル内の `@"^\\s*..."` は regex 不正 |
| 85 | `[4/6]` FlaUI flaky → Start-Sleep 3 + retry-once |
| 86 | `TextBox.Name` は AutomationId / `ShowInTaskbar=false` modal は Win32 EnumWindows 必須 |
