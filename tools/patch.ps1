<#
.SYNOPSIS  Find/Replace でファイル編集（BOM/EOL 保持、冪等、diff 表示）
.EXAMPLE   .\tools\patch.ps1 -Path X -Find Y -Replace Z
.EXAMPLE   .\tools\patch.ps1 -Path X -Find Y -Replace Z -Preview
#>
param(
    [Parameter(Mandatory=$true)][string]$Path,
    [Parameter(Mandatory=$true)][string]$Find,
    [Parameter(Mandatory=$true)][string]$Replace,
    [switch]$Preview
)
$ErrorActionPreference = "Stop"
$p = (Resolve-Path $Path).Path
$bytes = [IO.File]::ReadAllBytes($p)
$hasBom = ($bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF)
$enc = New-Object System.Text.UTF8Encoding($hasBom)
$txt = [IO.File]::ReadAllText($p, [Text.Encoding]::UTF8)
if ($txt.IndexOf($Find, [StringComparison]::Ordinal) -lt 0) {
    Write-Host "[patch] SKIP (not found, already applied?): $Path" -ForegroundColor Yellow
    exit 0
}
$new = $txt.Replace($Find, $Replace)
$tmp = Join-Path $env:TEMP ("patch_" + [guid]::NewGuid().ToString("N") + ".tmp")
[IO.File]::WriteAllText($tmp, $new, $enc)
git --no-pager diff --no-index -- $p $tmp
Remove-Item $tmp -Force -ErrorAction SilentlyContinue
if (-not $Preview) {
    [IO.File]::WriteAllText($p, $new, $enc)
    Write-Host "[patch] Applied (BOM=$hasBom)" -ForegroundColor Green
}
