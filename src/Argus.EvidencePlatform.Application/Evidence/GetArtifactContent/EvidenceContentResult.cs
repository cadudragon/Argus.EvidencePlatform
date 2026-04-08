using Argus.EvidencePlatform.Application.Common.Abstractions;

namespace Argus.EvidencePlatform.Application.Evidence.GetArtifactContent;

public sealed record EvidenceContentResult(
    EvidenceContentOutcome Outcome,
    EvidenceContentStream? Content)
{
    public static EvidenceContentResult Success(EvidenceContentStream content) => new(EvidenceContentOutcome.Success, content);

    public static EvidenceContentResult NotFound() => new(EvidenceContentOutcome.NotFound, null);

    public static EvidenceContentResult Conflict() => new(EvidenceContentOutcome.Conflict, null);
}
