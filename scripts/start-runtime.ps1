param()

$ErrorActionPreference = "Stop"

$docker = "C:\Program Files\Docker\Docker\resources\bin\docker.exe"
$dotnet = "C:\Program Files\dotnet\dotnet.exe"
$composeFile = "C:\Src\Argus.EvidencePlatform\compose.yaml"
$project = "C:\Src\Argus.EvidencePlatform\src\Argus.EvidencePlatform.Api\Argus.EvidencePlatform.Api.csproj"
$stdout = "C:\Src\argus-api.stdout.log"
$stderr = "C:\Src\argus-api.stderr.log"

& $docker compose -f $composeFile up -d postgres azurite

$existing = Get-NetTCPConnection -LocalPort 5058 -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1
if ($existing) {
    Stop-Process -Id $existing.OwningProcess -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

if (Test-Path $stdout) { Remove-Item $stdout -Force }
if (Test-Path $stderr) { Remove-Item $stderr -Force }

$env:ASPNETCORE_URLS = "http://0.0.0.0:5058"
$env:ASPNETCORE_ENVIRONMENT = "Development"

$process = Start-Process -FilePath $dotnet `
    -ArgumentList @("run", "--no-launch-profile", "--project", $project) `
    -WorkingDirectory "C:\Src\Argus.EvidencePlatform" `
    -RedirectStandardOutput $stdout `
    -RedirectStandardError $stderr `
    -PassThru

Write-Output $process.Id
