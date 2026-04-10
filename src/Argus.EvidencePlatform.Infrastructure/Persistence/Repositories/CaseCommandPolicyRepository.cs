using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Domain.Cases;
using Microsoft.EntityFrameworkCore;

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Repositories;

public sealed class CaseCommandPolicyRepository(ArgusDbContext dbContext) : ICaseCommandPolicyRepository
{
    public async Task<CaseCommandPolicy> GetOrCreateDefaultAsync(
        Guid caseId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.CaseCommandPolicies
            .SingleOrDefaultAsync(x => x.CaseId == caseId, cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var created = CaseCommandPolicy.CreateDefault(Guid.NewGuid(), caseId, now);
        await dbContext.CaseCommandPolicies.AddAsync(created, cancellationToken);
        return created;
    }
}
