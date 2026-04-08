param(
    [string]$SourceDirectory = "C:\Src\bb073-cutover\argus_evidence_platform",
    [string]$TargetDatabaseName = "argus_evidence_platform",
    [string]$DockerPath = "C:\Program Files\Docker\Docker\resources\bin\docker.exe",
    [string]$ContainerName = "argus-evidence-postgres",
    [string]$AzuriteContainerName = "argus-evidence-azurite",
    [string]$ComposeFile = "C:\Src\Argus.EvidencePlatform\compose.yaml",
    [string]$DotnetPath = "C:\Program Files\dotnet\dotnet.exe",
    [string]$InfrastructureProject = "C:\Src\Argus.EvidencePlatform\src\Argus.EvidencePlatform.Infrastructure\Argus.EvidencePlatform.Infrastructure.csproj"
)

$ErrorActionPreference = "Stop"

function Get-CopyColumnList {
    param([string[]]$Columns)

    return $Columns -join ", "
}

function Test-ManifestShape {
    param($Manifest)

    if ($null -eq $Manifest.tables -or $null -eq $Manifest.expectedRowCounts -or $null -eq $Manifest.fileSha256) {
        throw "Manifest is missing required relational export sections."
    }
}

function Initialize-AzuriteContainer {
    param(
        [string]$DockerExecutable,
        [string]$ComposeFilePath,
        [string]$AzuriteName
    )

    & $DockerExecutable compose -f $ComposeFilePath up -d azurite | Out-Null
    & $DockerExecutable stop $AzuriteName | Out-Null
}

function Restore-AzuriteWorkspace {
    param(
        [string]$DockerExecutable,
        [string]$AzuriteName,
        [string]$WorkspacePath
    )

    & $DockerExecutable run --rm --volumes-from $AzuriteName mcr.microsoft.com/azure-storage/azurite:3.35.0 sh -c "rm -rf /data/*" | Out-Null
    & $DockerExecutable cp "$WorkspacePath\." "${AzuriteName}:/data" | Out-Null
    & $DockerExecutable start $AzuriteName | Out-Null
}

if (-not (Test-Path $SourceDirectory)) {
    throw "Source directory not found: $SourceDirectory"
}

$manifestPath = Join-Path $SourceDirectory "manifest.json"
if (-not (Test-Path $manifestPath)) {
    throw "Manifest not found: $manifestPath"
}

$manifest = Get-Content -Raw $manifestPath | ConvertFrom-Json
$sourceDatabaseName = [string]$manifest.sourceDatabase
Test-ManifestShape -Manifest $manifest

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

$azuriteDataPath = Join-Path $SourceDirectory "azurite-data"
if (-not (Test-Path $azuriteDataPath)) {
    throw "Azurite data snapshot not found: $azuriteDataPath"
}

foreach ($table in $tables) {
    $tableName = $table.Table
    $safeName = $tableName.Replace(".", "__")
    $csvPath = Join-Path $SourceDirectory "$safeName.csv"
    if (-not (Test-Path $csvPath)) {
        throw "Missing CSV for ${tableName}: $csvPath"
    }

    $expectedSha256 = [string]$manifest.fileSha256.$tableName
    if ([string]::IsNullOrWhiteSpace($expectedSha256)) {
        throw "Missing file hash in manifest for $tableName."
    }

    $actualSha256 = (Get-FileHash -Algorithm SHA256 $csvPath).Hash.ToLowerInvariant()
    if ($actualSha256 -ne $expectedSha256.ToLowerInvariant()) {
        throw "CSV hash mismatch for $tableName. Expected $expectedSha256, got $actualSha256."
    }
}

$dropCreateSql = @"
select pg_terminate_backend(pid)
from pg_stat_activity
where datname = '$TargetDatabaseName'
  and pid <> pg_backend_pid();

drop database if exists "$TargetDatabaseName";
create database "$TargetDatabaseName";
"@

$dropCreateSql | & $DockerPath exec -i $ContainerName psql -U postgres -d postgres | Out-Null

Initialize-AzuriteContainer -DockerExecutable $DockerPath -ComposeFilePath $ComposeFile -AzuriteName $AzuriteContainerName
Restore-AzuriteWorkspace -DockerExecutable $DockerPath -AzuriteName $AzuriteContainerName -WorkspacePath $azuriteDataPath

$env:ARGUS_EVIDENCE_PLATFORM_POSTGRES = "Host=localhost;Port=5432;Database=$TargetDatabaseName;Username=postgres;Password=postgres"
& $DotnetPath tool restore | Out-Null
& $DotnetPath tool run dotnet-ef database update --project $InfrastructureProject --context ArgusDbContext | Out-Null

foreach ($table in $tables) {
    $tableName = $table.Table
    $safeName = $tableName.Replace(".", "__")
    $csvPath = Join-Path $SourceDirectory "$safeName.csv"

    $columnList = Get-CopyColumnList -Columns $table.Columns
    $containerCsvPath = "/tmp/$safeName.csv"
    & $DockerPath cp $csvPath "${ContainerName}:$containerCsvPath" | Out-Null

    $copyCommand = "\copy $tableName ($columnList) from '$containerCsvPath' with (format csv, header true)"
    $copyCommand | & $DockerPath exec -i $ContainerName psql -U postgres -d $TargetDatabaseName | Out-Null
    & $DockerPath exec -i $ContainerName rm -f $containerCsvPath | Out-Null
    Write-Host "Imported $tableName from $csvPath"
}

Write-Host "Import completed into $TargetDatabaseName from export $sourceDatabaseName"
