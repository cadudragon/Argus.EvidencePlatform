namespace Argus.EvidencePlatform.Contracts.Evidence;

public sealed record EvidenceTimelineItemResponse(
    Guid Id,
    Guid CaseId,
    string SourceId,
    string EvidenceType,
    DateTimeOffset CaptureTimestamp,
    DateTimeOffset ReceivedAt,
    string Status,
    string? Classification,
    string BlobName,
    string Sha256,
    long SizeBytes,
    string ContentType);
