# tools/investigate_psblog.ps1
# .psblog Import 調査スクリプト (Phase 2A 着手前チェック)
#
# 使い方:
#   .\tools\investigate_psblog.ps1
#   出力: tmp\psblog_investigation.md
#
# 注意: PowerShell 5.1 互換。BOM なし UTF-8 で書き出し。

$ErrorActionPreference = "Continue"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$log = [System.Collections.Generic.List[string]]::new()
function L { param([string]$s = "") $log.Add($s) | Out-Null; Write-Host $s }
function Run {
    param([string]$label, [scriptblock]$block)
    L ""
    L "### $label"
    try {
        $r = & $block 2>&1 | Out-String -Width 300
        if ([string]::IsNullOrWhiteSpace($r)) { L "(empty)" } else { L $r.TrimEnd() }
    } catch { L "(ERROR: $_)" }
}

# ---- 開始 ----
L "# .psblog Import 調査結果"
L ""
L "Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
L "Commit:    $(git rev-parse --short HEAD 2>&1)"
L ""

# ============================================================
L "## A. 既存コードベース整合"

Run "A1: python_runtime 構造" {
    "--- components/ ---"
    Get-ChildItem src\python_runtime\components -File -EA SilentlyContinue | Select-Object -ExpandProperty Name
    "--- lib/ ---"
    Get-ChildItem src\python_runtime\lib -File -EA SilentlyContinue | Select-Object -ExpandProperty Name
    "--- analysis/ ---"
    if (Test-Path src\python_runtime\analysis) { "EXISTS" } else { "NOT EXISTS" }
}

Run "A2: pyproject.toml" {
    "--- requires-python ---"
    Select-String pyproject.toml -Pattern 'requires-python' | Select-Object -First 1
    "--- packages ---"
    Select-String pyproject.toml -Pattern 'packages' -Context 0,10 | Select-Object -First 1
    "--- pytest ---"
    Select-String pyproject.toml -Pattern 'pytest' -Context 0,5 | Select-Object -First 1
}

Run "A3: first_order_lag.py 先頭 40 行" {
    Get-Content src\python_runtime\components\first_order_lag.py -EA SilentlyContinue | Select-Object -First 40
}

Run "A4: ThermalMass descriptor" {
    Select-String -Path 'parts\*.cs' -Pattern 'ThermalMass' -Context 0,25 -EA SilentlyContinue | Select-Object -First 1
}

Run "A5: MainForm File メニュー" {
    Select-String -Path 'CanvasForm\MainForm.cs' -Pattern 'menuItemFile' -Context 0,3 -EA SilentlyContinue | Select-Object -First 5
}

Run "A6: PhysicsImportForm 構造" {
    Get-ChildItem parts\Physics -File -EA SilentlyContinue | Select-Object -ExpandProperty Name
    "---"
    Select-String -Path 'parts\Physics\PhysicsImportForm.cs' -Pattern 'TabControl' -EA SilentlyContinue | Select-Object -First 3
}

Run "A7: PythonExecutor" {
    Select-String -Path 'SimulationGenerate\PythonExecutor.cs' -Pattern 'BeginOutputReadLine|WorkingDirectory|ProcessStartInfo' -Context 0,2 -EA SilentlyContinue | Select-Object -First 5
}

Run "A8: i18n" {
    $enJson = Get-Content Resources\i18n\en.json -Raw -EA SilentlyContinue
    if ($enJson) {
        $count = ($enJson | ConvertFrom-Json).PSObject.Properties.Count
        "key count: $count"
    }
    "--- Menu.File 命名 ---"
    Select-String -Path 'Resources\i18n\en.json' -Pattern '"Menu\.File' -EA SilentlyContinue | Select-Object -First 5
    "--- Component.* 命名 ---"
    Select-String -Path 'Resources\i18n\en.json' -Pattern '"Component\.' -EA SilentlyContinue | Select-Object -First 3
}

Run "A9: verify_all.ps1 セクション番号" {
    Select-String -Path 'tools\verify_all.ps1' -Pattern '\[\d+[a-z]?(\.\d+)?(/6)?\]' -EA SilentlyContinue | Select-Object -First 30
}

Run "A10: scan_imports.py" {
    "--- --check output ---"
    python tools\scan_imports.py --check 2>&1
    "--- exit code: $LASTEXITCODE ---"
}

# ============================================================
L ""
L "## C. 環境・依存"

Run "C1: Python" {
    python --version 2>&1
    "executable:"
    python -c "import sys; print(sys.executable)" 2>&1
}

Run "C2: stdlib (re/json/pathlib/datetime)" {
    python -c "import re, json, pathlib, datetime; print('OK')" 2>&1
}

Run "C3: pytest basetemp" {
    python -m pytest tests\python\test_lib_filters.py -v 2>&1 | Select-Object -First 5
}

Run "C4: pre-commit フック" {
    Select-String -Path '.pre-commit-config.yaml' -Pattern 'id:' -EA SilentlyContinue | Select-Object -First 20
}

# ============================================================
L ""
L "## D. 既存デモ整合"

Run "D1: oven_control_statable.psbproj 変数 (重要)" {
    python tools\inspect_psbproj.py examples\demos\oven_control_statable.psbproj 2>&1
}

Run "D2: demo_statable_runtime_loop.py 出力列" {
    Select-String -Path 'examples\demos\demo_statable_runtime_loop.py' -Pattern 'outputs\[|globals\[' -EA SilentlyContinue | Select-Object -First 20
}

Run "D3: examples\logs 存在" {
    if (Test-Path examples\logs) {
        "EXISTS:"
        Get-ChildItem examples\logs -EA SilentlyContinue | Select-Object -ExpandProperty Name
    } else {
        "NOT EXISTS (新規作成対象)"
    }
}

# ============================================================
L ""
L "## 完了"
L ""
L "このファイルの内容をそのまま返信してください。"

# ---- ファイル書き出し (UTF-8 BOM なし, Trap #45/#54 対策) ----
New-Item -ItemType Directory -Path tmp -Force -EA SilentlyContinue | Out-Null
$tmpAbs = (Resolve-Path tmp).Path
$outPath = Join-Path $tmpAbs "psblog_investigation.md"
[System.IO.File]::WriteAllLines($outPath, $log, [System.Text.UTF8Encoding]::new($false))

Write-Host ""
Write-Host "Written to: $outPath" -ForegroundColor Green
Write-Host "次の手順: このファイルの中身をそのままチャットに貼り付けてください。"