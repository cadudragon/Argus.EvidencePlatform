param(
    [Parameter(Mandatory = $true)]
    [string]$CaseExternalId,

    [int]$Limit = 5,

    [int]$DelaySeconds = 0
)

$ErrorActionPreference = "Stop"

if ($DelaySeconds -gt 0) {
    Start-Sleep -Seconds $DelaySeconds
}

$docker = "C:\Program Files\Docker\Docker\resources\bin\docker.exe"
$sql = @"
select
    ei."CaptureTimestamp",
    eb."BlobName",
    eb."SizeBytes",
    eb."Sha256"
from argus.evidence_items ei
join argus.cases c on c."Id" = ei."CaseId"
join argus.evidence_blobs eb on eb."EvidenceItemId" = ei."Id"
where c."ExternalCaseId" = '$CaseExternalId'
  and ei."EvidenceType" = 'Image'
order by ei."CaptureTimestamp" desc
limit $Limit;
"@

$sql | & $docker exec -i argus-evidence-postgres psql -U postgres -d argus_evidence_platform
