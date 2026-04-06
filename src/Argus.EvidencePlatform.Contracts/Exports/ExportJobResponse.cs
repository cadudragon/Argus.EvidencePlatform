namespace Argus.EvidencePlatform.Contracts.Exports;

public sealed record ExportJobResponse(
    Guid Id,
    Guid CaseId,
    string Status,
    string RequestedBy,
    DateTimeOffset RequestedAt,
    DateTimeOffset? CompletedAt);
