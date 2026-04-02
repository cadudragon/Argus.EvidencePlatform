namespace Argus.EvidencePlatform.Contracts.Cases;

public sealed record CaseResponse(
    Guid Id,
    string ExternalCaseId,
    string Title,
    string? Description,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ClosedAt);
