<#
.SYNOPSIS  PowerSimBuilder launcher (build + kill + start + optional catalog server)
.EXAMPLE   .\tools\launch.ps1
.EXAMPLE   .\tools\launch.ps1 -Build -KillExisting
.EXAMPLE   .\tools\launch.ps1 -Mode statable
.EXAMPLE   .\tools\launch.ps1 -Mode statable -XmlPath tests\data\scenarios\cooking_heater\design.xml
.EXAMPLE   .\tools\launch.ps1 -Build -ServeCatalog
#>
param(
    [ValidateSet("main","statable","batch")]
    [string]$Mode = "main",
    [string]$XmlPath,
    [switch]$Build,
    [switch]$KillExisting,
    [switch]$ServeCatalog,
    [switch]$NoWait
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$exe = Join-Path $repoRoot "bin\Debug\net10.0-windows\PowerSimBuilder.exe"
if (-not (Test-Path $exe)) {
    Write-Host "[launch] exe not found, building first..." -ForegroundColor Yellow
    $Build = $true
}

# 1) Kill existing
if ($KillExisting) {
    $procs = Get-Process PowerSimBuilder -ErrorAction SilentlyContinue
    if ($procs) {
        Write-Host "[launch] stopping $($procs.Count) PowerSimBuilder process(es)" -ForegroundColor Yellow
        $procs | Stop-Process -Force
        Start-Sleep -Milliseconds 500
    }
}

# 2) Build
if ($Build) {
    Write-Host "[launch] dotnet build..." -ForegroundColor Cyan
    $out = & dotnet build PowerSimBuilder.csproj --nologo 2>&1
    $exit = $LASTEXITCODE
    $out | Where-Object { "$_" -match "error|Build succeeded|成功|エラー" } | Select-Object -First 10 | ForEach-Object { Write-Host "  $_" }
    if ($exit -ne 0) {
        Write-Host "[launch] build FAILED (exit=$exit)" -ForegroundColor Red
        exit $exit
    }
    Write-Host "[launch] build OK" -ForegroundColor Green
}

# 3) Optional catalog HTTP server (new window)
if ($ServeCatalog) {
    Write-Host "[launch] starting catalog server on :8765 (Resources/)" -ForegroundColor Cyan
    Start-Process powershell -ArgumentList @(
        "-NoExit", "-Command",
        "cd '$repoRoot'; Write-Host 'Catalog: http://localhost:8765/Resources/generated_catalog.json'; python -m http.server 8765"
    )
    Start-Sleep -Seconds 2
}

# 4) Build args by mode
$argsList = @()
switch ($Mode) {
    "main" {
        Write-Host "[launch] starting MainForm" -ForegroundColor Cyan
    }
    "statable" {
        if ($XmlPath) {
            $resolved = (Resolve-Path $XmlPath -ErrorAction Stop).Path
            $out = Join-Path $repoRoot "generated\controller_box_powersim.py"
            $argsList = @("--statable-import", $resolved, $out)
            Write-Host "[launch] StaTable batch: $resolved -> $out" -ForegroundColor Cyan
        } else {
            $argsList = @("--statable-import")
            Write-Host "[launch] StaTableImportForm GUI" -ForegroundColor Cyan
        }
    }
    "batch" {
        if (-not $XmlPath) { Write-Host "[launch] -Mode batch requires -XmlPath" -ForegroundColor Red; exit 2 }
        $resolved = (Resolve-Path $XmlPath -ErrorAction Stop).Path
        $out = Join-Path $repoRoot "generated\controller_box_powersim.py"
        $argsList = @("--statable-import", $resolved, $out)
        Write-Host "[launch] batch: $resolved -> $out" -ForegroundColor Cyan
    }
}

# 5) Launch
Write-Host "[launch] exe: $exe"
if ($argsList.Count -gt 0) { Write-Host "[launch] args: $($argsList -join ' ')" }

if ($Mode -eq "batch") {
    # batch mode: run synchronously, print exit code
    & $exe @argsList
    Write-Host "[launch] exit=$LASTEXITCODE" -ForegroundColor $(if ($LASTEXITCODE -eq 0) { "Green" } else { "Red" })
    exit $LASTEXITCODE
}
else {
    if ($argsList.Count -gt 0) {
        Start-Process -FilePath $exe -ArgumentList $argsList
    } else {
        Start-Process -FilePath $exe
    }
    if (-not $NoWait) {
        Write-Host "[launch] started (GUI)" -ForegroundColor Green
    }
}
