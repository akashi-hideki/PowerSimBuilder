# SPEC: .psblog Import (Signal Log Reader)

> **対象**: PowerSimBuilder v1.10.0 以降
> **目的**: StaTable が出力する `.psblog` を PSB に取り込み、Canvas 上で時系列再生・波形分類・コンポーネント推論を行う
> **最終更新**: 2026-11-08
> **状態**: 仕様確定、実装フェーズ未着手
> **前提仕様**: `docs/SPEC_PSBLOG.md` v1 confirmed (StaTable approved 2026-11-08)

---

## 1. 背景と課題

### 現状

- Writer 側（StaTable）で `psblog/v1` 出力仕様が確定（`docs/SPEC_PSBLOG.md`）
- PSB 側の Reader は未実装
- 既存 6 デモはすべて **PSB → StaTable 方向**（設計 → シミュレーション）。逆方向（実機 → PSB）の取り込み経路が欠落

### 課題

| # | 課題 | 背景 |
|:---:|---|---|
| K1 | **変数セットはログごとに可変** | SPEC §1: 固定セットを要求しない（最重要原則） |
| K2 | 混在 Data | 1 行 1 変数 / バッチ応答 / 到着順不同 |
| K3 | 破損行への耐性 | 1 行破損で全体停止しない（SPEC §10.1） |
| K4 | 変数セマンティクス不明 | 波形から「何の変数か」を推測する必要 |

### あるべき UX

```
File → Import Signal Log...
  → PsblogImportForm（ファイル選択 + サマリ + 変数一覧 + 分類結果）
  → 変数チェック → [Add to Canvas]
  → 各変数が RecordedSignal として配置
  → 推定コンポーネント（ThermalMass 等）も併置（チェック ON 時）
```

---

## §1.1 命名原則: Writer と Reader の責務分離

`.psblog` の語源は **"PowerSimBuilder Log"**。**PSB は通信しない**。

| 項目 | Writer (StaTable) | Reader (PSB) |
|---|---|---|
| 通信 | ✅ UART 等で受信 | ❌ **しない**（テキスト読むだけ） |
| `@source uart:COM3` | 自分の経路 | **不透明文字列**（`header` に格納のみ） |
| `@protocol pstable-uart/v1` | 自分の ID | **不透明文字列**（同上） |
| モジュール名 | — | **`psblog.py`**（`uart_monitor.py` は誤り） |
| UI 文言 | — | **"Import Signal Log..."**（"UART" を含めない） |

**将来の CAN/LIN**（SPEC §7 拡張）でも、PSB 側のコード/UI は無変更で済むこと。

**再発防止**: Writer 側の名称（"UART Monitor"）を Reader 側のモジュール名に流用しない。

---

## 2. 設計

### 2.1 モジュール構成

| パス | 役割 | Phase |
|---|---|:---:|
| `src/python_runtime/psblog.py` | psblog/v1 パーサ + `SignalTrace` | 2A |
| `src/python_runtime/analysis/signal_classifier.py` | 波形分類 + コンポーネント推論 | 4 |
| `src/python_runtime/components/recorded_signal.py` | 時系列再生コンポーネント | 3 |
| `parts/PsblogImportForm.cs` | Import ダイアログ | 5 |
| `SimulationGenerate/PsblogParser.cs` | Python subprocess wrapper | 5 |
| `parts/ComponentDescriptorDictionary.cs` | `RecordedSignal` descriptor 追加 | 3 |
| `CanvasForm/MainForm.cs` | File メニュー項目追加 | 5 |
| `examples/logs/*.psblog` | サンプルログ（tracked） | 2A |
| `user_logs/*.psblog` | ユーザー記録（gitignored） | 2A |
| `tools/csv_to_psblog.py` | CSV → `.psblog` 変換（デモ用） | 3 |
| `tools/psblog_summary.py` | CLI サマリ（CI 検証用） | 2B |
| `tools/verify_psblog.py` | サンプルログ回帰検証 | 2B |
| `tests/python/test_psblog.py` | パーサ **24 tests** | 2A |
| `tests/python/test_signal_classifier.py` | 分類器 15 tests | 4 |
| `PowerSimBuilder.Tests/Psblog/PsblogParserTests.cs` | C# wrapper 3 tests | 5 |
| `PowerSimBuilder.Tests/Psblog/RecordedSignalDescriptorTests.cs` | 2 tests | 3 |
| `tests/PowerSimBuilder.UITests/PsblogImportE2ETests.cs` | FlaUI E2E 1 test | 5 |

