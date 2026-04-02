namespace Argus.EvidencePlatform.Contracts.Cases;

public sealed record CreateCaseRequest(
    string ExternalCaseId,
    string Title,
    string? Description);
