namespace Argus.EvidencePlatform.Application.Evidence.ListArtifacts;

public sealed record ListCaseArtifactsQuery(
    Guid CaseId,
    string? Cursor,
    int? PageSize);
