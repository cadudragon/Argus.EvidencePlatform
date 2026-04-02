using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Audit;

namespace Argus.EvidencePlatform.Application.Audit.GetCaseAuditTrail;

public sealed class GetCaseAuditTrailHandler(IAuditRepository auditRepository)
{
    public Task<IReadOnlyList<AuditEntryResponse>> Handle(
        GetCaseAuditTrailQuery query,
        CancellationToken cancellationToken)
    {
        return auditRepository.GetByCaseIdAsync(query.CaseId, cancellationToken);
    }
}
