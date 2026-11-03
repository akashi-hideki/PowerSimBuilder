# SPEC: 複数選択 + グループ保存

> **対象**: PowerSimBuilder v1.9.0 以降
> **目的**: 複数コンポーネントの選択・一括操作・グループ保存/ロード
> **最終更新**: 2026-11-03
> **状態**: 調査完了、実装フェーズ未着手

---

## 1. 背景と動機

現状、キャンバスは**単一選択**のみ対応（`SelectedComponent`）。以下の操作ができない:

- 複数コンポーネントの一括削除・移動・コピー
- 選択範囲を「グループ」として保存し、別プロジェクトで再利用
- ControllerBox + PID + OvenChamber のような**シミュレーション 1 セット**を 1 単位で扱う

他の開発者が使う前提で、**後方互換性を保った API** を設計する。

---

## 2. 現状分析（2026-11-03 調査）

### 2.1 選択モデル

| 項目 | 現状 | 参照箇所 |
|------|------|---------|
| `DesignCanvas.SelectedComponent` | 単一選択 | 約 80 |
| `ComponentControl.IsSelected` | bool（視覚表示用） | 約 30 |
| `CanvasModel.SelectedComponentIndex` | int（-1 = 未選択） | 保存/復元用 |

### 2.2 選択変更の経路

```
ComponentControl.MouseDown
  -> DesignCanvas.ComponentControl_MouseDown
  -> HandleComponentMouseDown
    -> StartDragging(component, location)

ComponentControl.SelectedChanged
  -> DesignCanvas.Component_SelectedChanged
  -> HandleComponentSelectedChanged(sender)
    -> 前の選択を解除 -> 新しい選択を設定 -> SelectionChanged 発火
```

### 2.3 マウスイベント

| イベント | ハンドラ | 拡張容易性 |
|--------|---------|-----------|
| `OnMouseDown` | `HandleMouseDown(e.Location)` | キャンバス座標で判定可能 |
| `ComponentControl_MouseDown` | `HandleComponentMouseDown(sender, e)` | Ctrl キー判定追加可能 |
| `ComponentControl_MouseMove` | `HandleComponentMouseMove` | 複数ドラッグ対応可能 |
| `ComponentControl_MouseUp` | `HandleComponentMouseUp` | 拡張可能 |

### 2.4 影響範囲

| カテゴリ | ファイル数 | 参照数 |
|--------|---------|-----------|
| コア（CanvasManager / DesignCanvas / ComponentControl） | 3 | 約 100 |
| UI（MainForm / ComponentPalette） | 2 | 約 25 |
| テスト | 5 | 約 50 |

---

## 3. API 設計（後方互換）

### 3.1 DesignCanvas

```csharp
public ComponentControl SelectedComponent
{
    get => SelectedComponents.FirstOrDefault();
    private set
    {
        SelectedComponents.Clear();
        if (value != null) SelectedComponents.Add(value);
    }
}

public IReadOnlyList<ComponentControl> SelectedComponents { get; } = new List<ComponentControl>();

public void SelectComponent(ComponentControl c, bool additive = false);
public void SelectComponents(IEnumerable<ComponentControl> components);
public void ClearSelection();
public bool IsComponentSelected(ComponentControl c);
```

### 3.2 CanvasManager

```csharp
public ComponentControl SelectedComponent { get; set; }
public IReadOnlyList<ComponentControl> SelectedComponents { get; }
public void SelectComponents(IEnumerable<ComponentControl> components);
```

### 3.3 保存モデル

```csharp
public class CanvasModel
{
    public List<ComponentModel> Components { get; set; } = new();
    public List<ConnectionModel> Connections { get; set; } = new();
    public int SelectedComponentIndex { get; set; } = -1;
    public List<int> SelectedComponentIndices { get; set; } = new();
}
```

---

## 4. 実装フェーズ

| Phase | 内容 | 見積 |
|--------|---------|-----------|
| 1 | 複数選択のコア | 2h |
| 2 | 一括移動 | 1h |
| 3 | グループ保存 | 1h |
| 4 | グループロード | 1h |
| 5 | テスト + ドキュメント | 1h |

**合計見積: 6h**

---

## 5. リスクと対策

| リスク | 対策 |
|--------|---------|-----------|
| `SelectedComponent` 参照 80 箇所の互換性 | getter で `SelectedComponents.FirstOrDefault()` |
| 既存テスト 30 個の破壊 | 後方互換 API で回避 |
| `CanvasModel` JSON 互換性 | 新規フィールド追加のみ |
| 複数ドラッグの座標計算 | 初期位置を `Dictionary` で保持 |

---

## 6. 未決事項

- 矩形選択: Phase 1 では省略
- Shift+クリック: Ctrl と同義にするか要検討
- GroupBox 見た目: Phase 3 では省略、データのみ