### 2.2 ストレージ

`SPEC_ROLE_SNIPPETS.md` §2.1 の `templates/` + `user/` パターンに従う。

```
examples/logs/                          # tracked（サンプル、配布物）
├── README.md
├── sample_cooking_heater.psblog        # 10 変数 / SPEC §11 完全例
├── sample_pid_tuning.psblog            # 4 変数サブセット
├── sample_diagnostic.psblog            # 3 変数サブセット
├── sample_minimal.psblog               # 1 変数（最小）
└── sample_rotated/                     # ローテーション例（Phase 2B）
    ├── log.psblog
    └── log.1.psblog

user_logs/                              # gitignored（ユーザー記録、機微情報を含む可能性）
└── *.psblog
```

`.gitignore` 追記:

```
# User-recorded signal logs (may contain sensitive plant data)
user_logs/
```

### 2.3 公開 API (`psblog.py`)

```python
from __future__ import annotations
from dataclasses import dataclass, field
from datetime import datetime
from pathlib import Path

@dataclass
class VarMeta:
    name: str
    type: str           # "uint8_t" / "float32" / "bool" / "raw"
    unit: str           # "-" は "" に正規化
    group: str
    description: str = ""
    role: str = ""      # SPEC §6.2

@dataclass
class SignalTrace:
    header: dict[str, str] = field(default_factory=dict)
    vars: dict[str, VarMeta] = field(default_factory=dict)
    rows: list[tuple[int, str, float | int | bool | str]] = field(default_factory=list)
    footer: dict[str, str] = field(default_factory=dict)
    warnings: int = 0
    source_files: list[Path] = field(default_factory=list)

    def series(self, var: str) -> list[tuple[float, float]]: ...
    def captures_at(self) -> datetime: ...

def parse_psblog(path: Path | str) -> SignalTrace: ...
def parse_psblog_glob(base: Path | str) -> SignalTrace: ...   # Phase 2B
def to_json(trace: SignalTrace, out_path: Path | str) -> None: ...  # Phase 2B
def main(argv: list[str] | None = None) -> int: ...          # Phase 2B
```

### 2.4 型マッピング (SPEC §7 反映)

| psblog `<type>` | Python 戻り値型 | パース失敗時 |
|---|---|---|
| `uint8_t` / `uint16_t` / `uint32_t` | `int` | token skip + `warnings++` |
| `int8_t` / `int16_t` / `int32_t` | `int` | 同上 |
| `float32` / `float64` | `float` | 同上 |
| `bool` | `bool` | `int(v) != 0` |
| `raw` | `str` | 常に成功 |
| 未知 | `int → float → str` 段階的降格 | 最終段で `str` |

### 2.5 エラーハンドリング (SPEC §10.1 反映)

| 状況 | 挙動 | `warnings` |
|---|---|:---:|
| 空行 / `#` 始まり | ignore | — |
| `@var` で必須不足 | 行 skip | +1 |
| Data 変数未宣言 | トークン skip | +1 |
| Data 数値パース失敗 | トークン skip | +1 |
| 未知 `@meta` 行 | ignore（将来拡張用） | — |
| 行全体破損 | その行のみ skip、継続 | +1 |
| `@schema` != `psblog/v1` | **`ValueError`**（唯一の致命的エラー） | 致命的 |
| `@captured_at` 欠落 | **`ValueError`** | 致命的 |
| `@dropped != 0` | footer 記録、UI で警告表示 | — |
| ファイル先頭 BOM | `utf-8-sig` で透過除去（防御的寛容性） | — |

