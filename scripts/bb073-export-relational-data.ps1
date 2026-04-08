param(
    [string]$DatabaseName = "argus_evidence_platform",
    [string]$OutputDirectory = "C:\Src\bb073-cutover",
    [string]$DockerPath = "C:\Program Files\Docker\Docker\resources\bin\docker.exe",
    [string]$ContainerName = "argus-evidence-postgres",
    [string]$AzuriteContainerName = "argus-evidence-azurite"
)

$ErrorActionPreference = "Stop"

function Get-OrderedSelectSql {
    param(
        [string]$TableName,
        [string[]]$Columns
    )

    return "select {0} from {1} order by 1" -f ($Columns -join ", "), $TableName
}

$tables = @(
    @{ Table = "argus.firebase_app_registrations"; Columns = @('"Id"','"Key"','"DisplayName"','"ProjectId"','"ServiceAccountPath"','"IsActiveForNewCases"','"CreatedAt"','"UpdatedAt"') },
    @{ Table = "argus.cases"; Columns = @('"Id"','"FirebaseAppId"','"ExternalCaseId"','"Title"','"Description"','"Status"','"CreatedAt"','"ClosedAt"') },
    @{ Table = "argus.activation_tokens"; Columns = @('"Id"','"Token"','"CaseId"','"CaseExternalId"','"IssuedAt"','"ValidUntil"','"ConsumedAt"','"ConsumedByDeviceId"') },
    @{ Table = "argus.device_sources"; Columns = @('"Id"','"DeviceId"','"CaseId"','"CaseExternalId"','"EnrolledAt"','"ValidUntil"','"LastSeenAt"') },
    @{ Table = "argus.fcm_token_bindings"; Columns = @('"Id"','"FirebaseAppId"','"DeviceId"','"FcmToken"','"BoundAt"','"UpdatedAt"') },
    @{ Table = "argus.notification_captures"; Columns = @('"Id"','"CaseId"','"CaseExternalId"','"DeviceId"','"Sha256"','"CaptureTimestamp"','"PackageName"','"Title"','"Text"','"BigText"','"NotificationTimestamp"','"Category"','"ReceivedAt"') },
    @{ Table = "argus.text_capture_batches"; Columns = @('"Id"','"CaseId"','"CaseExternalId"','"DeviceId"','"Sha256"','"CaptureTimestamp"','"CaptureCount"','"PayloadJson"','"PackageNamesJson"','"ReceivedAt"') },
    @{ Table = "argus.evidence_items"; Columns = @('"Id"','"CaseId"','"SourceId"','"EvidenceType"','"CaptureTimestamp"','"ReceivedAt"','"Status"','"Classification"') },
    @{ Table = "argus.evidence_blobs"; Columns = @('"Id"','"EvidenceItemId"','"ContainerName"','"BlobName"','"BlobVersionId"','"ContentType"','"SizeBytes"','"Sha256"','"StoredAt"') },
    @{ Table = "argus.export_jobs"; Columns = @('"Id"','"CaseId"','"Status"','"RequestedBy"','"RequestedAt"','"CompletedAt"') },
    @{ Table = "argus.audit_entries"; Columns = @('"Id"','"CaseId"','"ActorType"','"ActorId"','"Action"','"EntityType"','"EntityId"','"OccurredAt"','"CorrelationId"','"PayloadJson"') }
)

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$exportRoot = Join-Path $OutputDirectory $DatabaseName
New-Item -ItemType Directory -Path $exportRoot -Force | Out-Null

$manifest = [ordered]@{
    sourceDatabase = $DatabaseName
    exportedAtUtc = (Get-Date).ToUniversalTime().ToString("o")
    tables = @()
    expectedRowCounts = [ordered]@{}
    fileSha256 = [ordered]@{}
    blobStorage = [ordered]@{
        mode = "azurite-data-directory"
        relativePath = "azurite-data"
    }
}

foreach ($table in $tables) {
    $tableName = $table.Table
    $safeName = $tableName.Replace(".", "__")
    $csvPath = Join-Path $exportRoot "$safeName.csv"
    $selectSql = Get-OrderedSelectSql -TableName $tableName -Columns $table.Columns

    $copyCommand = "\copy ({0}) to stdout with (format csv, header true)" -f $selectSql
    $copyCommand | & $DockerPath exec -i $ContainerName psql -U postgres -d $DatabaseName | Set-Content -Encoding UTF8 $csvPath

    $countSql = "select count(*) from $tableName;"
    $rowCount = (& $DockerPath exec -i $ContainerName psql -U postgres -d $DatabaseName -A -t -c $countSql).Trim()
    $sha256 = (Get-FileHash -Algorithm SHA256 $csvPath).Hash.ToLowerInvariant()

    $manifest.tables += [ordered]@{
        tableName = $tableName
        exportColumns = $table.Columns
    }
    $manifest.expectedRowCounts[$tableName] = [int64]$rowCount
    $manifest.fileSha256[$tableName] = $sha256

    Write-Host "Exported $tableName ($rowCount rows) -> $csvPath"
}

$azuriteDataPath = Join-Path $exportRoot "azurite-data"
if (Test-Path $azuriteDataPath) {
    Remove-Item -LiteralPath $azuriteDataPath -Recurse -Force
}

& $DockerPath cp "${AzuriteContainerName}:/data" $azuriteDataPath | Out-Null

$blobFileCount = (Get-ChildItem -LiteralPath $azuriteDataPath -Recurse -File | Measure-Object).Count
$manifest.blobStorage["fileCount"] = [int64]$blobFileCount

$manifestPath = Join-Path $exportRoot "manifest.json"
$manifest | ConvertTo-Json -Depth 6 | Set-Content -Encoding UTF8 $manifestPath
Write-Host "Manifest written to $manifestPath"
Write-Host "Exported Azurite data ($blobFileCount files) -> $azuriteDataPath"
