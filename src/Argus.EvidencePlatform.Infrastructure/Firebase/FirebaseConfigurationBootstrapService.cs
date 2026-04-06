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
            var migratedCases = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                  update argus.cases
                  set "FirebaseAppId" = {firebaseAppId}
                  where "FirebaseAppId" is null
                     or "FirebaseAppId" = {Guid.Empty};
                  """,
                cancellationToken);

            var migratedBindings = await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"""
                  update argus.fcm_token_bindings
                  set "FirebaseAppId" = {firebaseAppId}
                  where "FirebaseAppId" is null
                     or "FirebaseAppId" = {Guid.Empty};
                  """,
                cancellationToken);

            if (migratedCases > 0 || migratedBindings > 0)
            {
                logger.LogInformation(
                    "Migrated {CaseCount} legacy cases and {BindingCount} FCM bindings to Firebase app {FirebaseAppId}.",
                    migratedCases,
                    migratedBindings,
                    firebaseAppId);
            }

            return;
        }

        var hasLegacyCases = await dbContext.Cases
            .AsNoTracking()
            .Where(x => x.Id != Guid.Empty)
            .Select(x => EF.Property<Guid?>(x, nameof(Domain.Cases.Case.FirebaseAppId)))
            .AnyAsync(x => x == null || x == Guid.Empty, cancellationToken);
        var hasLegacyBindings = await dbContext.FcmTokenBindings
            .AsNoTracking()
            .Where(x => x.Id != Guid.Empty)
            .Select(x => EF.Property<Guid?>(x, nameof(Domain.Devices.FcmTokenBinding.FirebaseAppId)))
            .AnyAsync(x => x == null || x == Guid.Empty, cancellationToken);
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
