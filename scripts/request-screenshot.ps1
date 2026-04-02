param(
    [Parameter(Mandatory = $true)]
    [string]$DeviceId,

    [string]$BaseUrl = "http://192.168.1.68:5058"
)

$ErrorActionPreference = "Stop"

$body = @{
    deviceId = $DeviceId
} | ConvertTo-Json

$response = Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/api/device-commands/screenshot" `
    -ContentType "application/json" `
    -Body $body

$response | ConvertTo-Json -Depth 10
