namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public sealed record StagedBlobDescriptor(
    string ContainerName,
    string BlobName,
    string ContentType,
    long SizeBytes,
    string Sha256,
    string? BlobVersionId);
