$ErrorActionPreference = "Continue"
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$OutputEncoding = [Text.Encoding]::UTF8

$fail = 0
function Sec($t) { Write-Host ""; Write-Host ("=" * 60) -ForegroundColor Cyan; Write-Host " $t" -ForegroundColor Cyan; Write-Host ("=" * 60) -ForegroundColor Cyan }
function Ok($m) { Write-Host "  [OK] $m" -ForegroundColor Green }
function Ng($m) { Write-Host "  [NG] $m" -ForegroundColor Red; $script:fail++ }
function Warn($m) { Write-Host "  [!!] $m" -ForegroundColor Yellow }

Sec "1. Visibility"
$vis = gh repo view akashi-hideki/PowerSimBuilder --json visibility --jq ".visibility" 2>$null
Write-Host "  visibility: $vis"
if ($vis -eq "PRIVATE") { Ok "PRIVATE (safe to proceed)" } else { Warn "already $vis" }

Sec "2. Secret scan (tracked files, real token patterns only)"
# Real GitHub token formats: ghp_xxx (36+), gho_xxx, github_pat_xxx (long)
# Exclude setup_github_token.py (it validates tokens, not stores them)
$realTokenPattern = "ghp_[A-Za-z0-9]{36,}|gho_[A-Za-z0-9]{36,}|github_pat_[A-Za-z0-9_]{50,}|AKIA[0-9A-Z]{16}"
$files = git ls-files | Where-Object { $_ -notmatch "setup_github_token\.py$" -and $_ -match "\.(py|cs|json|yml|yaml|md|txt|ps1|sh)$" }
$found = @()
foreach ($f in $files) {
    if (-not (Test-Path -LiteralPath $f)) { continue }
    try {
        $hits = Select-String -LiteralPath $f -Pattern $realTokenPattern -ErrorAction Stop
        if ($hits) { $found += $hits }
    } catch { }
}
if ($found.Count -gt 0) { $found | ForEach-Object { Ng "$($_.Path):$($_.LineNumber)  $($_.Line.Trim())" } }
else { Ok "no real token patterns in tracked files (excluded setup_github_token.py)" }

Sec "3. detect-secrets baseline"
if (Test-Path .secrets.baseline) {
    try {
        $sb = Get-Content .secrets.baseline -Raw | ConvertFrom-Json
        $cnt = 0
        if ($sb.results) { $cnt = ($sb.results.PSObject.Properties | Measure-Object).Count }
        Write-Host "  entries: $cnt"
        if ($cnt -eq 0) { Ok "no findings" } else { Warn "$cnt entries (verify manually)" }
    } catch { Warn "cannot parse" }
} else { Warn ".secrets.baseline not present (optional tool)" }

Sec "4. user_* tracking"
$u1 = git ls-files user_physics_models/ 2>$null
$u2 = git ls-files user_components/ 2>$null
if ($u1) { Ng "user_physics_models tracked: $($u1 -join ", ")" } else { Ok "user_physics_models not tracked" }
if ($u2) { Ng "user_components tracked: $($u2 -join ", ")" } else { Ok "user_components not tracked" }

Sec "5. Author emails"
$authors = git log --all --format="%ae" | Sort-Object -Unique
$authors | ForEach-Object { Write-Host "  $_" }
$gmail = $authors | Where-Object { $_ -match "@gmail\.com$" }
if ($gmail) { Warn "gmail author present (same as StaTable, OK)" } else { Ok "no gmail authors" }

Sec "6. StaTable author (reference)"
$st = gh api repos/akashi-hideki/StaTable/commits --jq ".[0].commit.author.email" 2>$null
Write-Host "  StaTable latest author: $st"
if ($st -eq "akashi.hideki@gmail.com") { Ok "PowerSimBuilder gmail matches StaTable policy" }

Sec "7. LAWYER_CONSULTATION history"
$lc = git log --all --full-history --oneline -- "docs/ip/LAWYER_CONSULTATION.md" 2>$null
if ($lc) { Ng "found in history" } else { Ok "not in history" }

Sec "8. Sensitive files tracked"
$sens = git ls-files | Where-Object { $_ -match "LAWYER_CONSULTATION|\.env$|credential|\.pem$|\.key$|settings\.json$" }
if ($sens) { $sens | ForEach-Object { Ng $_ } } else { Ok "none" }

Sec "9. Large files tracked (>500KB)"
# Use git cat-file -s on each object, no Test-Path (avoids Japanese path issues)
$big = @()
git ls-tree -r -l HEAD | ForEach-Object {
    $parts = $_ -split "\s+", 5
    if ($parts.Length -ge 5) {
        $size = [int]$parts[3]
        $path = $parts[4]
        if ($size -gt 500000) { $big += "{0,10:N0}  {1}" -f $size, $path }
    }
}
if ($big.Count -gt 0) { $big | ForEach-Object { Warn $_ } } else { Ok "no files >500KB in HEAD" }

Sec "10. .gitignore key patterns"
$giLines = Get-Content .gitignore
$needles = @("LAWYER_CONSULTATION", "user_physics_models/", "user_components/", "workspace/projects", "generated/temp_*.py")
foreach ($n in $needles) {
    $hit = $giLines | Where-Object { $_.Trim() -eq $n -or $_.Trim() -like "*$n*" }
    if ($hit) { Ok "present: $n" } else { Warn "missing: $n" }
}

Write-Host ""
Write-Host ("=" * 60) -ForegroundColor Cyan
if ($fail -eq 0) {
    Write-Host "  RESULT: OK to proceed with Public visibility" -ForegroundColor Green
    exit 0
} else {
    Write-Host "  RESULT: $fail CRITICAL issue(s)" -ForegroundColor Red
    exit 1
}
