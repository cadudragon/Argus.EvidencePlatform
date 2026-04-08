namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public sealed record EvidenceArtifactListItem(
    Guid Id,
    Guid CaseId,
    string SourceId,
    string ArtifactType,
    DateTimeOffset CaptureTimestamp,
    DateTimeOffset ReceivedAt,
    string Status,
    string? Classification,
    string ContentType,
    long SizeBytes,
    string Sha256,
    bool HasBinary);
