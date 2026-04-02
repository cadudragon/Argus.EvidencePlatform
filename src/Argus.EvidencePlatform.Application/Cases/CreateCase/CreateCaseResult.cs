using Argus.EvidencePlatform.Contracts.Cases;

namespace Argus.EvidencePlatform.Application.Cases.CreateCase;

public sealed record CreateCaseResult(
    CaseResponse? Case,
    bool AlreadyExists)
{
    public static CreateCaseResult Created(CaseResponse response) => new(response, false);

    public static CreateCaseResult Conflict() => new(null, true);
}
