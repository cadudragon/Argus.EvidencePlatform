param(
    [string]$BaseUrl = "http://localhost:5058",
    [int]$ReadyRetryCount = 30,
    [int]$ReadyRetryDelaySeconds = 2
)

$ErrorActionPreference = "Stop"

Write-Host "Running local E2E against $BaseUrl"

for ($attempt = 1; $attempt -le $ReadyRetryCount; $attempt++) {
    try {
        Invoke-RestMethod -Method Get -Uri "$BaseUrl/health/ready" | Out-Null
        break
    }
    catch {
        if ($attempt -eq $ReadyRetryCount) {
            throw "API did not become ready at $BaseUrl after $ReadyRetryCount attempts."
        }

        Start-Sleep -Seconds $ReadyRetryDelaySeconds
    }
}

$case = Invoke-RestMethod `
    -Method Post `
    -Uri "$BaseUrl/api/cases" `
    -ContentType "application/json" `
    -Body (@{
        externalCaseId = "CASE-E2E-$(Get-Date -Format 'yyyyMMddHHmmss')"
        title = "Caso E2E Local"
        description = "Validacao local do backend"
    } | ConvertTo-Json)

$caseId = $case.id
Write-Host "Created case $caseId"

$tmp = Join-Path $env:TEMP "argus-e2e-evidence.txt"
Set-Content -Path $tmp -Value "sample evidence $(Get-Date -Format o)"

try {
    Add-Type -AssemblyName System.Net.Http

    $multipart = New-Object System.Net.Http.MultipartFormDataContent

    $stringParts = @{
        caseId = $caseId
        sourceId = "local-source"
        evidenceType = "Document"
        classification = "test"
        captureTimestamp = (Get-Date).ToUniversalTime().ToString("o")
    }

    foreach ($entry in $stringParts.GetEnumerator()) {
        $multipart.Add(
            (New-Object System.Net.Http.StringContent($entry.Value)),
            $entry.Key)
    }

    $fileStream = [System.IO.File]::OpenRead($tmp)

    try {
        $fileContent = New-Object System.Net.Http.StreamContent($fileStream)
        $fileContent.Headers.ContentType = [System.Net.Http.Headers.MediaTypeHeaderValue]::Parse("text/plain")
        $multipart.Add($fileContent, "file", [System.IO.Path]::GetFileName($tmp))

        $httpClient = New-Object System.Net.Http.HttpClient

        try {
            $response = $httpClient.PostAsync(
                "$BaseUrl/api/evidence/artifacts",
                $multipart).GetAwaiter().GetResult()

            [void]$response.EnsureSuccessStatusCode()
            $artifact = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult() | ConvertFrom-Json
        }
        finally {
            $httpClient.Dispose()
        }
    }
    finally {
        $fileStream.Dispose()
        $multipart.Dispose()
    }

    Write-Host "Accepted evidence $($artifact.evidenceId)"

    $timeline = Invoke-RestMethod `
        -Method Get `
        -Uri "$BaseUrl/api/evidence/cases/$caseId/timeline"

    Write-Host "Timeline items: $($timeline.Count)"

    $export = Invoke-RestMethod `
        -Method Post `
        -Uri "$BaseUrl/api/exports" `
        -ContentType "application/json" `
        -Body (@{
            caseId = $caseId
            format = "zip"
            reason = "local e2e"
        } | ConvertTo-Json)

    Write-Host "Queued export job $($export.id)"

    $exportJob = Invoke-RestMethod `
        -Method Get `
        -Uri "$BaseUrl/api/exports/$($export.id)"

    Write-Host "Export job status: $($exportJob.status)"

    $audit = Invoke-RestMethod `
        -Method Get `
        -Uri "$BaseUrl/api/audit/cases/$caseId"

    Write-Host "Audit entries: $($audit.Count)"
    $audit | Select-Object occurredAt, action, actorId | Format-Table -AutoSize
}
finally {
    Remove-Item -LiteralPath $tmp -ErrorAction SilentlyContinue
}
