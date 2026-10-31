$ErrorActionPreference = "Continue"
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$OutputEncoding = [Text.Encoding]::UTF8

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  QA Day 3: Error Handling + Regression" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan

$exe = ".\bin\Debug\net10.0-windows\PowerSimBuilder.exe"
$logDir = "tmp\qa_day3"
$results = @()

if (-not (Test-Path $exe)) {
    Write-Host "ERROR: exe not found. Run build first." -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $logDir)) { New-Item -ItemType Directory -Path $logDir -Force | Out-Null }
Get-ChildItem $logDir -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

# ============================================================
# [1/5] Malformed .psbproj handling
# ============================================================
Write-Host ""
Write-Host "[1/5] Malformed .psbproj handling" -ForegroundColor Yellow

# Case A: non-existent file
$case = "A_nonexistent"
$in = "$logDir\nope.psbproj"
$out = "$logDir\nope_out.psbproj"
Write-Host "  $case..." -NoNewline
$proc = Start-Process -FilePath $exe -ArgumentList @("--test-load-save", $in, $out) -NoNewWindow -PassThru -Wait -RedirectStandardOutput "$logDir\$case.stdout.txt" -RedirectStandardError "$logDir\$case.stderr.txt"
$log = "$logDir\..\..\PowerSimBuilder_TestLoad.log"
$srcLog = Join-Path $env:TEMP "PowerSimBuilder_TestLoad.log"
$outLog = "$logDir\$case.outlog.txt"
if (Test-Path $srcLog) { Copy-Item $srcLog $outLog -Force }
$txt = if (Test-Path $outLog) { Get-Content $outLog -Raw } else { "" }
$ok = ($txt -match "Test_LoadProjectFromPath (COMPLETE|FAILED)")
$results += [PSCustomObject]@{ Case=$case; Pass=$ok; Note=if($ok){"no crash"}else{"NO LOG"} }
Write-Host $(if($ok){" OK"}else{" NG"}) -ForegroundColor $(if($ok){"Green"}else{"Red"})

# Case B: empty file
$case = "B_empty"
$in = "$logDir\empty.psbproj"
$out = "$logDir\empty_out.psbproj"
New-Item -ItemType File -Path $in -Force | Out-Null
Write-Host "  $case..." -NoNewline
if (Test-Path $srcLog) { Remove-Item $srcLog -Force }
$proc = Start-Process -FilePath $exe -ArgumentList @("--test-load-save", $in, $out) -NoNewWindow -PassThru -Wait -RedirectStandardOutput "$logDir\$case.stdout.txt" -RedirectStandardError "$logDir\$case.stderr.txt"
$outLog = "$logDir\$case.outlog.txt"
if (Test-Path $srcLog) { Copy-Item $srcLog $outLog -Force }
$txt = if (Test-Path $outLog) { Get-Content $outLog -Raw } else { "" }
$ok = ($txt -match "Test_LoadProjectFromPath (COMPLETE|FAILED)")
$results += [PSCustomObject]@{ Case=$case; Pass=$ok; Note=if($ok){"no crash"}else{"NO LOG"} }
Write-Host $(if($ok){" OK"}else{" NG"}) -ForegroundColor $(if($ok){"Green"}else{"Red"})

# Case C: non-ZIP text file
$case = "C_notzip"
$in = "$logDir\notzip.psbproj"
$out = "$logDir\notzip_out.psbproj"
"this is not a zip file" | Set-Content -Path $in -Encoding UTF8
Write-Host "  $case..." -NoNewline
if (Test-Path $srcLog) { Remove-Item $srcLog -Force }
$proc = Start-Process -FilePath $exe -ArgumentList @("--test-load-save", $in, $out) -NoNewWindow -PassThru -Wait -RedirectStandardOutput "$logDir\$case.stdout.txt" -RedirectStandardError "$logDir\$case.stderr.txt"
$outLog = "$logDir\$case.outlog.txt"
if (Test-Path $srcLog) { Copy-Item $srcLog $outLog -Force }
$txt = if (Test-Path $outLog) { Get-Content $outLog -Raw } else { "" }
$ok = ($txt -match "Test_LoadProjectFromPath (COMPLETE|FAILED)")
$results += [PSCustomObject]@{ Case=$case; Pass=$ok; Note=if($ok){"no crash"}else{"NO LOG"} }
Write-Host $(if($ok){" OK"}else{" NG"}) -ForegroundColor $(if($ok){"Green"}else{"Red"})

# Case D: ZIP without project.json
$case = "D_no_projectjson"
$in = "$logDir\nojson.psbproj"
$out = "$logDir\nojson_out.psbproj"
python tmp\mk_nojson.py $in
Write-Host "  $case..." -NoNewline
if (Test-Path $srcLog) { Remove-Item $srcLog -Force }
$proc = Start-Process -FilePath $exe -ArgumentList @("--test-load-save", $in, $out) -NoNewWindow -PassThru -Wait -RedirectStandardOutput "$logDir\$case.stdout.txt" -RedirectStandardError "$logDir\$case.stderr.txt"
$outLog = "$logDir\$case.outlog.txt"
if (Test-Path $srcLog) { Copy-Item $srcLog $outLog -Force }
$txt = if (Test-Path $outLog) { Get-Content $outLog -Raw } else { "" }
$ok = ($txt -match "Test_LoadProjectFromPath (COMPLETE|FAILED)")
$results += [PSCustomObject]@{ Case=$case; Pass=$ok; Note=if($ok){"no crash"}else{"NO LOG"} }
Write-Host $(if($ok){" OK"}else{" NG"}) -ForegroundColor $(if($ok){"Green"}else{"Red"})

