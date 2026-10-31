$ErrorActionPreference = "Continue"
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$OutputEncoding = [Text.Encoding]::UTF8

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  LOCAL CI (Release build + tests) - CI substitute" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

$fail = 0

# 1) Release build
Write-Host ""
Write-Host "[1/4] Release build" -ForegroundColor Yellow
$buildOut = dotnet build PowerSimBuilder.sln -c Release --nologo 2>&1
$buildExit = $LASTEXITCODE
$errs = $buildOut | Where-Object { "$_" -match "error CS" } | Select-Object -First 5
if ($errs) { $errs | ForEach-Object { Write-Host "  $_" -ForegroundColor Red } }
Write-Host "  exit=$buildExit $(if($buildExit -eq 0){""OK""}else{""NG""})" -ForegroundColor $(if($buildExit -eq 0){"Green"}else{"Red"})
if ($buildExit -ne 0) { $fail++ }

# 2) Release test (Category!=UI)
Write-Host ""
Write-Host "[2/4] Release test (Category!=UI)" -ForegroundColor Yellow
$testOut = dotnet test PowerSimBuilder.Tests -c Release --no-build --filter "Category!=UI" --nologo 2>&1
$testExit = $LASTEXITCODE
$lastLines = $testOut | Where-Object { "$_".Trim() -ne "" } | Select-Object -Last 3
$lastLines | ForEach-Object { Write-Host "  $_" }
Write-Host "  exit=$testExit $(if($testExit -eq 0){""OK""}else{""NG""})" -ForegroundColor $(if($testExit -eq 0){"Green"}else{"Red"})
if ($testExit -ne 0) { $fail++ }

# 3) Debug E2E
Write-Host ""
Write-Host "[3/4] Debug E2E (ProjectRoundTrip)" -ForegroundColor Yellow
Get-Process PowerSimBuilder -ErrorAction SilentlyContinue | Stop-Process -Force
$e2eOut = dotnet test tests\PowerSimBuilder.UITests --filter "FullyQualifiedName~ProjectRoundTripTests" --nologo 2>&1
$e2eExit = $LASTEXITCODE
$e2eOut | Where-Object { "$_".Trim() -ne "" } | Select-Object -Last 2 | ForEach-Object { Write-Host "  $_" }
Write-Host "  exit=$e2eExit $(if($e2eExit -eq 0){""OK""}else{""NG""})" -ForegroundColor $(if($e2eExit -eq 0){"Green"}else{"Red"})
if ($e2eExit -ne 0) { $fail++ }

# 4) Python tests (pytest)
Write-Host ""
Write-Host "[4/4] Python tests (pytest)" -ForegroundColor Yellow
$pyOut = pytest tests/python -q 2>&1
$pyExit = $LASTEXITCODE
$pyOut | Select-Object -Last 3 | ForEach-Object { Write-Host "  $_" }
Write-Host "  exit=$pyExit $(if($pyExit -eq 0){""OK""}else{""NG""})" -ForegroundColor $(if($pyExit -eq 0){"Green"}else{"Red"})
if ($pyExit -ne 0) { $fail++ }

# Summary
Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
if ($fail -eq 0) {
    Write-Host "  LOCAL CI: ALL PASSED" -ForegroundColor Green
    exit 0
} else {
    Write-Host "  LOCAL CI: $fail FAILURE(S)" -ForegroundColor Red
    exit 1
}
