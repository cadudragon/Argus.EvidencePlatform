using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Cases;
using Argus.EvidencePlatform.Domain.Cases;
using Microsoft.EntityFrameworkCore;

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Repositories;

public sealed class CaseRepository(ArgusDbContext dbContext) : ICaseRepository
{
    public Task AddAsync(Case entity, CancellationToken cancellationToken)
    {
        return dbContext.Cases.AddAsync(entity, cancellationToken).AsTask();
    }

    public Task<bool> ExistsAsync(Guid caseId, CancellationToken cancellationToken)
    {
        return dbContext.Cases.AnyAsync(x => x.Id == caseId, cancellationToken);
    }

    public Task<bool> ExistsByExternalCaseIdAsync(string externalCaseId, CancellationToken cancellationToken)
    {
        return dbContext.Cases.AnyAsync(x => x.ExternalCaseId == externalCaseId, cancellationToken);
    }

    public Task<Guid?> GetIdByExternalCaseIdAsync(string externalCaseId, CancellationToken cancellationToken)
    {
        return dbContext.Cases
            .AsNoTracking()
            .Where(x => x.ExternalCaseId == externalCaseId)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<CaseResponse?> GetByIdAsync(Guid caseId, CancellationToken cancellationToken)
    {
        return dbContext.Cases
            .AsNoTracking()
            .Where(x => x.Id == caseId)
            .Select(x => new CaseResponse(
                x.Id,
                x.ExternalCaseId,
                x.Title,
                x.Description,
                x.Status.ToString(),
                x.CreatedAt,
                x.ClosedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
