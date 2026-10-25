<#
  Merge fmpy + NuGet license CSVs into licenses_full.csv, fixing 4 known entries.
  Verifies no GPL/AGPL contamination.
#>
$ErrorActionPreference = 'Stop'
Set-Location 'C:\dev\PowerSimBuilder'

function OK   ($m) { Write-Host ('[OK]   ' + $m) -ForegroundColor Green }
function NG   ($m) { Write-Host ('[NG]   ' + $m) -ForegroundColor Red; exit 1 }
function Info ($m) { Write-Host ('[INFO] ' + $m) -ForegroundColor Cyan }

$pyCsv    = 'docs\ip\licenses_python_fmpy.csv'
$nugetCsv = 'docs\ip\licenses_nuget.csv'
$fullCsv  = 'docs\ip\licenses_full.csv'

# ---------- 1. Fix known entries ----------
$fixes = @{
    '"FMPy","0.3.32","UNKNOWN","UNKNOWN"' =
        '"FMPy","0.3.32","BSD-2-Clause","https://github.com/CATIA-Systems/FMPy"'
    '"NCalc","1.3.8","http://ncalc.codeplex.com/license","https://github.com/sheetsync/NCalc"' =
        '"NCalc","1.3.8","MIT","https://github.com/sheetsync/NCalc"'
    '"Pythonnet","3.0.5","See LICENSE","https://pythonnet.github.io/"' =
        '"Pythonnet","3.0.5","MIT","https://github.com/pythonnet/pythonnet"'
    '"Fody","6.8.2","See License.txt","https://github.com/Fody/Fody"' =
        '"Fody","6.8.2","MIT","https://github.com/Fody/Fody"'
}

Info 'Step 1: fixing 4 known license entries'

$pyContent    = Get-Content $pyCsv -Raw -Encoding UTF8
$nugetContent = Get-Content $nugetCsv -Raw -Encoding UTF8

foreach ($from in $fixes.Keys) {
    $to = $fixes[$from]
    if ($pyContent.Contains($from)) {
        $pyContent = $pyContent.Replace($from, $to)
        OK ("fixed in fmpy: {0}" -f $from.Split(',')[0])
    }
    if ($nugetContent.Contains($from)) {
        $nugetContent = $nugetContent.Replace($from, $to)
        OK ("fixed in nuget: {0}" -f $from.Split(',')[0])
    }
}

Set-Content $pyCsv    -Value $pyContent    -Encoding UTF8 -NoNewline
Set-Content $nugetCsv -Value $nugetContent -Encoding UTF8 -NoNewline

# ---------- 2. Merge ----------
Info 'Step 2: merging into licenses_full.csv'

$nugetLines = Get-Content $nugetCsv
$pyLines    = Get-Content $pyCsv

$merged = @($nugetLines[0])
$merged += $nugetLines[1..($nugetLines.Count-1)]
$merged += $pyLines[1..($pyLines.Count-1)]

$merged | Out-File $fullCsv -Encoding UTF8 -NoNewline:$false

$total = (Get-Content $fullCsv).Count - 1
OK ("merged: {0} entries ({1} NuGet + {2} Python)" -f $total, ($nugetLines.Count-1), ($pyLines.Count-1))

# ---------- 3. Verify no GPL/AGPL/LGPL ----------
Info 'Step 3: checking for GPL/AGPL/LGPL contamination'

$gplHits = Select-String -Path $fullCsv -Pattern '\b(AGPL|GPL|LGPL)\b' -CaseSensitive:$false
if ($gplHits) {
    Write-Host '[NG] GPL-family detected:' -ForegroundColor Red
    $gplHits | ForEach-Object { Write-Host $_ }
} else {
    OK 'No GPL/AGPL/LGPL in licenses_full.csv'
}

# ---------- 4. Distribution ----------
Info 'Step 4: license distribution'

Get-Content $fullCsv | Select-Object -Skip 1 | ForEach-Object {
    $parts = $_ -split '","'
    if ($parts.Count -ge 3) { $parts[2] }
} | Group-Object | Sort-Object Count -Descending | ForEach-Object {
    Write-Host ("  {0,3}  {1}" -f $_.Count, $_.Name)
}

# ---------- 5. Summary ----------
Write-Host ''
Write-Host '=== licenses_full.csv ===' -ForegroundColor Cyan
Get-Content $fullCsv