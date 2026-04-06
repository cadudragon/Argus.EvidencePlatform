using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Domain.Cases;
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
        var firebaseAppId = await dbContext.Cases
            .AsNoTracking()
            .Where(x => x.Id == caseId)
            .Select(x => EF.Property<Guid?>(x, nameof(Case.FirebaseAppId)))
            .SingleOrDefaultAsync(cancellationToken);
        if (firebaseAppId is null || firebaseAppId == Guid.Empty)
        {
            return null;
        }

        var persisted = await dbContext.FirebaseAppRegistrations
            .AsNoTracking()
            .Where(x => x.Id == firebaseAppId.Value)
            .Select(x => new FirebaseAppRoutingContext(x.Id, x.Key, x.ProjectId))
            .SingleOrDefaultAsync(cancellationToken);
        if (persisted is not null)
        {
            return persisted;
        }

        var configured = options.Value.Apps
            .GroupBy(app => FirebaseAppRegistration.CreateDeterministicId(app.Key))
            .Select(group => group.Last())
            .Select(app => new FirebaseAppRoutingContext(
                FirebaseAppRegistration.CreateDeterministicId(app.Key),
                app.Key.Trim(),
                app.ProjectId.Trim()))
            .SingleOrDefault(app => app.FirebaseAppId == firebaseAppId.Value);

        return configured;
    }
}
