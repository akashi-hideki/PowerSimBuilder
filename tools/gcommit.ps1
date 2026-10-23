# tools/gcommit.ps1
# Pre-commit safe commit wrapper.
#
# Usage:
#   .\tools\gcommit.ps1 "commit message"              # stage all, run pre-commit, commit
#   .\tools\gcommit.ps1 "commit message" -Push        # + push to origin
#   .\tools\gcommit.ps1 "commit message" -Amend       # amend previous commit
#   .\tools\gcommit.ps1 "msg" -Files a.cs,b.cs        # stage only specified files
#   .\tools\gcommit.ps1 "msg" -SkipPrecommit          # skip pre-commit (emergency)
#
# Behaviour:
#   1. git add (all, or -Files)
#   2. pre-commit run   (auto-fixes are written to disk)
#   3. git add again    (picks up auto-fixes)
#   4. git commit -m
#   5. optional: git push

param(
    [Parameter(Mandatory=$true, Position=0)]
    [string]$Message,

    [switch]$Push,
    [switch]$Amend,
    [switch]$SkipPrecommit,
    [string[]]$Files
)

$ErrorActionPreference = 'Stop'

function Fail([string]$message, [int]$code = 1) {
    Write-Host ""
    Write-Host "[FAIL] $message" -ForegroundColor Red
    exit $code
}

function Step([string]$label) {
    Write-Host ""
    Write-Host "== $label" -ForegroundColor Cyan
}

# 0. sanity
if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Fail "git is not installed or not on PATH"
}
if (-not (Test-Path (Join-Path (Get-Location) '.git'))) {
    Fail "not a git repository: $(Get-Location)"
}

# 1. stage
Step "1/5 stage"
if ($Files) {
    Write-Host "git add $($Files -join ' ')"
    & git add -- @Files
} else {
    Write-Host "git add -A"
    & git add -A
}
if ($LASTEXITCODE -ne 0) { Fail "git add failed" }

# 2. pre-commit run
if (-not $SkipPrecommit) {
    $pre = Get-Command pre-commit -ErrorAction SilentlyContinue
    if ($pre) {
        Step "2/5 pre-commit run"
        & pre-commit run 2>&1 | ForEach-Object { Write-Host $_ }
        # exit code may be 1 due to auto-fix; that's fine
        Write-Host "pre-commit finished (auto-fixes may have been applied)"
    } else {
        Write-Host "[SKIP] pre-commit not installed"
    }
} else {
    Write-Host "[SKIP] -SkipPrecommit"
}

# 3. re-stage auto-fixes
Step "3/5 re-stage"
& git add -A
if ($LASTEXITCODE -ne 0) { Fail "git add (restage) failed" }

# check staged
$staged = & git diff --cached --name-only
if (-not $staged) {
    Write-Host "[INFO] nothing staged, exiting" -ForegroundColor Yellow
    exit 0
}

# 4. commit
Step "4/5 commit"
$commitArgs = @('commit', '-m', $Message)
if ($Amend) { $commitArgs = @('commit', '--amend', '-m', $Message) }
& git @commitArgs
if ($LASTEXITCODE -ne 0) { Fail "git commit failed (exit $LASTEXITCODE)" }

# 5. push
if ($Push) {
    Step "5/5 push"
    & git push
    if ($LASTEXITCODE -ne 0) { Fail "git push failed (exit $LASTEXITCODE)" }
}

Write-Host ""
Write-Host "[OK] done" -ForegroundColor Green
