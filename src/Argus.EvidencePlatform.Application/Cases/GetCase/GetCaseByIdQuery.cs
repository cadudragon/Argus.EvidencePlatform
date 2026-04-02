using Argus.EvidencePlatform.Contracts.Cases;

namespace Argus.EvidencePlatform.Application.Cases.GetCase;

public sealed record GetCaseByIdQuery(Guid CaseId);
