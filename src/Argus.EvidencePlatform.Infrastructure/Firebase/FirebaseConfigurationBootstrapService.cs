using Argus.EvidencePlatform.Domain.Firebase;
using Argus.EvidencePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Argus.EvidencePlatform.Infrastructure.Firebase;

public sealed class FirebaseConfigurationBootstrapService(
    IServiceProvider serviceProvider,
    IOptions<FirebaseOptions> options,
    ILogger<FirebaseConfigurationBootstrapService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var configuredApps = options.Value.Apps
            .GroupBy(app => FirebaseAppRegistration.CreateDeterministicId(app.Key))
            .Select(group => group.Last())
            .ToList();
        if (configuredApps.Count == 0)
        {
            logger.LogInformation("No Firebase apps are configured for persistence bootstrap.");
            return;
        }

        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ArgusDbContext>();
        var now = DateTimeOffset.UtcNow;
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        var existingRegistrations = await dbContext.FirebaseAppRegistrations
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        foreach (var app in configuredApps)
        {
            var deterministicId = FirebaseAppRegistration.CreateDeterministicId(app.Key);

            if (!existingRegistrations.TryGetValue(deterministicId, out var existing))
            {
                existing = FirebaseAppRegistration.Create(
                    app.Key,
                    string.IsNullOrWhiteSpace(app.DisplayName) ? app.Key : app.DisplayName,
                    app.ProjectId,
                    app.ServiceAccountPath,
                    app.IsActiveForNewCases,
                    now);
                await dbContext.FirebaseAppRegistrations.AddAsync(existing, cancellationToken);
                existingRegistrations[deterministicId] = existing;
            }
            else
            {
                existing.UpdateConfiguration(
                    string.IsNullOrWhiteSpace(app.DisplayName) ? app.Key : app.DisplayName,
                    app.ProjectId,
                    app.ServiceAccountPath,
                    app.IsActiveForNewCases,
                    now);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (configuredApps.Count == 1)
        {
            var firebaseAppId = FirebaseAppRegistration.CreateDeterministicId(configuredApps[0].Key);
            var legacyCases = await dbContext.Cases
                .Where(x => x.FirebaseAppId == Guid.Empty)
                .ToListAsync(cancellationToken);
            foreach (var legacyCase in legacyCases)
            {
                dbContext.Entry(legacyCase).Property(nameof(legacyCase.FirebaseAppId)).CurrentValue = firebaseAppId;
            }

            var legacyBindings = await dbContext.FcmTokenBindings
                .Where(x => x.FirebaseAppId == Guid.Empty)
                .ToListAsync(cancellationToken);
            foreach (var legacyBinding in legacyBindings)
            {
                dbContext.Entry(legacyBinding).Property(nameof(legacyBinding.FirebaseAppId)).CurrentValue = firebaseAppId;
            }

            if (legacyCases.Count > 0 || legacyBindings.Count > 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        var hasLegacyCases = await dbContext.Cases.AnyAsync(x => x.FirebaseAppId == Guid.Empty, cancellationToken);
        var hasLegacyBindings = await dbContext.FcmTokenBindings.AnyAsync(x => x.FirebaseAppId == Guid.Empty, cancellationToken);
        if (hasLegacyCases || hasLegacyBindings)
        {
            throw new InvalidOperationException(
                "Legacy cases or FCM bindings without FirebaseAppId cannot be migrated automatically when multiple Firebase apps are configured.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
