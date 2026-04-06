using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Domain.TextCaptures;

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Repositories;

public sealed class TextCaptureBatchRepository(ArgusDbContext dbContext) : ITextCaptureBatchRepository
{
    public Task AddAsync(TextCaptureBatch entity, CancellationToken cancellationToken)
    {
        return dbContext.TextCaptureBatches.AddAsync(entity, cancellationToken).AsTask();
    }
}
