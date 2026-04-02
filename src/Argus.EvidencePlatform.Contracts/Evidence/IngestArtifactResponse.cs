namespace Argus.EvidencePlatform.Contracts.Evidence;

public sealed record IngestArtifactResponse(
    Guid ReceiptId,
    Guid EvidenceId,
    Guid CaseId,
    string SourceId,
    string EvidenceType,
    string BlobName,
    string Sha256,
    long SizeBytes,
    DateTimeOffset ReceivedAt,
    string Status);
