<#
.SYNOPSIS  pre-commit -> add -> commit(retry) -> push -> status を 1 コマンドで。
.EXAMPLE   .\tools\finish.ps1 "docs(handoff): update THREAD_HANDOFF"
.EXAMPLE   .\tools\finish.ps1 "wip: test" -NoPush -SkipStatus
#>
param(
    [Parameter(Mandatory=$true, Position=0)][string]$Message,
    [switch]$NoPush,
    [switch]$SkipStatus
)

$ErrorActionPreference = "Stop"

# 0) 変更有無
$changed = @(git status --porcelain)
if ($changed.Count -eq 0) {
    Write-Host "[finish] 変更なし。commit をスキップ。" -ForegroundColor Yellow
    if (-not $NoPush) { git push origin main }
    if (-not $SkipStatus) { & "$PSScriptRoot\status_check.ps1" }
    return
}

# 1) pre-commit (auto-fix) を先に走らせる
$files = @(git diff --name-only) + @(git diff --cached --name-only) + @(git ls-files --others --exclude-standard)
$files = @($files | Sort-Object -Unique | Where-Object { $_ })
if ($files.Count -gt 0) {
    Write-Host "[finish] pre-commit on $($files.Count) file(s)..." -ForegroundColor Cyan
    pre-commit run --files $files | Out-Host
}

# 2) commit (最大3回リトライ: 罠 #4)
$ok = $false
for ($i = 1; $i -le 3; $i++) {
    git add -A
    git commit -m $Message
    if ($LASTEXITCODE -eq 0) { Write-Host "[finish] commit 成功 ($i)" -ForegroundColor Green; $ok = $true; break }
    Write-Host "[finish] commit 失敗 ($i) -> 再 stage で再試行" -ForegroundColor Yellow
    Start-Sleep -Seconds 1
}
if (-not $ok) { Write-Host "[finish] commit 失敗。手動確認を。" -ForegroundColor Red; exit 1 }

# 3) push
if (-not $NoPush) {
    Write-Host "[finish] push origin main..." -ForegroundColor Cyan
    git push origin main
    if ($LASTEXITCODE -ne 0) { Write-Host "[finish] push 失敗" -ForegroundColor Red; exit 1 }
}

# 4) log + status
git log --oneline -3 | Out-Host
git status --short | Out-Host

if (-not $SkipStatus) { & "$PSScriptRoot\status_check.ps1" }
