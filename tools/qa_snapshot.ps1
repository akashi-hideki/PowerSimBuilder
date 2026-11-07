<#
.SYNOPSIS
    One-action quantitative QA snapshot for GO/NO-GO.

.DESCRIPTION
    Collects all quantitative evidence into docs/qa/<Label>/.
    Full-resolution CSV goes to tmp/qa_full/ (gitignored).

.EXAMPLE
    .\tools\qa_snapshot.ps1 -DryRun
    .\tools\qa_snapshot.ps1 -SkipVerifyAll
    .\tools\qa_snapshot.ps1 -Label 2026-11-08-gonogo
#>
param(
    [string]$Label = (Get-Date -Format "yyyy-MM-dd"),
    [string]$Project = "examples\demos\oven_control_statable.psbproj",
    [string]$Target  = "ovenchamber.outputs.oven_current_temp=200",
    [double]$CsvHz   = 1.0,
    [switch]$SkipVerifyAll,
    [switch]$Force,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------
# Run python with 2>&1 safely (native stderr under $ErrorActionPreference=Stop
# becomes a terminating NativeCommandError on PS 5.1). Returns stdout+stderr
# lines; exits 1 on failure unless -AllowNonZero.
# ---------------------------------------------------------------------------
function Invoke-PythonChecked {
    param(
        [Parameter(Mandatory)] [string[]]$PythonArgs,
        [Parameter(Mandatory)] [string]$StepLabel,
        [switch]$AllowNonZero
    )
    $prevEAP = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $output = & python @PythonArgs 2>&1
        $exit = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $prevEAP
    }
    if ($exit -ne 0 -and -not $AllowNonZero) {
        $output | Select-Object -Last 10 | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
        Write-Host "[ERR] $StepLabel (exit=$exit)" -ForegroundColor Red
        exit 1
    }
    return $output
}

# --- locate repo root ---
$repoRoot = $PSScriptRoot
while ($repoRoot -and -not (Test-Path (Join-Path $repoRoot "PowerSimBuilder.sln"))) {
    $parent = Split-Path $repoRoot -Parent
    if (-not $parent -or $parent -eq $repoRoot) { break }
    $repoRoot = $parent
}
if (-not $repoRoot -or -not (Test-Path (Join-Path $repoRoot "PowerSimBuilder.sln"))) {
    Write-Host "[ERR] PowerSimBuilder.sln not found from $PSScriptRoot" -ForegroundColor Red
    exit 1
}