**注**: SPEC §4.1 は Writer に「BOM 付与禁止」を課す。Reader は防御的に BOM を許容する（誤設定ツールとの相互運用）。これは §10.1 の「Fault tolerant」原則に沿う。

### 2.6 ローテーション結合 (SPEC §4.3)

- `parse_psblog_glob(base)` は `<base>.psblog` / `<base>.N.psblog` を glob 収集
- 各ファイルの `@captured_at` でソート → 先頭基準で `t_ms` 再計算（offset 加算）
- `source_files` に読込一覧保持
- `@dropped` を合算
- ローテーション境界のサンプル欠落は許容（SPEC §4.3 明記）

### 2.7 CLI インタフェース

```
python -m python_runtime.psblog <file.psblog> [--json-out <path>] [--glob]
```

- `--json-out <path>` を **必須化**（stdout パイプ 4KB 問題の恒久回避、Trap #49）
- exit 0: 成功（`warnings > 0` でも 0）
- exit 1: 致命的エラー（schema 不一致 / captured_at 欠落 / ファイル不在）

C# wrapper (`SimulationGenerate/PsblogParser.cs`) は `PythonExecutor` と同型（`BeginOutputReadLine` + `BeginErrorReadLine` 必須、Trap #49）。

---

## 3. 波形分類 (`analysis/signal_classifier.py`)

### 3.1 API

```python
@dataclass
class Classification:
    var_name: str
    category: str          # "constant" / "ramp" / "step" / "exponential" / "pwm" / "digital"
    confidence: float      # 0.0 - 1.0
    proposed_component: str | None
    reasoning: str

def classify(trace: SignalTrace, var: str) -> Classification: ...
def classify_all(trace: SignalTrace) -> list[Classification]: ...
```

### 3.2 判定ルール (SPEC §6 を実装可能に具体化)

| category | ヒューリスティック | 提案コンポーネント |
|---|---|---|
| `constant` | stddev < 1e-6 | （無視） |
| `ramp` | 線形回帰 R² > 0.99 かつ slope > 0 | `Integrator` |
| `step` | 段差検出 1〜3 箇所 | `StepSource` |
| `exponential` | 1 次遅れ近似誤差 < 5%（簡易 3 点法、scipy 不使用） | `FirstOrderLag` / `ThermalMass` |
| `pwm` | 段差 > 5 箇所 かつ レベル数 2〜4 | `PIDController` 出力 |
| `digital` | 値域 ⊆ {0,1} かつ遷移 > 0 | `DigitalInput` |
| `unknown` | 上記該当なし | （ユーザー判断） |

### 3.3 `role` 優先 (SPEC §6.2)

| role | 強制提案 | confidence |
|---|---|---|
| `target` | `StepSource` | 1.0 |
| `pid_out` | `PIDController` | 1.0 |
| `input` | `DigitalInput` / `StepSource`（shape による） | 1.0 |
| `output` | shape 判定 | shape 依存 |
| `error` / `state` | （無視） | — |

---

## 4. RecordedSignal コンポーネント

### 4.1 Python 実装（実測 `first_order_lag.py` の API 準拠）

