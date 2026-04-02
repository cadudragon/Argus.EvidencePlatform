namespace Argus.EvidencePlatform.Contracts.Enrollment;

public sealed record ActivationSuccessResponse(
    string CaseId,
    long ValidUntil,
    IReadOnlyList<string> Scope);
