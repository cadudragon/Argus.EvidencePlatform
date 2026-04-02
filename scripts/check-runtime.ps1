param(
    [string]$BaseUrl = "http://192.168.1.68:5058"
)

$ErrorActionPreference = "Continue"

Write-Host "=== LISTEN ==="
Get-NetTCPConnection -LocalPort 5058 -State Listen -ErrorAction SilentlyContinue |
    Select-Object LocalAddress, LocalPort, OwningProcess

Write-Host ""
Write-Host "=== HEALTH ==="
Invoke-WebRequest -UseBasicParsing "$BaseUrl/health/live" |
    Select-Object StatusCode, StatusDescription, Content

Write-Host ""
Write-Host "=== STDERR ==="
if (Test-Path "C:\Src\argus-api.stderr.log") {
    Get-Content "C:\Src\argus-api.stderr.log" -Tail 200
}
