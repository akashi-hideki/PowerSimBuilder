$ErrorActionPreference = "Continue"
[Console]::OutputEncoding = [Text.Encoding]::UTF8
$OutputEncoding = [Text.Encoding]::UTF8

$fail = 0
function Sec($t) { Write-Host ""; Write-Host ("=" * 60) -ForegroundColor Cyan; Write-Host " $t" -ForegroundColor Cyan; Write-Host ("=" * 60) -ForegroundColor Cyan }
function Ok($m) { Write-Host "  [OK] $m" -ForegroundColor Green }
function Ng($m) { Write-Host "  [NG] $m" -ForegroundColor Red; $script:fail++ }
function Warn($m) { Write-Host "  [!!] $m" -ForegroundColor Yellow }

# 1. Visibility
Sec "1. Current visibility"
$vis = gh repo view akashi-hideki/PowerSimBuilder --json visibility --jq ".visibility" 2>$null
Write-Host "  visibility: $vis"
if ($vis -eq "PRIVATE") { Ok "repo is PRIVATE (safe to proceed)" } else { Warn "repo is already $vis" }

# 2. Secret scan in tracked files
Sec "2. Secret scan (tracked files)"
$hits = Select-String -Path tools\setup_github_token.py -Pattern "ghp_|gho_|github_pat_|AKIA[0-9A-Z]{16}|secret\s*=\s*['""][^'""]{10,}" -ErrorAction SilentlyContinue
if ($hits) { $hits | ForEach-Object { Ng ("$($_.Path):$($_.LineNumber)  $($_.Line.Trim())") } } else { Ok "no hardcoded tokens in setup_github_token.py" }

# 3. .secrets.baseline
Sec "3. .secrets.baseline (detect-secrets)"
if (Test-Path .secrets.baseline) {
    try {
        $sb = Get-Content .secrets.baseline -Raw | ConvertFrom-Json
        $cnt = 0
        if ($sb.results) { $cnt = ($sb.results.PSObject.Properties | Measure-Object).Count }
        Write-Host "  entries: $cnt"
        if ($cnt -eq 0) { Ok "no findings" } else { $sb.results.PSObject.Properties | ForEach-Object { $p = $_.Name; $_.Value | ForEach-Object { Warn "$p : $($_.type) (line $($_.line_number))" } } }
    } catch { Warn "cannot parse: $_" }
} else { Warn ".secrets.baseline not found" }

# 4. user_* tracking
Sec "4. user_physics_models / user_components tracking"
$u1 = git ls-files user_physics_models/ 2>$null
$u2 = git ls-files user_components/ 2>$null
if ($u1) { Ng "user_physics_models tracked: $($u1 -join ', ')" } else { Ok "user_physics_models not tracked" }
if ($u2) { $files = $u2 -split "`n" | Where-Object { $_ -notmatch "\.gitkeep$" }
    if ($files) { Warn "user_components tracked: $($files -join ', ')" } else { Ok "user_components: only .gitkeep" }
} else { Ok "user_components not tracked" }

# 5. Author emails
Sec "5. Author emails (commit history)"
$authors = git log --all --format="%ae" | Sort-Object -Unique
$authors | ForEach-Object { Write-Host "  $_" }
$gmail = $authors | Where-Object { $_ -match "@gmail\.com$" }
if ($gmail) { Warn "gmail author(s) present: $($gmail -join ', ')  (compare with StaTable)" } else { Ok "no gmail authors" }

# 6. StaTable author (comparison)
Sec "6. StaTable author email (for comparison)"
$st = gh api repos/akashi-hideki/StaTable/commits --jq ".[0].commit.author.email" 2>$null
Write-Host "  StaTable latest author: $st"

# 7. LAWYER_CONSULTATION history
Sec "7. LAWYER_CONSULTATION history"
$lc = git log --all --full-history --oneline -- "docs/ip/LAWYER_CONSULTATION.md" 2>$null
if ($lc) { Ng "LAWYER_CONSULTATION found in history: $lc" } else { Ok "LAWYER_CONSULTATION not in history" }

# 8. Sensitive files currently tracked
Sec "8. Sensitive files currently tracked"
$sens = git ls-files | Select-String -Pattern "LAWYER_CONSULTATION|\.env$|secret|credential|\.pem$|\.key$|settings\.json$"
if ($sens) { $sens | ForEach-Object { Ng $_.Line } } else { Ok "no sensitive files tracked" }

# 9. Large binary files tracked (FMU etc.)
Sec "9. Binary files tracked (>500KB)"
$big = git ls-files | ForEach-Object { if (Test-Path $_) { $f = Get-Item $_; if ($f.Length -gt 500000) { "{0}  ({1:N0} bytes)" -f $_, $f.Length } } }
if ($big) { $big | ForEach-Object { Warn $_ } } else { Ok "no files >500KB tracked" }

# 10. .gitignore key patterns
Sec "10. .gitignore key patterns"
$gi = Get-Content .gitignore -Raw
$patterns = @("LAWYER_CONSULTATION","user_physics_models","workspace/projects","generated/\*_powersim","temp_\*\.py")
foreach ($p in $patterns) { if ($gi -match [regex]::Escape($p)) { Ok "ignored: $p" } else { Warn "NOT ignored: $p" } }

# Summary
Write-Host ""
Write-Host ("=" * 60) -ForegroundColor Cyan
if ($fail -eq 0) {
    Write-Host "  RESULT: OK to proceed with Public visibility" -ForegroundColor Green
    Write-Host "  (Review warnings above before proceeding)" -ForegroundColor Yellow
    exit 0
} else {
    Write-Host "  RESULT: $fail CRITICAL issue(s) - DO NOT make public yet" -ForegroundColor Red
    exit 1
}