```python
class RecordedSignal(Component):
    def __init__(self, comp_id: str, comp_type: str = "RecordedSignal") -> None:
        super().__init__(comp_id, comp_type)
        self.parameters = {
            "psblog_path": "",
            "var_name": "",
            "loop": False,
            "hold_last": True,
        }
        self.inputs = {}
        self.outputs = {"value": 0.0}
        self._trace = None
        self._series = None
        self._t0 = 0.0
        self._cache_idx = 0

    def reset(self) -> None:
        self.outputs = {"value": 0.0}
        self._t0 = 0.0
        self._cache_idx = 0

    def _ensure_loaded(self):
        if self._trace is None:
            # lazy import（Trap #57 系）
            from python_runtime.psblog import parse_psblog
            path = self.parameters.get("psblog_path", "")
            if not path:
                return
            self._trace = parse_psblog(path)
            self._series = self._trace.series(self.parameters.get("var_name", ""))
            self._t0 = self._series[0][0] if self._series else 0.0

    def update(self, t: float, dt: float, sim_data) -> None:
        self._ensure_loaded()
        if not self._series:
            self.outputs["value"] = 0.0
            return
        self.outputs["value"] = self._lookup(t - self._t0)
```

### 4.2 C# Descriptor

```csharp
["RecordedSignal"] = new ComponentDescriptor {
    Name = "RecordedSignal",
    Category = "Source",
    Description = I18n.T("Component.RecordedSignal.Description"),
    Parameters = new Dictionary<string, ParamSpec> {
        ["psblog_path"] = new ParamSpec { Type = "path", Label = I18n.T("Param.PsblogPath") },
        ["var_name"]    = new ParamSpec { Type = "string", Label = I18n.T("Param.VarName") },
        ["loop"]        = new ParamSpec { Type = "bool", Default = false },
        ["hold_last"]   = new ParamSpec { Type = "bool", Default = true },
    },
    InputPorts = Array.Empty<PortSpec>(),
    OutputPorts = new[] { new PortSpec { Name = "value", Type = "float" } },
}
```

### 4.3 `demo_main.py` への影響

`PythonSimulationMainBuilder` は **変更不要**。ただし `psblog_path` は `RepairGeneratedPaths` の絶対パス化対象に追加（Trap #79）。

---

## 5. Import UI (`PsblogImportForm`)

### 5.1 メニュー追加

```
File
├── New Project           (既存)
├── Open...               (既存)
├── Import Signal Log...  ← NEW
├── Save                  (既存)
└── ...
```

i18n: `Menu.File.ImportPsblog` = "Import Signal Log..." / "导入信号日志..."

### 5.2 ダイアログレイアウト

```
┌─ Import Signal Log ────────────────────────────────────┐
│ [File path: _________________________] [Browse...]     │
│                                                        │
│ ┌─ Summary ─────────────────────────────────────────┐  │
│ │ Source:    uart:COM3   (opaque, not interpreted)  │  │
│ │ Captured:  2026-11-09 10:30:00 JST                │  │
│ │ Duration:  300.0 s / 30,000 samples               │  │
│ │ Firmware:  StaTable v1.9.0                        │  │
│ │ Warnings:  0   Dropped: 0                         │  │
│ └───────────────────────────────────────────────────┘  │
│                                                        │
│ ┌─ Variables ───────────────────────────────────────┐  │
│ │ ☑ Name               Type    Unit  Shape  → Component      │  │
│ │ ☑ oven_current_temp  uint16  degC  exp    → ThermalMass    │  │
│ │ ☑ heater_top_duty    uint8   %     pwm    → PIDController  │  │
│ │ ☐ g_system_tick      uint32  1ms   ramp   → (ignore)       │  │
│ │ ☑ door_open          uint8   -     digital→ DigitalInput   │  │
│ └───────────────────────────────────────────────────┘  │
│                                                        │
│ ☐ Place inferred components                            │
│                                                        │
│ [ Cancel ]                          [ Add to Canvas ]  │
└────────────────────────────────────────────────────────┘
```

**注**: Summary の `Source: uart:COM3` は **不透明文字列として表示のみ**（PSB は解釈しない）。

### 5.3 [Add to Canvas] 動作

