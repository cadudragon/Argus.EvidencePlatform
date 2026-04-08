namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public sealed record EvidenceContentStream(
    Stream Content,
    string ContentType,
    long? ContentLength,
    DateTimeOffset? LastModified,
    bool SupportsRangeProcessing,
    string FileName);
