using Argus.EvidencePlatform.Contracts.Audit;
using Argus.EvidencePlatform.Domain.Audit;

namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public interface IAuditRepository
{
    Task AddAsync(AuditEntry entry, CancellationToken cancellationToken);
    Task<IReadOnlyList<AuditEntryResponse>> GetByCaseIdAsync(Guid caseId, CancellationToken cancellationToken);
}