1. チェック変数ごとに `RecordedSignal` 配置（`psblog_path` 絶対化）
2. [Place inferred components] ON なら提案コンポーネントも併置（**配線は手動**）
3. `ArrangeComponents()` で座標決定
4. 完了ダイアログ（`PsblogImport.Complete`）

### 5.4 i18n キー一覧（20 キー）

| キー | en | zh |
|---|---|---|
| `Menu.File.ImportPsblog` | Import Signal Log... | 导入信号日志... |
| `PsblogImport.Title` | Import Signal Log | 导入信号日志 |
| `PsblogImport.FileLabel` | File path | 文件路径 |
| `PsblogImport.Browse` | Browse... | 浏览... |
| `PsblogImport.Summary` | Summary | 摘要 |
| `PsblogImport.Variables` | Variables | 变量 |
| `PsblogImport.Col.Name` | Name | 名称 |
| `PsblogImport.Col.Type` | Type | 类型 |
| `PsblogImport.Col.Unit` | Unit | 单位 |
| `PsblogImport.Col.Shape` | Shape | 波形 |
| `PsblogImport.Col.Component` | → Component | → 组件 |
| `PsblogImport.PlaceInferred` | Place inferred components | 同时放置推定组件 |
| `PsblogImport.Cancel` | Cancel | 取消 |
| `PsblogImport.AddToCanvas` | Add to Canvas | 添加到画布 |
| `PsblogImport.Complete` | {0} signal(s) added. | 已添加 {0} 个信号。 |
| `PsblogImport.ErrorParse` | Failed to parse: {0} | 解析失败: {0} |
| `PsblogImport.WarnDropped` | {0} samples dropped | {0} 个样本丢失 |
| `Param.PsblogPath` | Log file | 日志文件 |
| `Param.VarName` | Variable | 变量名 |
| `Component.RecordedSignal.Description` | Replay one signal from a .psblog log | 从 .psblog 日志回放单个信号 |

---

## 6. 初期サンプルログ

| ファイル | 変数数 | 用途 |
|---|---:|---|
| `sample_cooking_heater.psblog` | 10 | SPEC §11 完全例（oven 閉ループ） |
| `sample_pid_tuning.psblog` | 4 | PID チューニング用サブセット |
| `sample_diagnostic.psblog` | 3 | ハードウェア診断用サブセット |
| `sample_minimal.psblog` | 1 | 最小ログ（1 変数） |
| `sample_rotated/` | — | ローテーション結合（Phase 2B） |

**変数の出典**: `generated/CookingHeaterController_powersim.py` の `self.globals`（34 変数）。**`.psbproj` とは独立**（SPEC §1）。

---

## 7. 実装フェーズ

| Phase | 内容 | 規模 | 見積 | 依存 | 公開前 |
|:---:|---|:---:|---:|:---:|:---:|
| 1 | SPEC 確定（本ドキュメント） | S | 本セッション | — | ✅ |
| 2A | `psblog.py` + 24 tests + 4 samples | S | 1 日 | Phase 1 | ✅ 前倒し可 |
| 2B | ローテーション + CLI + `verify_psblog.py` | S | 0.5 日 | 2A | 公開後 |
| 3 | `RecordedSignal` (Python + Descriptor) + `csv_to_psblog.py` | M | 1 日 | 2A | 公開後 |
| 4 | `signal_classifier.py` + 15 tests | M | 1 日 | 2A | 公開後 |
| 5 | `PsblogImportForm` + i18n + E2E | M | 1.5 日 | 4 | 公開後 |
| 6 | `verify_all.ps1` 統合 + README + CHANGELOG | S | 0.5 日 | 5 | 公開後 |
| **合計** | | | **~5.5 日** | | |

**方針**: 11/22 公開前に前倒しできるのは **Phase 2A のみ**。他は公開後 v1.10.0 にまとめる。

---

## 8. 後方互換性

