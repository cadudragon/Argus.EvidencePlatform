param(
    [Parameter(Mandatory = $true)]
    [string]$CaseExternalId,

    [string]$OutputDirectory = "C:\Src\exported-screenshots"
)

$ErrorActionPreference = "Stop"

$dotnet = "C:\Program Files\dotnet\dotnet.exe"
$docker = "C:\Program Files\Docker\Docker\resources\bin\docker.exe"

if (-not (Test-Path $OutputDirectory)) {
    New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
}

$sql = @"
select
    ei."CaptureTimestamp",
    eb."ContainerName",
    eb."BlobName",
    eb."ContentType",
    eb."Sha256"
from argus.evidence_items ei
join argus.cases c on c."Id" = ei."CaseId"
join argus.evidence_blobs eb on eb."EvidenceItemId" = ei."Id"
where c."ExternalCaseId" = '$CaseExternalId'
  and ei."EvidenceType" = 'Image'
order by ei."CaptureTimestamp" asc;
"@

$rows = $sql | & $docker exec -i argus-evidence-postgres psql -U postgres -d argus_evidence_platform -A -F "|" -t

if (-not $rows) {
    Write-Host "No screenshots found for case $CaseExternalId"
    exit 0
}

$downloads = @()

foreach ($row in $rows) {
    if ([string]::IsNullOrWhiteSpace($row)) {
        continue
    }

    $parts = $row -split "\|", 5
    $downloads += [pscustomobject]@{
        CaptureTimestamp = $parts[0].Trim()
        ContainerName = $parts[1].Trim()
        BlobName = $parts[2].Trim()
        ContentType = $parts[3].Trim()
        Sha256 = $parts[4].Trim()
    }
}

if ($downloads.Count -eq 0) {
    Write-Host "No screenshots found for case $CaseExternalId"
    exit 0
}

$tempRoot = Join-Path $env:TEMP "argus-export-screenshots"
$projectDir = Join-Path $tempRoot "downloader"
$projectPath = Join-Path $projectDir "Downloader.csproj"
$programPath = Join-Path $projectDir "Program.cs"
$inputPath = Join-Path $tempRoot "downloads.json"

New-Item -ItemType Directory -Path $projectDir -Force | Out-Null
@($downloads) | ConvertTo-Json -Depth 4 | Set-Content -Path $inputPath

$projectXml = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Azure.Storage.Blobs" Version="12.25.1" />
  </ItemGroup>
</Project>
"@

$programCs = @"
using System.Text.Json;
using Azure.Storage.Blobs;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: Downloader <downloads.json> <output-directory>");
    return 1;
}

var jsonPath = args[0];
var outputDirectory = args[1];
var json = await File.ReadAllTextAsync(jsonPath);
var downloads = JsonSerializer.Deserialize<List<DownloadRow>>(json, new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true
}) ?? new List<DownloadRow>();

Directory.CreateDirectory(outputDirectory);

var blobServiceClient = new BlobServiceClient("UseDevelopmentStorage=true");

for (var index = 0; index < downloads.Count; index++)
{
    var row = downloads[index];
    var extension = Path.GetExtension(row.BlobName);

    if (string.IsNullOrWhiteSpace(extension))
    {
        extension = row.ContentType switch
        {
            var contentType when contentType.StartsWith("image/jpeg", StringComparison.OrdinalIgnoreCase) => ".jpg",
            var contentType when contentType.StartsWith("image/png", StringComparison.OrdinalIgnoreCase) => ".png",
            var contentType when contentType.StartsWith("image/webp", StringComparison.OrdinalIgnoreCase) => ".webp",
            _ => ".bin"
        };
    }

    var safeTimestamp = row.CaptureTimestamp
        .Replace(":", "-")
        .Replace(" ", "-")
        .Replace("+00", "Z", StringComparison.Ordinal);

    var fileName = $"{index:D2}_{safeTimestamp}_{row.Sha256[..Math.Min(12, row.Sha256.Length)]}{extension}";
    var targetPath = Path.Combine(outputDirectory, fileName);

    var blobClient = blobServiceClient
        .GetBlobContainerClient(row.ContainerName)
        .GetBlobClient(row.BlobName);

    await blobClient.DownloadToAsync(targetPath);
    Console.WriteLine(targetPath);
}

return 0;

internal sealed record DownloadRow(
    string CaptureTimestamp,
    string ContainerName,
    string BlobName,
    string ContentType,
    string Sha256);
"@

Set-Content -Path $projectPath -Value $projectXml
Set-Content -Path $programPath -Value $programCs

& $dotnet run --project $projectPath -- $inputPath $OutputDirectory
