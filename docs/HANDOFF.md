# 引き継ぎドキュメント — PowerSimBuilder

**作成日**: 2026-10-12
**前スレッドの到達点**: v0.1.0 リリース完了（公開レベル到達）

---

## 1. プロジェクト基本情報

| 項目 | 値 |
|------|-----|
| リポジトリ | https://github.com/akashi-hideki/PowerSimBuilder |
| ローカルパス | `C:\dev\PowerSimBuilder` |
| デフォルトブランチ | `main` |
| ライセンス | Apache-2.0 |
| 現行バージョン | v0.1.0 |
| 言語 | C# (.NET Framework 4.7.2) + Python 3.10+ (runtime) |
| GUI | WinForms |
| CI | GitHub Actions (windows-latest) |

---

## 2. 達成済み事項

### 環境整備
- OneDrive 外へ移動 (`C:\dev\PowerSimBuilder`)
- `.gitignore` 整備（build artifacts, IDE state, backup files）

### ビルド / テスト
- Costura.Fody 6.0.0 に統一（重複解消）
- `LangVersion=latest`, `PlatformTarget=x64`, `GenerateAssemblyInfo=true`
- xUnit テストプロジェクト (`PowerSimBuilder.Tests`) — 14 テスト成功

### バグ修正
- `LogcatTraceHelper.IsUserDefinedClass` の null 引数クラッシュ

### CI/CD
- GitHub Actions workflow (`.github/workflows/ci.yml`)
  - `Build & Test (Windows)`: Restore -> Build -> Test
  - `Code Quality`: Warnings as errors
- 全コミット success

### ドキュメント
- `README.md`（バッジ付き）
- `LICENSE`（Apache-2.0）
- `CHANGELOG.md`（Keep a Changelog 形式）
- `CONTRIBUTING.md`
- `.editorconfig`
- `docs/specs/SPEC_POWERSIM.md`（旧 readme.md の仕様書）
- `docs/specs/STATABLE_INTEGRATION.md`（StaTable 連携設計）

### ツール
- `tools/git_runner.py` — ops ファイルベースの git 操作
- `tools/check_actions.ps1` — GitHub Actions 状態確認
- `tools/git_ops.txt` — ops ファイル

### GitHub 設定
- Topics 10 個（powersim, simulation, csharp, winforms, python, ...）
- Release v0.1.0 作成

---

## 3. 主要ファイルの所在

```
C:\dev\PowerSimBuilder\
  .editorconfig
  .gitignore
  .github/workflows/ci.yml
  CHANGELOG.md
  CONTRIBUTING.md
  LICENSE
  README.md
  PowerSimBuilder.sln
  PowerSimBuilder.csproj
  PowerSimBuilder.Tests/
    PowerSimBuilder.Tests.csproj
    Core/PointJsonConverterTests.cs
    Core/ConnectionGraphTests.cs
    Core/LogcatTraceHelperSmokeTests.cs
    CodeGen/PythonSimulationMainBuilderTests.cs
  CanvasForm/          GUI (WinForms)
  ComponentEdit/       Code generation
  SimulationGenerate/  Python runtime bridge
  SimPartsManege/      Component library
  parts/               Utilities
  docs/
    specs/SPEC_POWERSIM.md
    specs/STATABLE_INTEGRATION.md
  tools/
    git_runner.py
    git_ops.txt
    check_actions.ps1
```

---

## 4. 運用ルール（重要）

### git 操作は必ず ops ファイル経由

```powershell
# 1. tools/git_ops.txt を編集
notepad tools\git_ops.txt

# 2. 確認
python tools\git_runner.py --list

# 3. dry-run
python tools\git_runner.py --dry-run

# 4. 実行
python tools\git_runner.py
```

直接 git add / git commit / git push は避ける。

### ops ファイル書式

```
add <path>
commit <single-line message>     # 1行のみ（複数行は未対応）
push [origin main]
```

### CI 確認

```powershell
powershell -ExecutionPolicy Bypass -File tools\check_actions.ps1
powershell -ExecutionPolicy Bypass -File tools\check_actions.ps1 -Watch
```

### ビルド環境

| ツール | パス |
|--------|------|
| MSBuild | `C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe` |
| dotnet | `C:\Program Files\dotnet\dotnet.exe` |
| gh | `C:\Program Files\GitHub CLI\gh.exe` |

PATH に無いため、フルパスで呼ぶ。

---

## 5. PowerShell 5.1 の落とし穴（学習済み）

| 問題 | 対策 |
|------|------|
| `@'...'@` のネスト不可 | Python で書く |
| BOM なし UTF-8 で日本語コメントが壊れる | `Set-Content -Encoding UTF8` (BOM付き) |
| markdown が here-string で壊れる | Python の `Path().write_text()` を使う |
| `Write-Host "=" * 70` が配列扱い | `Write-Host ("=" * 70)` |
| `$PSScriptRoot` がターミナル貼り付け時は空 | `-File` 実行で呼ぶ |

---

## 6. 次のフェーズ候補（優先度順）

### 中期（2週間）

| # | タスク | 説明 | 参考 |
|:-:|-------|------|------|
| 1 | Python ランタイム整備 | `component_base.py`, `simulation_data.py`, 8 コンポーネント | `docs/specs/SPEC_POWERSIM.md` |
| 2 | テストカバレッジ拡充 | coverlet 再導入、40%+ 目標 | 既存 14 テスト拡張 |
| 3 | Release workflow | タグ push で ZIP 自動生成 | `.github/workflows/release.yml` |
| 4 | Python Editor 統合テスト | `PythonEditorForm` テスト追加 | `ComponentEdit/PythonEditorForm.cs` |
| 5 | ドキュメント英語化 | `README_ja.md` 追加、`docs/en/` | - |

### 長期

| # | タスク | 説明 |
|:-:|-------|------|
| 6 | StaTable 統合実装 | `docs/specs/STATABLE_INTEGRATION.md` の実装 |
| 7 | PyPI 版 PowerSimBuilder | Python パッケージとしても配布 |
| 8 | カバレッジバッジ | Codecov 統合 |
| 9 | マルチプラットフォーム | .NET 8 移行検討 |

### 推奨着手順

1. Python ランタイム整備 — 現状最大のギャップ
2. テストカバレッジ拡充 — CI 品質向上
3. Release workflow — 自動化
4. StaTable 統合 — 差別化要素

---

## 7. 参考リンク

- 設計仕様: `docs/specs/SPEC_POWERSIM.md`
- StaTable 連携: `docs/specs/STATABLE_INTEGRATION.md`
- CHANGELOG: `CHANGELOG.md`
- コントリビューション: `CONTRIBUTING.md`
- Releases: https://github.com/akashi-hideki/PowerSimBuilder/releases

---

## 8. 次スレッドの開始プロンプト（推奨）

```
PowerSimBuilder (https://github.com/akashi-hideki/PowerSimBuilder) の
継続作業です。前スレッドで v0.1.0 をリリース済み。

ローカル: C:\dev\PowerSimBuilder
引き継ぎドキュメント: docs\HANDOFF.md を参照

次は「Python ランタイム整備」に着手したいです。
docs\specs\SPEC_POWERSIM.md に設計があるので、
component_base.py / simulation_data.py / 8 コンポーネント の
実装を進めたいです。
```

---

**End of handoff document.**