Push-Location $repoRoot
try {
    $qaDir = "docs\qa\$Label"
    $fullDir = "tmp\qa_full"
    $fullCsv = Join-Path $fullDir "simulation_signals_full.csv"

    if ((Test-Path $qaDir) -and -not $Force -and -not $DryRun) {
        Write-Host "[ERR] $qaDir already exists. Use -Force." -ForegroundColor Red
        exit 1
    }
    if (-not (Test-Path $Project)) {
        Write-Host "[ERR] project not found: $Project" -ForegroundColor Red
        exit 1
    }
    if (-not (Test-Path "generated\demo_main.py")) {
        Write-Host "[ERR] generated\demo_main.py not found. Run Create Simulation in the GUI first." -ForegroundColor Red
        exit 1
    }

    Write-Host "============================================================" -ForegroundColor Cyan
    Write-Host "  QA SNAPSHOT: $qaDir" -ForegroundColor Cyan
    Write-Host "  Project : $Project" -ForegroundColor Cyan
    Write-Host "  Target  : $Target" -ForegroundColor Cyan
    Write-Host "  CsvHz   : $CsvHz" -ForegroundColor Cyan
    Write-Host "  Mode    : $(if ($DryRun) {'DRY-RUN'} elseif ($SkipVerifyAll) {'FAST'} else {'FULL'})" -ForegroundColor Cyan
    Write-Host "============================================================" -ForegroundColor Cyan

    if (-not $DryRun) {
        New-Item -ItemType Directory -Path $qaDir -Force | Out-Null
        New-Item -ItemType Directory -Path $fullDir -Force | Out-Null
    }

    # --- [1/8] patch demo_main.py ---
    Write-Host "`n[1/8] Ensure PSB_DUMP_CSV in demo_main.py" -ForegroundColor Yellow
    if ($DryRun) {
        Write-Host "  [DRY] python tools\patch_demo_main_csv.py --apply" -ForegroundColor DarkYellow
    } else {
        $out = Invoke-PythonChecked -PythonArgs @("tools\patch_demo_main_csv.py", "--apply") -StepLabel "patch failed"
        $out | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
    }

    # --- [2/8] environment ---
    Write-Host "[2/8] Environment snapshot" -ForegroundColor Yellow
    if ($DryRun) {
        Write-Host "  [DRY] python tools\capture_env.py $qaDir\environment.json" -ForegroundColor DarkYellow
    } else {
        $null = Invoke-PythonChecked -PythonArgs @("tools\capture_env.py", "$qaDir\environment.json") -StepLabel "capture_env failed"
        $envJson = Get-Content "$qaDir\environment.json" -Raw | ConvertFrom-Json
        $tagDisp = if ([string]::IsNullOrEmpty($envJson.git_tag)) { "(no tag)" } else { $envJson.git_tag }
        Write-Host "  commit=$($envJson.git_commit.Substring(0,7)) tag=$tagDisp dirty=$($envJson.git_dirty)" -ForegroundColor Gray
    }

    # --- [3/8] headless demo_main.py ---
    Write-Host "[3/8] Run demo_main.py (headless, CSV+PNG)" -ForegroundColor Yellow
    if ($DryRun) {
        Write-Host "  [DRY] PSB_DUMP_CSV=$fullCsv MPLBACKEND=Agg; cd generated; python demo_main.py" -ForegroundColor DarkYellow
    } else {
        $prevCsv  = $env:PSB_DUMP_CSV
        $prevMpl  = $env:MPLBACKEND
        $prevWarn = $env:PYTHONWARNINGS
        $env:PSB_DUMP_CSV = (Resolve-Path $fullDir).Path + "\simulation_signals_full.csv"
        $env:MPLBACKEND = "Agg"
        $env:PYTHONWARNINGS = "ignore"
        Push-Location generated
        try {
            $out = Invoke-PythonChecked -PythonArgs @("demo_main.py") -StepLabel "demo_main.py failed"
            $out | Select-Object -Last 3 | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
        } finally {
            Pop-Location
            $env:PSB_DUMP_CSV = $prevCsv
            $env:MPLBACKEND = $prevMpl
            $env:PYTHONWARNINGS = $prevWarn
        }
        if (-not (Test-Path $fullCsv)) {
            Write-Host "[ERR] full CSV not produced: $fullCsv" -ForegroundColor Red
            exit 1
        }
        $sizeMB = [int]((Get-Item $fullCsv).Length / 1MB)
        Write-Host "  [OK] full CSV: ${sizeMB} MB" -ForegroundColor Green
    }

    # --- [4/8] downsample ---
    Write-Host "[4/8] Downsample -> $CsvHz Hz" -ForegroundColor Yellow
    if ($DryRun) {
        Write-Host "  [DRY] python tools\downsample_csv.py $fullCsv $qaDir\simulation_signals_1hz.csv $CsvHz" -ForegroundColor DarkYellow
    } else {
        $out = Invoke-PythonChecked -PythonArgs @("tools\downsample_csv.py", $fullCsv, "$qaDir\simulation_signals_1hz.csv", "$CsvHz") -StepLabel "downsample failed"
        $out | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
    }

    # --- [5/8] verify_statable_psbproj ---
    Write-Host "[5/8] .psbproj direct metrics" -ForegroundColor Yellow
    if ($DryRun) {
        Write-Host "  [DRY] python tools\verify_statable_psbproj.py $Project > $qaDir\statable_psbproj.txt" -ForegroundColor DarkYellow
    } else {
        $out = Invoke-PythonChecked -PythonArgs @("tools\verify_statable_psbproj.py", $Project) -StepLabel "verify_statable_psbproj failed"
        $out | Set-Content -Path "$qaDir\statable_psbproj.txt" -Encoding UTF8
        $m = ($out | Out-String) | Select-String -Pattern "oven_current_temp\s+([\d.]+)\s+(-?[\d.]+)\s+([\d.]+)\s+([\d.]+)\s+([\d.]+)" | Select-Object -First 1
        if ($m) { Write-Host "  ss=$($m.Matches[0].Groups[1].Value) ovs=$($m.Matches[0].Groups[4].Value)% settle=$($m.Matches[0].Groups[5].Value)s" -ForegroundColor Green }
    }

    # --- [6/8] analyze_sim_csv ---
    Write-Host "[6/8] CSV-recomputed metrics" -ForegroundColor Yellow
    if ($DryRun) {
        Write-Host "  [DRY] python tools\analyze_sim_csv.py $qaDir\simulation_signals_1hz.csv --target $Target > $qaDir\metrics.txt" -ForegroundColor DarkYellow
    } else {
        $out = Invoke-PythonChecked -PythonArgs @("tools\analyze_sim_csv.py", "$qaDir\simulation_signals_1hz.csv", "--target", $Target) -StepLabel "analyze_sim_csv failed"
        $out | Set-Content -Path "$qaDir\metrics.txt" -Encoding UTF8
    }

    # --- [7/8] controller_box_gating ---
    Write-Host "[7/8] ControllerBox gating verification" -ForegroundColor Yellow
    if ($DryRun) {
        Write-Host "  [DRY] python tools\verify_controller_box.py $Project > $qaDir\controller_box_gating.txt" -ForegroundColor DarkYellow
    } else {
        $out = Invoke-PythonChecked -PythonArgs @("tools\verify_controller_box.py", $Project) -StepLabel "verify_controller_box failed"
        $out | Set-Content -Path "$qaDir\controller_box_gating.txt" -Encoding UTF8
        $g = ($out | Out-String) | Select-String -Pattern "\[PASS\]" | Select-Object -First 1
        if ($g) { Write-Host "  [OK] $($g.Line.Trim())" -ForegroundColor Green }
    }

    # --- [8/8] verify_all ---
    Write-Host "[8/8] verify_all.ps1 (full test suite)" -ForegroundColor Yellow
    if ($SkipVerifyAll) {
        Write-Host "  [SKIP] -SkipVerifyAll" -ForegroundColor DarkYellow
    } elseif ($DryRun) {
        Write-Host "  [DRY] .\tools\verify_all.ps1 > $qaDir\verify_all.log" -ForegroundColor DarkYellow
    } else {
        Get-Process PowerSimBuilder -ErrorAction SilentlyContinue | Stop-Process -Force
        $prevEAP = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            $vaOut = & .\tools\verify_all.ps1 2>&1
            $vaExit = $LASTEXITCODE
        } finally {
            $ErrorActionPreference = $prevEAP
        }
        $vaOut | Set-Content -Path "$qaDir\verify_all.log" -Encoding UTF8
        if ($vaExit -ne 0) {
            Write-Host "  [WARN] verify_all exit=$vaExit (recorded in log, not failing snapshot)" -ForegroundColor DarkYellow
        } else {
            Write-Host "  [OK] verify_all exit=0" -ForegroundColor Green
        }
    }

    # --- assemble ---
    if (-not $DryRun) {
        if (Test-Path "generated\simulation_signals.png") {
            Copy-Item "generated\simulation_signals.png" $qaDir -Force
            Write-Host "[final] simulation_signals.png" -ForegroundColor Yellow
        }

        $readme = @"
# QA Snapshot - $Label

Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
Project: $Project
Target: $Target
CSV: $CsvHz Hz

## Contents
| File | Source |
|---|---|
| environment.json | tools/capture_env.py |
| statable_psbproj.txt | tools/verify_statable_psbproj.py |
| metrics.txt | tools/analyze_sim_csv.py |
| controller_box_gating.txt | tools/verify_controller_box.py |
| simulation_signals_1hz.csv | tools/downsample_csv.py |
| simulation_signals.png | generated/simulation_signals.png |
| verify_all.log | tools/verify_all.ps1 |

## Reproduce
    .\tools\qa_snapshot.ps1 -Label $Label -Force
"@
        Set-Content -Path "$qaDir\README.md" -Value $readme -Encoding UTF8

        $gi = Get-Content .gitignore -Raw -Encoding UTF8
        if ($gi -notmatch "docs/qa/\*\*/simulation_signals_full.csv") {
            Add-Content -Path .gitignore -Value "`n# QA: full-resolution CSV (regenerable)`ndocs/qa/**/simulation_signals_full.csv" -Encoding UTF8
        }

        Write-Host ""
        Write-Host "============================================================" -ForegroundColor Green
        Write-Host "  DONE: $qaDir" -ForegroundColor Green
        Write-Host "============================================================" -ForegroundColor Green
        Get-ChildItem $qaDir -File | Select-Object Name, @{N='Size';E={"{0:N0}" -f $_.Length}} | Format-Table -AutoSize
    }
} finally {
    Pop-Location
}
exit 0
