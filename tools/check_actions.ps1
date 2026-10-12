<#
.SYNOPSIS
    Check GitHub Actions runs for the current repository.

.DESCRIPTION
    Displays recent workflow runs, their status, and test results.

.PARAMETER Limit
    Number of recent runs to display (default: 5).

.PARAMETER Watch
    Watch the latest run until completion.

.EXAMPLE
    .\check_actions.ps1
    .\check_actions.ps1 -Limit 10
    .\check_actions.ps1 -Watch
#>
param(
    [int]$Limit = 5,
    [switch]$Watch
)

$ErrorActionPreference = "Stop"

# --- Repo root 検出 ---
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repo = Split-Path -Parent $scriptDir
Set-Location $repo

Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host " GitHub Actions Status" -ForegroundColor Cyan
Write-Host ("=" * 70) -ForegroundColor Cyan

# --- gh 存在確認 ---
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Host "❌ gh CLI が見つかりません。https://cli.github.com/ からインストールしてください。" -ForegroundColor Red
    exit 1
}

# --- 認証確認 ---
$authStatus = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ gh 未認証。'gh auth login' を実行してください。" -ForegroundColor Red
    Write-Host $authStatus
    exit 1
}
Write-Host "✅ gh authenticated" -ForegroundColor Green

# --- リポジトリ情報 ---
$repoInfo = gh repo view --json nameWithOwner | ConvertFrom-Json
Write-Host "   Repository: $($repoInfo.nameWithOwner)" -ForegroundColor Gray
Write-Host ""

# --- 最近の runs ---
Write-Host "=== Recent Runs (limit: $Limit) ===" -ForegroundColor Cyan
gh run list --limit $Limit `
    --json databaseId,displayTitle,status,conclusion,workflowName,headBranch,createdAt,event `
    | ConvertFrom-Json `
    | Format-Table -Property `
        @{N='ID';E={$_.databaseId}},
        @{N='Workflow';E={$_.workflowName}},
        @{N='Branch';E={$_.headBranch}},
        @{N='Event';E={$_.event}},
        @{N='Status';E={$_.status}},
        @{N='Conclusion';E={$_.conclusion}},
        @{N='Title';E={ if ($_.displayTitle.Length -gt 40) { $_.displayTitle.Substring(0,40) + '…' } else { $_.displayTitle } }} `
    -AutoSize

# --- 最新 run の詳細 ---
$latest = gh run list --limit 1 --json databaseId,status,conclusion,workflowName `
    | ConvertFrom-Json

if ($latest) {
    $latestId = $latest.databaseId
    Write-Host ""
    Write-Host "=== Latest Run Details (ID: $latestId) ===" -ForegroundColor Cyan
    Write-Host "   Workflow: $($latest.workflowName)"
    Write-Host "   Status:   $($latest.status)"
    Write-Host "   Conclusion: $($latest.conclusion)"

    # --- Job 単位のステータス ---
    Write-Host ""
    Write-Host "   Jobs:" -ForegroundColor Cyan
    gh run view $latestId --json jobs `
        | ConvertFrom-Json `
        | ForEach-Object { $_.jobs } `
        | Format-Table -Property `
            @{N='Job';E={$_.name}},
            @{N='Status';E={$_.status}},
            @{N='Conclusion';E={$_.conclusion}},
            @{N='Duration';E={
                if ($_.startedAt -and $_.completedAt) {
                    $ts = ([datetime]$_.completedAt) - ([datetime]$_.startedAt)
                    "{0:mm\:ss}" -f $ts
                } else { '-' }
            }} `
        -AutoSize
}

# --- Watch モード ---
if ($Watch) {
    Write-Host ""
    Write-Host "=== Watching latest run ($latestId) ===" -ForegroundColor Cyan
    gh run watch $latestId --exit-status
    $exitCode = $LASTEXITCODE

    Write-Host ""
    if ($exitCode -eq 0) {
        Write-Host "✅ Run succeeded" -ForegroundColor Green
    } else {
        Write-Host "❌ Run failed (exit=$exitCode)" -ForegroundColor Red
        Write-Host ""
        Write-Host "--- Failed step logs ---" -ForegroundColor Yellow
        gh run view $latestId --log-failed 2>&1 | Select-Object -Last 60
    }
}

Write-Host ""
Write-Host ("=" * 70) -ForegroundColor Cyan
Write-Host " Open in browser: gh run view --web" -ForegroundColor Gray
Write-Host ("=" * 70) -ForegroundColor Cyan
