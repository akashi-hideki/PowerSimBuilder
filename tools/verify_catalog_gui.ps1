<#
  Verify generated_catalog.json reachability and emit a report block.
  Usage:
    .\verify_catalog_gui.ps1
    .\verify_catalog_gui.ps1 -SampleCount 3
    .\verify_catalog_gui.ps1 -CatalogPath Resources\generated_catalog.json
#>
param(
    [string]$CatalogPath = 'Resources\generated_catalog.json',
    [int]$SampleCount = 1,
    [int]$HttpPort = 8765
)
$ErrorActionPreference = 'Stop'
Set-Location 'C:\dev\PowerSimBuilder'

function Line { Write-Host ('=' * 64) -ForegroundColor DarkGray }
function OK   ($m) { Write-Host ('[OK]   ' + $m) -ForegroundColor Green }
function NG   ($m) { Write-Host ('[NG]   ' + $m) -ForegroundColor Red }
function Warn ($m) { Write-Host ('[WARN] ' + $m) -ForegroundColor Yellow }
function Info ($m) { Write-Host ('[INFO] ' + $m) -ForegroundColor Cyan }

# ---------- 1. Catalog file existence ----------
Line
Info ('Step 1: catalog file = {0}' -f $CatalogPath)
if (-not (Test-Path $CatalogPath)) { NG 'catalog file not found'; exit 1 }
$catSize = (Get-Item $CatalogPath).Length
OK ("file exists: {0} bytes" -f $catSize)

# ---------- 2. Parse catalog ----------
Line
Info 'Step 2: parse catalog JSON'
try {
    $cat = Get-Content $CatalogPath -Raw -Encoding UTF8 | ConvertFrom-Json
} catch {
    NG ("JSON parse failed: {0}" -f $_); exit 1
}
if (-not $cat.models) { NG 'no "models" array in catalog'; exit 1 }
$total = @($cat.models).Count
OK ("version={0}  source={1}" -f $cat.version, $cat.source)
OK ("generated_at={0}" -f $cat.generated_at)
OK ("models: {0}" -f $total)

# ---------- 3. Distribution by platform / FMI version ----------
Line
Info 'Step 3: distribution (description field)'
$byPlatform = @{}
$byFmiVer = @{}
foreach ($m in $cat.models) {
    if ($m.description -match 'FMI ([0-9.]+)\s*/\s*\S+\s*/\s*(\S+)\s*/') {
        $v = $matches[1]; $p = $matches[2]
        if (-not $byPlatform.ContainsKey($p)) { $byPlatform[$p] = 0 }
        $byPlatform[$p]++
        if (-not $byFmiVer.ContainsKey($v)) { $byFmiVer[$v] = 0 }
        $byFmiVer[$v]++
    }
}
foreach ($k in $byPlatform.Keys) { OK ("  platform {0,-8} : {1}" -f $k, $byPlatform[$k]) }
foreach ($k in $byFmiVer.Keys)   { OK ("  FMI {0,-8} : {1}" -f $k, $byFmiVer[$k]) }

# ---------- 4. URL head-check on samples ----------
Line
Info ('Step 4: HEAD check on {0} sample(s)' -f $SampleCount)
$sampleN = [Math]::Min($SampleCount, $total)
$failCount = 0
for ($i = 0; $i -lt $sampleN; $i++) {
    $m = $cat.models[$i]
    Write-Host ("  [{0}] {1}" -f $i, $m.name)
    Write-Host ("      {0}" -f $m.url)
    try {
        $r = Invoke-WebRequest -Uri $m.url -Method Head -UseBasicParsing -TimeoutSec 15 -ErrorAction Stop
        if ($r.StatusCode -eq 200) {
            OK ("      HTTP {0}" -f $r.StatusCode)
        } else {
            Warn ("      HTTP {0}" -f $r.StatusCode); $failCount++
        }
    } catch {
        NG ("      request failed: {0}" -f $_.Exception.Message); $failCount++
    }
}
if ($failCount -eq 0) { OK 'all sample URLs reachable' } else { Warn ("{0} sample(s) failed" -f $failCount) }

# ---------- 5. HTTP server reachability ----------
Line
Info ('Step 5: local HTTP server on port {0}' -f $HttpPort)
$serverOk = $false
try {
    $r = Invoke-WebRequest -Uri ("http://localhost:{0}/{1}" -f $HttpPort, ($CatalogPath -replace '\\','/')) -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
    if ($r.StatusCode -eq 200) { OK ("HTTP {0} (server running)" -f $r.StatusCode); $serverOk = $true }
    else { Warn ("HTTP {0}" -f $r.StatusCode) }
} catch {
    Warn 'server not running. Start in another terminal:'
    Write-Host "      cd C:\dev\PowerSimBuilder" -ForegroundColor DarkGray
    Write-Host ("      python -m http.server {0}" -f $HttpPort) -ForegroundColor DarkGray
}

# ---------- 6. Report ----------
Line
Write-Host '== REPORT (copy this block) ==' -ForegroundColor Magenta
Write-Host ''
Write-Host ("catalog_file      : {0} ({1} bytes)" -f $CatalogPath, $catSize)
Write-Host ("catalog_models    : {0}" -f $total)
Write-Host ("catalog_source    : {0}" -f $cat.source)
Write-Host ("catalog_generated : {0}" -f $cat.generated_at)
$platStr = ($byPlatform.GetEnumerator() | Sort-Object Name | ForEach-Object { "{0}={1}" -f $_.Key, $_.Value }) -join ', '
$fmiStr  = ($byFmiVer.GetEnumerator()  | Sort-Object Name | ForEach-Object { "{0}={1}" -f $_.Key, $_.Value }) -join ', '
Write-Host ("distribution      : FMI [{0}] / platform [{1}]" -f $fmiStr, $platStr)
Write-Host ("url_head_check    : {0}/{1} ok" -f ($sampleN - $failCount), $sampleN)
Write-Host ("http_server       : {0}" -f $(if ($serverOk) { "running" } else { "NOT running" }))
Write-Host ''
Write-Host '--- fill in after GUI check ---'
Write-Host 'gui_load          : __ models  (expected 50)'
Write-Host 'gui_download      : NetworkAccessDialog=__  FileTabSwitch=__'
Write-Host 'notes             :'
Write-Host ''
Write-Host '== END REPORT ==' -ForegroundColor Magenta