using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Domain.Firebase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Repositories;

public sealed class FirebaseAppRoutingResolver(
    ArgusDbContext dbContext,
    IOptions<Argus.EvidencePlatform.Infrastructure.Firebase.FirebaseOptions> options) : IFirebaseAppRoutingResolver
{
    public async Task<FirebaseAppRoutingContext?> ResolveForCaseAsync(Guid caseId, CancellationToken cancellationToken)
    {
        var persisted = await dbContext.Cases
            .AsNoTracking()
            .Where(x => x.Id == caseId && x.FirebaseAppId != Guid.Empty)
            .Join(
                dbContext.FirebaseAppRegistrations.AsNoTracking(),
                c => c.FirebaseAppId,
                f => f.Id,
                (c, f) => new FirebaseAppRoutingContext(f.Id, f.Key, f.ProjectId))
            .SingleOrDefaultAsync(cancellationToken);
        if (persisted is not null)
        {
            return persisted;
        }

        var firebaseAppId = await dbContext.Cases
            .AsNoTracking()
            .Where(x => x.Id == caseId)
            .Select(x => x.FirebaseAppId)
            .SingleOrDefaultAsync(cancellationToken);
        if (firebaseAppId == Guid.Empty)
        {
            return null;
        }

        var configured = options.Value.Apps
            .GroupBy(app => FirebaseAppRegistration.CreateDeterministicId(app.Key))
            .Select(group => group.Last())
            .Select(app => new FirebaseAppRoutingContext(
                FirebaseAppRegistration.CreateDeterministicId(app.Key),
                app.Key.Trim(),
                app.ProjectId.Trim()))
            .SingleOrDefault(app => app.FirebaseAppId == firebaseAppId);

        return configured;
    }
}
