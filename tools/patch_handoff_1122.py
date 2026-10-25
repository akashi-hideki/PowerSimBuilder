"""Insert the 11/22 deferral section into HANDOFF_MVP1_2.md.

Usage:
    python tools/patch_handoff_1122.py --dry-run
    python tools/patch_handoff_1122.py
"""

from __future__ import annotations

import argparse
import re
import shutil
import sys
from pathlib import Path

HANDOFF = Path(__file__).parent.parent / "HANDOFF_MVP1_2.md"

NEW_SECTION = r"""## 公開 11/22（土）延期決定 — v1.7.0/v1.8.0/v1.8.1 ロードマップ

**2026-10-25 決定。旧計画（11/01 公開）を破棄し、11/22 公開へ延期。**

### 決定理由

| 観点 | 旧計画（11/01） | 新計画（11/22） |
|------|--------------|----------------|
| シミュレーション実演 | 未検証前提 | E2E 検証期間を確保 |
| ストーリー | "Real simulation test" が実態と乖離するリスク | 実測ベースで判断可能 |
| 罠 #42 再発 | リスクあり | 構造的に回避 |

### 実装状態（2026-10-25 実測）

罠 #42 の教訓に従い `git grep` でコードベースを実測:

| 機能 | 状態 | 根拠 |
|------|------|------|
| FMU パース | 実装済 | `PhysicsImportForm.ParseFmu` |
| fmpy スニペット生成 | 実装 + テスト済 | `PhysicsSnippetInserterTests` |
| Python 実行エンジン | 実装済 | `SimulationGenerate\PythonExecutor.cs` |
| Run ボタン | 実装済 | `PythonEditorForm.btnRun` → `_executor.Run()` |
| PNG 生成 | 実装済 | `PythonSimulationMainBuilder.cs:827` |

**→ MVP-2 は「実装済み、E2E 未検証」の状態**（設計段階ではない）。

### 2 つの独立パス（重要区別）

| パス | 内容 | E2E 証明 |
|------|------|:---:|
| **A** | StaTable XML → ControllerBox → シミュレーション → PNG | 10/25 証明済（`generated/demo_main.py`） |
| **B** | Component + FMU → fmpy load_fmu → シミュレーション → PNG | 未証明（スニペット挿入のみテスト済） |

**公開ストーリーの判断は #1（10/29）の結果待ち。**

### ロードマップ（小刻みリリース）

| バージョン | 内容 | 時期 | 公開 |
|-----------|------|:---:|:---:|
| **v1.7.0** | `tools/handoff_editor.py`（3 モード GUI） | 10/28 | 内部 |
| **v1.8.0** | パスB E2E 証明 + デモシナリオ強化 | 11/13 | 内部 |
| **v1.8.1** | E2E 回帰 + 修正 | 11/20 | — |
| **公開** | v1.8.1 タグで公開 | **11/22（土）** | ✅ |

### v1.8.0 タスク

| # | タスク | 検証内容 | 時期 |
|---|--------|---------|:---:|
| 1 | FMU E2E 手動テスト | Component → Add Physics → FMU → Run → PNG | 10/29 |
| 2 | 発見バグ修正 | パスB が動かない場合 | 11/1–11/10 |
| 3 | デモシナリオ強化 | ステップ入力 + 60秒以上 + 複数信号 | 11/8–11/10 |
| 4 | デモ用 StaTable XML 準備 | 公開時に使える制御系 | 11/10 |
| 5 | README に実演手順追記 | パスA + パスB の 2 デモ | 11/11 |

### 公開ストーリー 3 案（#1 の結果で確定）

| 案 | 条件 | ストーリー |
|----|------|-----------|
| **A** | パスA のみ証明 | 「StaTable 設計 → シミュレーション生成 → 波形出力」 |
| **B**（推奨） | パスA + パスB 証明 | 「StaTable 設計 + 物理モデル（FMU）統合 → 閉ループ検証」 |
| **C** | パスB 失敗 | v1.8.0 の FMU 修正を 11/22 以降に延期、v1.7.0 で公開 |

### 保留事項の再整理

| # | 項目 | 更新 |
|---|------|------|
| 1 | リポジトリ public 化 | **11/22 公開と同時**（従来: シミュレーションサンプルテスト完了後） |
| 2 | カタログ公開配布の再開 | 上記と連動 |
| 3 | `LAWYER_CONSULTATION.md` | 変化なし（ローカルのみ） |

### 罠 #42 再発防止の運用

今後、「実装済み」と文書に書く際は以下を必須とする:

- `Select-String -Path "*.cs","**\*.cs" -Pattern "<機能名>"` でコード実測
- `Select-String -Path "*.cs","**\*.cs" -Pattern "<関連テスト名>"` でテスト実測
- 可能なら E2E の最小パスを実行検証

「テストがある」「UI がある」だけで「実装済み」と書かず、**E2E 検証の有無を明記**する。
"""


def main(argv=None):
    parser = argparse.ArgumentParser(description="Insert 11/22 deferral section")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args(argv)

    if not HANDOFF.exists():
        print(f"ERROR: file not found: {HANDOFF}", file=sys.stderr)
        return 1

    text = HANDOFF.read_text(encoding="utf-8")
    nl = "\r\n" if "\r\n" in text else "\n"

    m = re.search(r"^## v\d+\.\d+\.\d+", text, re.MULTILINE)
    if not m:
        print("ERROR: no top version section header", file=sys.stderr)
        return 1

    idx = m.start()
    line_no = text[:idx].count(nl) + 1
    top_header = text[idx:].split(nl, 1)[0]
    print(f"Insert at line {line_no}: {top_header}")

    section = NEW_SECTION.rstrip() + nl + nl
    new_text = text[:idx] + section + text[idx:]
    print(f"Size: {len(text)} -> {len(new_text)} (+{len(new_text) - len(text)})")

    if args.dry_run:
        print("[DRY] no file written")
        return 0

    bak = Path(str(HANDOFF) + ".bak")
    shutil.copy2(HANDOFF, bak)
    print(f"Backup: {bak}")

    HANDOFF.write_text(new_text, encoding="utf-8")
    print(f"Wrote: {HANDOFF}")
    return 0


if __name__ == "__main__":
    sys.exit(main())