# SPEC: Role Function Snippets (GUI 編集・挿入)

> **対象**: PowerSimBuilder v1.9.0 以降
> **目的**: 一般ユーザーがロール関数を GUI で編集・挿入できるようにする
> **最終更新**: 2026-11-03
> **状態**: SPEC 策定済み / 実装未着手

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