| 項目 | 影響 |
|---|---|
| 既存 C# テスト 599 | 変更なし（新規テストのみ追加） |
| 既存 Python テスト 457 | 変更なし |
| `python_runtime/__init__.py` | **`psblog` は lazy import**（起動時 import しない） |
| `demo_main.py` 生成 | 変更なし（`RecordedSignal` は既存経路で自然に生成） |
| `requirements.txt` | **追加なし**（`re` / `json` / `pathlib` / `datetime` のみ） |
| `ComponentDescriptorDictionary.cs` | 追記のみ（既存 descriptor 不変） |
| 既存 `.psbproj` | 影響なし |

---

## 9. リスクと対策

| # | リスク | 対策 |
|:---:|---|---|
| R1 | 可変変数セットの誤解（罠 #42 変種） | **§1.1 命名原則** + 24 tests に可変セット 5 本 + サンプル 4 本 |
| R2 | `python_runtime.__init__.py` 汚染 | `psblog` を lazy import に統一 |
| R3 | C# subprocess 4KB パイプ満杯（Trap #49） | `--json-out <file>` を必須化 |
| R4 | i18n カンマ欠落（罠 #24、5 回再発） | `tools/i18n_check.py --fix` + `tools/gcommit.ps1` |
| R5 | `RecordedSignal` パレット非表示（Trap #53） | `Pallets/ComponentDescriptors.xml` 削除 → GUI 再起動 |
| R6 | ローテーション境界のサンプル欠落 | SPEC §4.3 で許容済み |
| R7 | 11/22 公開圧迫 | Phase 2A のみ前倒し、他は公開後 |
| R8 | `ShowDialog` + `ShowInTaskbar=false` の FlaUI 検出不可（Trap #15/#86） | Win32 `EnumWindows` フォールバック |
| R9 | 分類器の scipy 依存誘発 | 簡易 3 点法で実装、依存追加ゼロ方針 |

---

## 10. 未決事項（2026-11-08 確定済み）

| # | 項目 | 確定 |
|:---:|---|---|
| 1 | C#↔Python 連携 | subprocess + `--json-out` |
| 2 | `psblog.py` 単一ファイル | 単一ファイル（~300 行） |
| 3 | `signal_classifier` 配置 | `src/python_runtime/analysis/` + `pyproject.toml` packages に 1 行追加 |
| 4 | `RecordedSignal` の C# descriptor | 追加（実測 API: `comp_id`, `comp_type`, `self.parameters`） |
| 5 | Import ダイアログ | 変数一覧 + 分類結果 + 選択チェック |
| 6 | サンプル vs ユーザーログ | `examples/logs/`（tracked） + `user_logs/`（gitignored） |
| 7 | ローテーション自動収集 | glob 収集する |
| 8 | リリースタイミング | 11/22 公開後 v1.10.0（minor bump） |

---

## 11. 関連コミット・参考

| ファイル | 用途 |
|---|---|
| `docs/SPEC_PSBLOG.md` | Writer 側（StaTable）仕様 v1 confirmed |
| `docs/SPEC_ROLE_SNIPPETS.md` | ストレージパターン参考（`templates/` + `user/`） |
| `docs/SPEC_MULTISELECT_GROUP.md` | API 設計 + 実装フェーズ構造の参考 |
| `docs/SPEC_PYTHON_RUNTIME.en.md` | Python 依存管理の方針参考 |
| `tools/verify_all.ps1` | 統合先（`[3.6]` / `[3.7]` / `[4e]`） |
| `tools/gcommit.ps1` | Trap #80 恒久対策済 |
| `tools/i18n_check.py` | 罠 #24 恒久対策済 |
| `tools/scan_imports.py` | 新規依存混入チェック |
| `THREAD_HANDOFF.md` | Trap #1〜#92 参照 |

---

*このドキュメントは PowerSimBuilder 側の `.psblog` Import 機能の実装契約。*
*Writer 側仕様は `docs/SPEC_PSBLOG.md` を参照。*
