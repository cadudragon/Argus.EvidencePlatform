namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public sealed record EvidenceArtifactDescriptor(
    Guid Id,
    Guid CaseId,
    string SourceId,
    string ArtifactType,
    DateTimeOffset CaptureTimestamp,
    DateTimeOffset ReceivedAt,
    string Status,
    string? Classification,
    string ContainerName,
    string BlobName,
    string? BlobVersionId,
    string ContentType,
    long SizeBytes,
    string Sha256);
