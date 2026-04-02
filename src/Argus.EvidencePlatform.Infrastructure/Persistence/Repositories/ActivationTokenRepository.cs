using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Domain.Enrollment;
using Microsoft.EntityFrameworkCore;

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Repositories;

public sealed class ActivationTokenRepository(ArgusDbContext dbContext) : IActivationTokenRepository
{
    public Task AddAsync(ActivationToken entity, CancellationToken cancellationToken)
    {
        return dbContext.ActivationTokens.AddAsync(entity, cancellationToken).AsTask();
    }

    public Task<ActivationToken?> GetByTokenAsync(string token, CancellationToken cancellationToken)
    {
        return dbContext.ActivationTokens
            .SingleOrDefaultAsync(x => x.Token == token, cancellationToken);
    }
}
