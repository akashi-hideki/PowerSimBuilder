# tools\status_check.ps1
# One-shot project status dashboard. Run this first in any session.

# UTF-8 output (trap #37 display fix)
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$OutputEncoding = [System.Text.Encoding]::UTF8

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

function Show-Section($title) {
    Write-Host ""
    Write-Host ("=" * 60)
    Write-Host " $title"
    Write-Host ("=" * 60)
}

# 1. git
Show-Section "1. GIT"
& git status --short
Write-Host "HEAD: $(git rev-parse --short HEAD) $(git log -1 --pretty=%s)"
$branch = git rev-parse --abbrev-ref HEAD
$upstream = git rev-parse --abbrev-ref '@{upstream}' 2>$null
if ($upstream) {
    $ahead = git rev-list --count "@{upstream}..HEAD" 2>$null
    $behind = git rev-list --count "HEAD..@{upstream}" 2>$null
    Write-Host "Branch: $branch / upstream: $upstream / ahead: $ahead / behind: $behind"
}
Write-Host "Latest tag: $(git describe --tags --abbrev=0 2>$null)"

# 2. build
Show-Section "2. BUILD"
$buildOut = & dotnet build --nologo 2>&1
$buildExit = $LASTEXITCODE
$buildOut | Select-String "error CS|Build succeeded|成功|エラー" | Select-Object -First 3
Write-Host "Build exit: $buildExit"

# 3. tests (E2E)
Show-Section "3. E2E TESTS (ProjectRoundTrip)"
Get-Process PowerSimBuilder -ErrorAction SilentlyContinue | Stop-Process -Force
$testOut = & dotnet test tests\PowerSimBuilder.UITests --filter "FullyQualifiedName~ProjectRoundTripTests" --nologo 2>&1
$testExit = $LASTEXITCODE
$testOut | Where-Object { "$_".Trim() -ne "" } | Select-Object -Last 3 | ForEach-Object { Write-Host $_ }
Write-Host "Test exit: $testExit"

# 4. demo assets
Show-Section "4. DEMO ASSETS (examples/demos/)"
if (Test-Path examples\demos) {
    Get-ChildItem examples\demos | Select-Object Name, Length | Format-Table -AutoSize | Out-String | Write-Host
}

# 5. workspace .psbproj (multi-path, includes OneDrive redirect)
Show-Section "5. WORKSPACE .psbproj (Documents + OneDrive)"
$candidateDirs = @(
    (Join-Path $env:USERPROFILE "Documents\PowerSimBuilder\workspace\projects"),
    (Join-Path $env:USERPROFILE "OneDrive\ドキュメント\PowerSimBuilder\workspace\projects"),
    (Join-Path $env:USERPROFILE "OneDrive\Documents\PowerSimBuilder\workspace\projects"),
    (Join-Path $repoRoot "workspace\projects")
)
$anyPsbproj = $false
foreach ($d in $candidateDirs) {
    if (Test-Path $d) {
        $items = @(Get-ChildItem -Path $d -Filter "*.psbproj" -ErrorAction SilentlyContinue)
        if ($items.Count -gt 0) {
            Write-Host "  $d"
            $items | ForEach-Object { Write-Host ("    {0} ({1} bytes)" -f $_.Name, $_.Length); $anyPsbproj = $true }
        }
    }
}
if (-not $anyPsbproj) { Write-Host "  (none)" }

# 6. generated/
Show-Section "6. GENERATED (generated/)"
if (Test-Path generated) {
    Get-ChildItem generated -File -ErrorAction SilentlyContinue |
        Select-Object Name, Length, LastWriteTime |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 8 |
        Format-Table -AutoSize | Out-String | Write-Host
}

# 7. processes
Show-Section "7. RUNNING PROCESSES"
$psb = Get-Process PowerSimBuilder -ErrorAction SilentlyContinue
if ($psb) { $psb | Select-Object Id, StartTime | Format-Table | Out-String | Write-Host } else { Write-Host "  PowerSimBuilder: not running" }
$py = Get-Process python -ErrorAction SilentlyContinue
if ($py) { $py | Select-Object Id, StartTime | Format-Table | Out-String | Write-Host } else { Write-Host "  python: not running" }

Write-Host ""
Write-Host "DONE."