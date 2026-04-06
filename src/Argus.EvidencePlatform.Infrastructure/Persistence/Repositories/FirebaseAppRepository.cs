using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Domain.Firebase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Repositories;

public sealed class FirebaseAppRepository(
    ArgusDbContext dbContext,
    IOptions<Argus.EvidencePlatform.Infrastructure.Firebase.FirebaseOptions> options) : IFirebaseAppRepository
{
    public async Task<FirebaseAppRegistration?> GetByIdAsync(Guid firebaseAppId, CancellationToken cancellationToken)
    {
        var registration = await dbContext.FirebaseAppRegistrations
            .SingleOrDefaultAsync(x => x.Id == firebaseAppId, cancellationToken);
        return registration ?? GetConfiguredApps().SingleOrDefault(x => x.Id == firebaseAppId);
    }

    public async Task<IReadOnlyList<FirebaseAppRegistration>> ListActiveForNewCasesAsync(CancellationToken cancellationToken)
    {
        var persistedApps = await dbContext.FirebaseAppRegistrations
            .Where(x => x.IsActiveForNewCases)
            .OrderBy(x => x.Key)
            .ToListAsync(cancellationToken);
        return persistedApps.Count > 0
            ? persistedApps
            : GetConfiguredApps().Where(x => x.IsActiveForNewCases).OrderBy(x => x.Key).ToList();
    }

    private IReadOnlyList<FirebaseAppRegistration> GetConfiguredApps()
    {
        return options.Value.Apps
            .GroupBy(app => FirebaseAppRegistration.CreateDeterministicId(app.Key))
            .Select(group =>
            {
                var app = group.Last();
                return FirebaseAppRegistration.Create(
                    app.Key,
                    string.IsNullOrWhiteSpace(app.DisplayName) ? app.Key : app.DisplayName,
                    app.ProjectId,
                    app.ServiceAccountPath,
                    app.IsActiveForNewCases,
                    DateTimeOffset.UnixEpoch);
            })
            .ToList();
    }
}
