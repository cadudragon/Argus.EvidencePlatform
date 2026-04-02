using Argus.EvidencePlatform.Contracts.Cases;

namespace Argus.EvidencePlatform.Application.Cases.CreateCase;

public sealed record CreateCaseCommand(
    string ExternalCaseId,
    string Title,
    string? Description);
