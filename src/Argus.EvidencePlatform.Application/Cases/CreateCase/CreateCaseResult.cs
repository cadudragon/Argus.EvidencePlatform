using Argus.EvidencePlatform.Contracts.Cases;

namespace Argus.EvidencePlatform.Application.Cases.CreateCase;

public sealed record CreateCaseResult(
    CreateCaseOutcome Outcome,
    CaseResponse? Case)
{
    public bool AlreadyExists => Outcome == CreateCaseOutcome.Conflict;

    public static CreateCaseResult Created(CaseResponse response) => new(CreateCaseOutcome.Created, response);

    public static CreateCaseResult Conflict() => new(CreateCaseOutcome.Conflict, null);

    public static CreateCaseResult FirebaseUnavailable() => new(CreateCaseOutcome.FirebaseUnavailable, null);
}
