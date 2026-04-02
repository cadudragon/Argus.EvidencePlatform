using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Audit;
using Argus.EvidencePlatform.Domain.Audit;
using Microsoft.EntityFrameworkCore;

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Repositories;

public sealed class AuditRepository(ArgusDbContext dbContext) : IAuditRepository
{
    public Task AddAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        return dbContext.AuditEntries.AddAsync(entry, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyList<AuditEntryResponse>> GetByCaseIdAsync(
        Guid caseId,
        CancellationToken cancellationToken)
    {
        return await dbContext.AuditEntries
            .AsNoTracking()
            .Where(x => x.CaseId == caseId)
            .OrderByDescending(x => x.OccurredAt)
            .Select(x => new AuditEntryResponse(
                x.Id,
                x.CaseId,
                x.ActorType.ToString(),
                x.ActorId,
                x.Action,
                x.EntityType,
                x.EntityId,
                x.OccurredAt,
                x.CorrelationId,
                x.PayloadJson))
            .ToListAsync(cancellationToken);
    }
}