# Case E: ZIP with malformed JSON
$case = "E_bad_json"
$in = "$logDir\badjson.psbproj"
$out = "$logDir\badjson_out.psbproj"
python tmp\mk_badjson.py $in
Write-Host "  $case..." -NoNewline
if (Test-Path $srcLog) { Remove-Item $srcLog -Force }
$proc = Start-Process -FilePath $exe -ArgumentList @("--test-load-save", $in, $out) -NoNewWindow -PassThru -Wait -RedirectStandardOutput "$logDir\$case.stdout.txt" -RedirectStandardError "$logDir\$case.stderr.txt"
$outLog = "$logDir\$case.outlog.txt"
if (Test-Path $srcLog) { Copy-Item $srcLog $outLog -Force }
$txt = if (Test-Path $outLog) { Get-Content $outLog -Raw } else { "" }
$ok = ($txt -match "Test_LoadProjectFromPath (COMPLETE|FAILED)")
$results += [PSCustomObject]@{ Case=$case; Pass=$ok; Note=if($ok){"no crash"}else{"NO LOG"} }
Write-Host $(if($ok){" OK"}else{" NG"}) -ForegroundColor $(if($ok){"Green"}else{"Red"})

# ============================================================
# [2/5] Empty project save
# ============================================================
Write-Host ""
Write-Host "[2/5] Empty project save" -ForegroundColor Yellow
$out = "$logDir\empty_project.psbproj"
Write-Host "  save empty..." -NoNewline
$proc = Start-Process -FilePath $exe -ArgumentList @("--test-save-project", $out) -NoNewWindow -PassThru -Wait -RedirectStandardOutput "$logDir\empty.stdout.txt" -RedirectStandardError "$logDir\empty.stderr.txt"
$ok = (Test-Path $out) -and ((Get-Item $out).Length -gt 0)
$results += [PSCustomObject]@{ Case="F_empty_save"; Pass=$ok; Note=if($ok){"file created"}else{"missing"} }
Write-Host $(if($ok){" OK"}else{" NG"}) -ForegroundColor $(if($ok){"Green"}else{"Red"})

# ============================================================
# [3/5] Python execution error
# ============================================================
Write-Host ""
Write-Host "[3/5] Python execution error handling" -ForegroundColor Yellow
$badPy = "$logDir\bad_syntax.py"
"def broken(:`n    pass" | Set-Content -Path $badPy -Encoding UTF8
$pyResult = python $badPy 2>&1
$pyExit = $LASTEXITCODE
$ok = ($pyExit -ne 0)
$results += [PSCustomObject]@{ Case="G_python_error"; Pass=$ok; Note="exit=$pyExit" }
Write-Host "  python syntax error -> exit=$pyExit $(if($ok){""OK""}else{""NG""})" -ForegroundColor $(if($ok){"Green"}else{"Red"})

# ============================================================
# [4/5] Full regression (Category!=UI)
# ============================================================
Write-Host ""
Write-Host "[4/5] Full regression (Category!=UI)" -ForegroundColor Yellow
$testOut = & dotnet test PowerSimBuilder.Tests --filter "Category!=UI" --nologo 2>&1
$testExit = $LASTEXITCODE
$lastLines = $testOut | Where-Object { "$_".Trim() -ne "" } | Select-Object -Last 3
$lastLines | ForEach-Object { Write-Host "  $_" }
$ok = ($testExit -eq 0)
$results += [PSCustomObject]@{ Case="H_full_regression"; Pass=$ok; Note="exit=$testExit" }
Write-Host "  exit=$testExit $(if($ok){""OK""}else{""NG""})" -ForegroundColor $(if($ok){"Green"}else{"Red"})

# ============================================================
# [5/5] Summary
# ============================================================
Write-Host ""
Write-Host "[5/5] SUMMARY" -ForegroundColor Yellow
$pass = @($results | Where-Object { $_.Pass }).Count
$total = $results.Count
Write-Host ""
Write-Host "  Cases: $pass / $total passed" -ForegroundColor $(if($pass -eq $total){"Green"}else{"Red"})
$results | ForEach-Object {
    $color = if ($_.Pass) { "Green" } else { "Red" }
    $mark = if ($_.Pass) { "OK" } else { "NG" }
    Write-Host ("    [{0}] {1,-22} {2}" -f $mark, $_.Case, $_.Note) -ForegroundColor $color
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
if ($pass -eq $total) {
    Write-Host "  ALL PASSED" -ForegroundColor Green
    exit 0
} else {
    Write-Host "  SOME FAILED" -ForegroundColor Red
    exit 1
}
