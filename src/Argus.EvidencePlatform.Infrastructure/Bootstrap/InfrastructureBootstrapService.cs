using Argus.EvidencePlatform.Infrastructure.Persistence;
using Argus.EvidencePlatform.Infrastructure.Storage;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Argus.EvidencePlatform.Infrastructure.Bootstrap;

public sealed class InfrastructureBootstrapService(
    IServiceProvider serviceProvider,
    BlobServiceClient blobServiceClient,
    RelationalDatabaseMigrator relationalDatabaseMigrator,
    IOptions<BlobStorageOptions> blobOptions,
    IOptions<InfrastructureBootstrapOptions> bootstrapOptions,
    ILogger<InfrastructureBootstrapService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = bootstrapOptions.Value;

        if (!settings.BootstrapOnStartup)
        {
            return;
        }

        var retryCount = Math.Max(settings.RetryCount, 1);
        var retryDelay = TimeSpan.FromSeconds(Math.Max(settings.RetryDelaySeconds, 1));

        for (var attempt = 1; attempt <= retryCount; attempt++)
        {
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ArgusDbContext>();

                await relationalDatabaseMigrator.EnsureMigratedAsync(dbContext, cancellationToken);
                await EnsureBlobContainersAsync(blobOptions.Value, cancellationToken);

                logger.LogInformation(
                    "Infrastructure bootstrap completed on attempt {Attempt}.",
                    attempt);
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (attempt < retryCount)
            {
                logger.LogWarning(
                    ex,
                    "Infrastructure bootstrap attempt {Attempt} failed. Retrying in {DelaySeconds}s.",
                    attempt,
                    retryDelay.TotalSeconds);

                await Task.Delay(retryDelay, cancellationToken);
            }
        }

        await using var finalScope = serviceProvider.CreateAsyncScope();
        var finalDbContext = finalScope.ServiceProvider.GetRequiredService<ArgusDbContext>();
        await relationalDatabaseMigrator.EnsureMigratedAsync(finalDbContext, cancellationToken);
        await EnsureBlobContainersAsync(blobOptions.Value, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task EnsureBlobContainersAsync(
        BlobStorageOptions settings,
        CancellationToken cancellationToken)
    {
        foreach (var containerName in GetContainerNames(settings))
        {
            await blobServiceClient
                .GetBlobContainerClient(containerName)
                .CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        }
    }

    private static IEnumerable<string> GetContainerNames(BlobStorageOptions settings)
    {
        return new HashSet<string>(
        [
            settings.StagingContainerName,
            settings.ExportsContainerName
        ],
        StringComparer.OrdinalIgnoreCase);
    }
}
