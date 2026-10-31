param([switch]$NoKill)
if (-not $NoKill) { Get-Process PowerSimBuilder -ErrorAction SilentlyContinue | Stop-Process -Force }
$out = & dotnet build --nologo 2>&1
$exit = $LASTEXITCODE
$out | Where-Object { "$_" -match "error|warning CS|Build succeeded|成功|エラー" } | Select-Object -First 20 | ForEach-Object { Write-Host $_ }
Write-Host "Build exit: $exit"
exit $exit
