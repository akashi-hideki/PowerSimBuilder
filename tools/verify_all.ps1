$ErrorActionPreference = "Continue"
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$OutputEncoding = [Text.Encoding]::UTF8
Write-Host ""
Write-Host ("=" * 60) -ForegroundColor Cyan
Write-Host "  FULL VERIFICATION" -ForegroundColor Cyan
Write-Host ("=" * 60) -ForegroundColor Cyan

# 1) Build
Write-Host ""
Write-Host "[1/6] BUILD" -ForegroundColor Yellow
$buildOut = & dotnet build --nologo 2>&1
$buildExit = $LASTEXITCODE
$errs = $buildOut | Where-Object { "$_" -match "error CS" } | Select-Object -First 5
if ($errs) { $errs | ForEach-Object { Write-Host "  $_" -ForegroundColor Red } }
if ($buildExit -eq 0) { Write-Host "  exit=0 OK" -ForegroundColor Green } else { Write-Host "  exit=$buildExit NG" -ForegroundColor Red }

# 2) psbproj metadata + components
Write-Host "[2/6] PSBPROJ METADATA" -ForegroundColor Yellow
$p = "examples\demos\oven_control.psbproj"
if (Test-Path $p) {
    python tools\inspect_psbproj.py $p
} else {
    Write-Host "  NOT FOUND: $p" -ForegroundColor Red
}

# 3) Python PID verification
Write-Host "[3/6] PYTHON PID VERIFICATION" -ForegroundColor Yellow
if (Test-Path "tools\verify_pid.py") {
    python tools\verify_pid.py 2>&1 | ForEach-Object { Write-Host "  $_" }
} else {
    Write-Host "  SKIP (tools\verify_pid.py not found)" -ForegroundColor Yellow
}

# 3.5) StaTable psbproj metrics
Write-Host "[3.5/6] STATABLE PSBPROJ METRICS" -ForegroundColor Yellow
$stOut = & python -m pytest tests\python\test_statable_psbproj_metrics.py -q 2>&1
$stExit = $LASTEXITCODE
$stOut | Where-Object { "$_".Trim() -ne "" } | Select-Object -Last 2 | ForEach-Object { Write-Host "  $_" }
if ($stExit -ne 0) { Write-Host "  exit=$stExit NG" -ForegroundColor Red } else { Write-Host "  exit=0 OK" }

# 4) E2E tests
Write-Host "[4/6] E2E TESTS" -ForegroundColor Yellow
Get-Process PowerSimBuilder -ErrorAction SilentlyContinue | Stop-Process -Force
# Run ALL UITests (SmokeTests + ProjectRoundTripTests + PythonEditorFormShownTests).
# xunit.runner.json disables parallelism; expect ~11 pass + 1 skip.
# Trap #63: previously filtered to ProjectRoundTripTests only (5/12); new E2E was silently skipped.
$testOut = & dotnet test tests\PowerSimBuilder.UITests --nologo 2>&1
$testExit = $LASTEXITCODE
$testOut | Where-Object { "$_".Trim() -ne "" } | Select-Object -Last 2 | ForEach-Object { Write-Host "  $_" }
if ($testExit -eq 0) { Write-Host "  exit=0 OK" -ForegroundColor Green } else { Write-Host "  exit=$testExit NG" -ForegroundColor Red }

# 5) Demo files
Write-Host "[5/6] DEMO FILES" -ForegroundColor Yellow
Get-ChildItem examples\demos | Select-Object Name, Length | ForEach-Object { Write-Host ("  {0,-32} {1,8}" -f $_.Name, $_.Length) }

# 6) Git status
Write-Host "[6/6] GIT STATUS" -ForegroundColor Yellow
$st = git status --short
if ($st) { $st | ForEach-Object { Write-Host "  $_" } } else { Write-Host "  clean" -ForegroundColor Green }
Write-Host "  HEAD: $(git rev-parse --short HEAD) $(git log -1 --pretty=%s)"

Write-Host ("=" * 60) -ForegroundColor Cyan
Write-Host "  DONE" -ForegroundColor Cyan
Write-Host ("=" * 60) -ForegroundColor Cyan
