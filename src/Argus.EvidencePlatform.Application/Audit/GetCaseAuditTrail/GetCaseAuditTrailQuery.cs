using Argus.EvidencePlatform.Contracts.Audit;

namespace Argus.EvidencePlatform.Application.Audit.GetCaseAuditTrail;

public sealed record GetCaseAuditTrailQuery(Guid CaseId);
