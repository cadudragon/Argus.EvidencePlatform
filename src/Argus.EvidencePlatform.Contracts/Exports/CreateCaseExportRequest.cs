namespace Argus.EvidencePlatform.Contracts.Exports;

public sealed record CreateCaseExportRequest(
    Guid CaseId,
    string? Format,
    string? Reason);
