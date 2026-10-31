<#
.SYNOPSIS psb.ps1 - safe file edit + finish + status
.EXAMPLE  .\tools\psb.ps1 status
.EXAMPLE  .\tools\psb.ps1 finish "docs: update handoff"
.EXAMPLE  .\tools\psb.ps1 replace file.cs "old text" "new text"
.EXAMPLE  .\tools\psb.ps1 insert-after file.md "anchor line" "new line"
.EXAMPLE  .\tools\psb.ps1 show file.md
#>
param(
    [Parameter(Position=0, Mandatory=$true)][ValidateSet("status","finish","replace","insert-after","show")][string]$Command,
    [Parameter(Position=1)][string]$Arg1,
    [Parameter(Position=2)][string]$Arg2,
    [Parameter(Position=3)][string]$Arg3
)
$ErrorActionPreference = "Stop"
Set-Location (Split-Path -Parent $PSScriptRoot)

function Read-Utf8([string]$path) {
    $full = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $path))
    [System.IO.File]::ReadAllText($full, [System.Text.Encoding]::UTF8)
}
function Write-Utf8Bom([string]$path, [string]$text) {
    $full = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $path))
    [System.IO.File]::WriteAllText($full, $text, (New-Object System.Text.UTF8Encoding($true)))
}

switch ($Command) {
    "status" { & "$PSScriptRoot\status_check.ps1" }
    "finish" { & "$PSScriptRoot\finish.ps1" $Arg1 }
    "show"   { Read-Utf8 $Arg1 | Out-Host }
    "replace" {
        $text = Read-Utf8 $Arg1
        if (-not $text.Contains($Arg2)) { Write-Host "NG: pattern not found in $Arg1" -ForegroundColor Red; exit 1 }
        $count = ([regex]::Matches($text, [regex]::Escape($Arg2))).Count
        $new = $text.Replace($Arg2, $Arg3)
        Write-Utf8Bom $Arg1 $new
        Write-Host "replace OK: $Arg1 ($count replacement, BOM saved)" -ForegroundColor Green
    }
    "insert-after" {
        $text = Read-Utf8 $Arg1
        $pattern = "(?m)^" + [regex]::Escape($Arg2) + ".*$"
        if ($text -notmatch $pattern) { Write-Host "NG: anchor not found: $Arg2" -ForegroundColor Red; exit 1 }
        $new = [regex]::Replace($text, $pattern, "`$0`r`n$Arg3", 1)
        Write-Utf8Bom $Arg1 $new
        Write-Host "insert-after OK: $Arg1" -ForegroundColor Green
    }
}
