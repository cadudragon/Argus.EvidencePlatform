using Argus.EvidencePlatform.Application.Common.Abstractions;

namespace Argus.EvidencePlatform.Infrastructure.Persistence;

public sealed class EfUnitOfWork(ArgusDbContext dbContext) : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
