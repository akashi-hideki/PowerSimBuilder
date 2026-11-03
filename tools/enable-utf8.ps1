# tools\enable-utf8.ps1
#
# Enable UTF-8 for the current PowerShell session (console + .NET).
#
# Usage:
#   .\tools\enable-utf8.ps1            # enable for this session
#   .\tools\enable-utf8.ps1 -Status    # show current encodings
#   .\tools\enable-utf8.ps1 -Persist   # enable + add to PS profile
#   .\tools\enable-utf8.ps1 -Restore   # revert to CP932 (Shift-JIS)
#
# Rationale: PowerShell 5.1 defaults to CP932; dotnet test / Get-Content
# emit UTF-8 bytes that then appear as mojibake. This tool switches the
# console codepage and the .NET Console encodings in one step.
[CmdletBinding()]
param(
    [switch]$Persist,
    [switch]$Status,
    [switch]$Restore,
    [switch]$Quiet
)

$ErrorActionPreference = "Stop"

function Write-Info { param($m) if (-not $Quiet) { Write-Host $m -ForegroundColor Cyan } }
function Write-Ok   { param($m) if (-not $Quiet) { Write-Host $m -ForegroundColor Green } }

if ($Status) {
    $cp = (& chcp) -replace "\D",""
    Write-Info "Console code page : $cp"
    Write-Info "Console output    : $([Console]::OutputEncoding.WebName)"
    Write-Info "Console input     : $([Console]::InputEncoding.WebName)"
    Write-Info "`$OutputEncoding  : $($OutputEncoding.WebName)"
    return
}

if ($Restore) {
    chcp 932 | Out-Null
    $cp932 = [System.Text.Encoding]::GetEncoding(932)
    [Console]::OutputEncoding = $cp932
    [Console]::InputEncoding  = $cp932
    Set-Variable -Name OutputEncoding -Value $cp932 -Scope Global -Force
    Write-Ok "Restored to CP932 (Shift-JIS)"
    return
}

$u8 = [System.Text.Encoding]::UTF8
try { chcp 65001 | Out-Null } catch { Write-Warning "chcp 65001 failed: $($_.Exception.Message)" }
try { [Console]::OutputEncoding = $u8 } catch { Write-Warning "OutputEncoding: $($_.Exception.Message)" }
try { [Console]::InputEncoding  = $u8 } catch { Write-Warning "InputEncoding: $($_.Exception.Message)" }
Set-Variable -Name OutputEncoding -Value $u8 -Scope Global -Force
Write-Ok "UTF-8 console enabled (CP65001)"

if ($Persist) {
    $profilePath = $PROFILE.CurrentUserAllHosts
    $dir = Split-Path $profilePath -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    $m0 = "# >>> psb-utf8 >>>"
    $m1 = "# <<< psb-utf8 <<<"
    $block = @(
        $m0
        "# Added by tools\enable-utf8.ps1 (PowerSimBuilder)"
        "chcp 65001 | Out-Null"
        "[Console]::OutputEncoding = [System.Text.Encoding]::UTF8"
        "[Console]::InputEncoding  = [System.Text.Encoding]::UTF8"
        "Set-Variable -Name OutputEncoding -Value ([System.Text.Encoding]::UTF8) -Scope Global -Force"
        $m1
    ) -join "`r`n"
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    if (Test-Path $profilePath) {
        $existing = [System.IO.File]::ReadAllText($profilePath, $utf8NoBom)
        if ($existing -match [regex]::Escape($m0)) {
            Write-Info "Profile already has psb-utf8 block: $profilePath"
        } else {
            [System.IO.File]::AppendAllText($profilePath, "`r`n" + $block + "`r`n", $utf8NoBom)
            Write-Ok "Profile updated: $profilePath"
        }
    } else {
        [System.IO.File]::WriteAllText($profilePath, $block + "`r`n", $utf8NoBom)
        Write-Ok "Profile created: $profilePath"
    }
    Write-Info "Restart PowerShell to apply permanently."
}
