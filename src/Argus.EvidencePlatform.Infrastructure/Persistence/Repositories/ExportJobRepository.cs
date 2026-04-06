using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Exports;
using Argus.EvidencePlatform.Domain.Exports;
using Microsoft.EntityFrameworkCore;

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Repositories;

public sealed class ExportJobRepository(ArgusDbContext dbContext) : IExportJobRepository
{
    public Task AddAsync(ExportJob entity, CancellationToken cancellationToken)
    {
        return dbContext.ExportJobs.AddAsync(entity, cancellationToken).AsTask();
    }

    public Task<ExportJobResponse?> GetByIdAsync(Guid exportJobId, CancellationToken cancellationToken)
    {
        return dbContext.ExportJobs
            .AsNoTracking()
            .Where(x => x.Id == exportJobId)
            .Select(x => new ExportJobResponse(
                x.Id,
                x.CaseId,
                x.Status.ToString(),
                x.RequestedBy,
                x.RequestedAt,
                x.CompletedAt))
            .SingleOrDefaultAsync(cancellationToken);
    }
}
