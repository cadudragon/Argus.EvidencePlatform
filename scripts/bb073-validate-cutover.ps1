param(
    [string]$SourceDirectory = "C:\Src\bb073-cutover\argus_evidence_platform",
    [string]$TargetDatabaseName = "argus_evidence_platform",
    [string]$DockerPath = "C:\Program Files\Docker\Docker\resources\bin\docker.exe",
    [string]$ContainerName = "argus-evidence-postgres",
    [string]$AzuriteContainerName = "argus-evidence-azurite",
    [string]$DeviceId,
    [string]$CaseExternalId,
    [string]$BaseUrl = "http://127.0.0.1:5058",
    [int]$ScreenshotDelaySeconds = 12
)

$ErrorActionPreference = "Stop"

function Test-AzuriteExtentIntegrity {
    param(
        [string]$DockerExecutable,
        [string]$AzuriteName,
        [string]$WorkingRoot
    )

    $inspectionRoot = Join-Path $WorkingRoot "azurite-validate"
    if (Test-Path $inspectionRoot) {
        Remove-Item -LiteralPath $inspectionRoot -Recurse -Force
    }

    & $DockerExecutable cp "${AzuriteName}:/data" $inspectionRoot | Out-Null

    $candidatePaths = @(
        (Join-Path $inspectionRoot "data\__azurite_db_blob__.json"),
        (Join-Path $inspectionRoot "__azurite_db_blob__.json")
    )

    $blobDatabasePath = $candidatePaths | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not (Test-Path $blobDatabasePath)) {
        throw "Azurite blob metadata file not found under $inspectionRoot"
    }

    $blobDatabase = Get-Content -Raw $blobDatabasePath | ConvertFrom-Json
    $blobCollection = $blobDatabase.collections | Where-Object { $_.name -eq '$BLOBS_COLLECTION$' } | Select-Object -First 1
    if ($null -eq $blobCollection) {
        throw "Azurite blob collection not found in metadata database."
    }

    $checkedBlobCount = 0L
    foreach ($blob in $blobCollection.data) {
        if ($null -eq $blob.persistency) {
            continue
        }

        $extentCandidates = @(
            (Join-Path $inspectionRoot ("data\__blobstorage__\{0}" -f $blob.persistency.id)),
            (Join-Path $inspectionRoot ("__blobstorage__\{0}" -f $blob.persistency.id))
        )
        $extentPath = $extentCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
        if (-not (Test-Path $extentPath)) {
            throw "Missing extent file for blob $($blob.name) under $inspectionRoot"
        }

        $requiredLength = [int64]$blob.persistency.offset + [int64]$blob.persistency.count
        $actualLength = (Get-Item -LiteralPath $extentPath).Length
        if ($actualLength -lt $requiredLength) {
            throw "Azurite extent truncated for blob $($blob.name). Required at least $requiredLength bytes in $extentPath, found $actualLength."
        }

        $checkedBlobCount++
    }

    return $checkedBlobCount
}

$allowGrowthTables = @(
    "argus.notification_captures",
    "argus.text_capture_batches",
    "argus.evidence_items",
    "argus.evidence_blobs",
    "argus.audit_entries"
)

$manifestPath = Join-Path $SourceDirectory "manifest.json"
if (-not (Test-Path $manifestPath)) {
    throw "Manifest not found: $manifestPath"
}

$manifest = Get-Content -Raw $manifestPath | ConvertFrom-Json

$azuriteDataPath = Join-Path $SourceDirectory "azurite-data"
if (-not (Test-Path $azuriteDataPath)) {
    throw "Azurite data snapshot not found: $azuriteDataPath"
}

$expectedBlobFileCount = [int64]$manifest.blobStorage.fileCount
$actualBlobFileCount = (
    & $DockerPath exec -i $AzuriteContainerName sh -c "find /data -type f | wc -l"
).Trim()

if ([int64]$actualBlobFileCount -lt $expectedBlobFileCount) {
    throw "Azurite file regression. Expected at least $expectedBlobFileCount files, got $actualBlobFileCount."
}

Write-Host "Validated Azurite file count: $actualBlobFileCount (minimum expected: $expectedBlobFileCount)"

$validatedBlobCount = Test-AzuriteExtentIntegrity -DockerExecutable $DockerPath -AzuriteName $AzuriteContainerName -WorkingRoot $SourceDirectory
Write-Host "Validated Azurite extent integrity for $validatedBlobCount blob entries"

foreach ($tableProperty in $manifest.expectedRowCounts.PSObject.Properties) {
    $tableName = $tableProperty.Name
    $expectedCount = [int64]$tableProperty.Value
    $actualCount = (& $DockerPath exec -i $ContainerName psql -U postgres -d $TargetDatabaseName -A -t -c "select count(*) from $tableName;").Trim()

    if ($allowGrowthTables -contains $tableName) {
        if ([int64]$actualCount -lt $expectedCount) {
            throw "Count regression for $tableName. Expected at least $expectedCount, got $actualCount."
        }

        Write-Host "Validated $tableName count: $actualCount (minimum expected: $expectedCount)"
        continue
    }

    if ([int64]$actualCount -ne $expectedCount) {
        throw "Count mismatch for $tableName. Expected $expectedCount, got $actualCount."
    }

    Write-Host "Validated $tableName count: $actualCount"
}

if (-not [string]::IsNullOrWhiteSpace($DeviceId) -and -not [string]::IsNullOrWhiteSpace($CaseExternalId)) {
    & "C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File "C:\Src\Argus.EvidencePlatform\scripts\request-screenshot.ps1" -DeviceId $DeviceId -BaseUrl $BaseUrl
    & "C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File "C:\Src\Argus.EvidencePlatform\scripts\check-latest-screenshots.ps1" -CaseExternalId $CaseExternalId -DelaySeconds $ScreenshotDelaySeconds
}

Write-Host "Cutover validation completed for $TargetDatabaseName"
