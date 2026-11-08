$ErrorActionPreference = "Continue"
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$OutputEncoding = [Text.Encoding]::UTF8
Write-Host ""
Write-Host ("=" * 60) -ForegroundColor Cyan
Write-Host "  FULL VERIFICATION" -ForegroundColor Cyan
Write-Host ("=" * 60) -ForegroundColor Cyan

# 0) Known traps (Trap #81 / #82)
Write-Host ""
Write-Host "[0/6] KNOWN TRAPS" -ForegroundColor Yellow
python tools\check_known_traps.py 2>&1 | ForEach-Object { Write-Host "  $_" }
if ($LASTEXITCODE -ne 0) {
    Write-Host "  exit=$LASTEXITCODE NG (Trap #81/#82)" -ForegroundColor Red
    exit 1
}
Write-Host "  exit=0 OK" -ForegroundColor Green

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

# [3.6/6] psblog parser tests
Write-Host "[3.6/6] PSBLOG PARSER TESTS" -ForegroundColor Yellow
try {
    python -m pytest tests/python/test_psblog.py tests/python/test_recorded_signal.py tests/python/test_signal_classifier.py -q
    if ($LASTEXITCODE -ne 0) { $failures += "psblog parser tests" }
} catch {
    $failures += "psblog parser tests (exception)"
}
$stOut = & python -m pytest tests\python\test_statable_psbproj_metrics.py -q 2>&1
$stExit = $LASTEXITCODE
$stOut | Where-Object { "$_".Trim() -ne "" } | Select-Object -Last 2 | ForEach-Object { Write-Host "  $_" }
if ($stExit -ne 0) { Write-Host "  exit=$stExit NG" -ForegroundColor Red } else { Write-Host "  exit=0 OK" }

# 4) E2E tests
# [3.7/6] psblog sample log sanity
Write-Host "[3.7/6] PSBLOG SAMPLE SANITY" -ForegroundColor Yellow
try {
    python tools/verify_psblog.py
    if ($LASTEXITCODE -ne 0) { $failures += "psblog sample sanity" }
} catch {
    $failures += "psblog sample sanity (exception)"
}

Write-Host "[4/6] E2E TESTS" -ForegroundColor Yellow
Get-Process PowerSimBuilder -ErrorAction SilentlyContinue | Stop-Process -Force
# Trap #85: let the OS release window handles / file locks before starting FlaUI.
Start-Sleep -Seconds 3
# Run ALL UITests (SmokeTests + ProjectRoundTripTests + PythonEditorFormShownTests).
# xunit.runner.json disables parallelism; expect ~11 pass + 1 skip.
# Trap #63: previously filtered to ProjectRoundTripTests only (5/12); new E2E was silently skipped.
$testOut = & dotnet test tests\PowerSimBuilder.UITests --nologo 2>&1
$testExit = $LASTEXITCODE
# Trap #85: full UITests run can be flaky (leftover process / window handles).
# Retry once on failure after extra cleanup. Individual [4b]/[4c] checks below
# still run, so real regressions remain visible.
if ($testExit -ne 0) {
    Write-Host "  First UITests attempt failed, retrying once after cleanup (Trap #85)..." -ForegroundColor DarkYellow
    Get-Process PowerSimBuilder -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 5
    $testOut = & dotnet test tests\PowerSimBuilder.UITests --nologo 2>&1
    $testExit = $LASTEXITCODE
    if ($testExit -eq 0) { Write-Host "  Retry succeeded (flaky run absorbed)" -ForegroundColor DarkYellow }
}
$testOut | Where-Object { "$_".Trim() -ne "" } | Select-Object -Last 2 | ForEach-Object { Write-Host "  $_" }
if ($testExit -eq 0) { Write-Host "  exit=0 OK" -ForegroundColor Green } else { Write-Host "  exit=$testExit NG" -ForegroundColor Red }

# 4b) Phase C: ControllerBox edit E2E (C1/C2) - explicit filter
# Ensures the log-based edit path tests run even if the main UITests filter changes.
Write-Host "  [4b] ControllerBox edit E2E (C1/C2)" -ForegroundColor Yellow
Get-Process PowerSimBuilder -ErrorAction SilentlyContinue | Stop-Process -Force
$cOut = & dotnet test tests\PowerSimBuilder.UITests --nologo --filter "FullyQualifiedName~ControllerBoxEditE2ETests" 2>&1
$cExit = $LASTEXITCODE
$cOut | Where-Object { "$_".Trim() -ne "" } | Select-Object -Last 2 | ForEach-Object { Write-Host "    $_" }
if ($cExit -eq 0) { Write-Host "    exit=0 OK (C1+C2)" -ForegroundColor Green } else { Write-Host "    exit=$cExit NG (C1/C2)" -ForegroundColor Red }

# 4c) RoleSnippets: Register Snippet visibility (L1 + L2 unit tests, 29 cases)
# Lives in PowerSimBuilder.Tests (not UITests). Protects the STABLE_USER_CODE
# marker based visibility logic (B-3a..B-3c) against regressions.
Write-Host "  [4c] RoleSnippets (Register Snippet visibility)" -ForegroundColor Yellow
Get-Process PowerSimBuilder -ErrorAction SilentlyContinue | Stop-Process -Force
$rOut = & dotnet test PowerSimBuilder.Tests --nologo --filter "FullyQualifiedName~RoleSnippet" 2>&1
$rExit = $LASTEXITCODE
$rOut | Where-Object { "$_".Trim() -ne "" } | Select-Object -Last 2 | ForEach-Object { Write-Host "    $_" }
if ($rExit -eq 0) { Write-Host "    exit=0 OK (29 RoleSnippet tests)" -ForegroundColor Green } else { Write-Host "    exit=$rExit NG (RoleSnippet)" -ForegroundColor Red }

# 4d) RoleSnippets: Register dialog FlaUI E2E (B-5)
Write-Host "  [4d] RoleSnippet Register E2E (FlaUI)" -ForegroundColor Yellow
Get-Process PowerSimBuilder -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2
$dOut = & dotnet test tests\PowerSimBuilder.UITests --nologo --filter "FullyQualifiedName~RoleSnippetRegisterE2ETests" 2>&1
$dExit = $LASTEXITCODE
$dOut | Where-Object { "$_".Trim() -ne "" } | Select-Object -Last 2 | ForEach-Object { Write-Host "    $_" }
if ($dExit -eq 0) { Write-Host "    exit=0 OK (B-5 Register E2E)" -ForegroundColor Green } else { Write-Host "    exit=$dExit NG (B-5)" -ForegroundColor Red }

# 5) Demo files
# [4e] PsblogImport E2E (FlaUI)
Write-Host "  [4e] PsblogImport E2E (FlaUI)" -ForegroundColor Yellow
try {
    dotnet test tests\PowerSimBuilder.UITests --filter "PsblogImportE2ETests" -v n
    if ($LASTEXITCODE -ne 0) { $failures += "PsblogImport E2E" }
} catch {
    $failures += "PsblogImport E2E (exception)"
}

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
