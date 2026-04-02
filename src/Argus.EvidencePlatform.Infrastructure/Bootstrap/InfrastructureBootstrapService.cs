using System.Data;
using Argus.EvidencePlatform.Infrastructure.Persistence;
using Argus.EvidencePlatform.Infrastructure.Storage;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Argus.EvidencePlatform.Infrastructure.Bootstrap;

public sealed class InfrastructureBootstrapService(
    IServiceProvider serviceProvider,
    BlobServiceClient blobServiceClient,
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

                await EnsureDatabaseAsync(dbContext, cancellationToken);
                await EnsureDeviceEnrollmentTablesAsync(dbContext, cancellationToken);
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
        await EnsureDatabaseAsync(finalDbContext, cancellationToken);
        await EnsureDeviceEnrollmentTablesAsync(finalDbContext, cancellationToken);
        await EnsureBlobContainersAsync(blobOptions.Value, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task EnsureDatabaseAsync(ArgusDbContext dbContext, CancellationToken cancellationToken)
    {
        if (dbContext.Database.IsInMemory())
        {
            return;
        }

        var migrationsAssembly = dbContext.Database.GetService<IMigrationsAssembly>();
        if (migrationsAssembly.Migrations.Count > 0)
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
            return;
        }

        if (!await HasApplicationTablesAsync(dbContext, cancellationToken))
        {
            var databaseCreator = dbContext.Database.GetService<IRelationalDatabaseCreator>();

            if (!await databaseCreator.ExistsAsync(cancellationToken))
            {
                await databaseCreator.CreateAsync(cancellationToken);
            }

            await databaseCreator.CreateTablesAsync(cancellationToken);
        }
    }

    private async Task<bool> HasApplicationTablesAsync(ArgusDbContext dbContext, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                select 1
                from information_schema.tables
                where table_schema = 'argus'
                  and table_name = 'cases'
                """;

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result is not null;
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task EnsureDeviceEnrollmentTablesAsync(
        ArgusDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (dbContext.Database.IsInMemory())
        {
            return;
        }

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            create schema if not exists argus;

            create table if not exists argus.activation_tokens
            (
                "Id" uuid primary key,
                "Token" character varying(9) not null,
                "CaseId" uuid not null,
                "CaseExternalId" character varying(128) not null,
                "IssuedAt" timestamp with time zone not null,
                "ValidUntil" timestamp with time zone not null,
                "ConsumedAt" timestamp with time zone null,
                "ConsumedByDeviceId" character varying(128) null
            );

            create unique index if not exists "IX_activation_tokens_Token"
                on argus.activation_tokens ("Token");

            create table if not exists argus.device_sources
            (
                "Id" uuid primary key,
                "DeviceId" character varying(128) not null,
                "CaseId" uuid not null,
                "CaseExternalId" character varying(128) not null,
                "EnrolledAt" timestamp with time zone not null,
                "ValidUntil" timestamp with time zone not null,
                "LastSeenAt" timestamp with time zone null
            );

            create unique index if not exists "IX_device_sources_DeviceId"
                on argus.device_sources ("DeviceId");

            create table if not exists argus.fcm_token_bindings
            (
                "Id" uuid primary key,
                "DeviceId" character varying(128) not null,
                "FcmToken" character varying(4096) not null,
                "BoundAt" timestamp with time zone not null,
                "UpdatedAt" timestamp with time zone not null
            );

            create unique index if not exists "IX_fcm_token_bindings_DeviceId"
                on argus.fcm_token_bindings ("DeviceId");

            create table if not exists argus.notification_captures
            (
                "Id" uuid primary key,
                "CaseId" uuid not null,
                "CaseExternalId" character varying(128) not null,
                "DeviceId" character varying(128) not null,
                "Sha256" character varying(128) not null,
                "CaptureTimestamp" timestamp with time zone not null,
                "PackageName" character varying(256) not null,
                "Title" character varying(512) null,
                "Text" character varying(4096) null,
                "BigText" character varying(16384) null,
                "NotificationTimestamp" timestamp with time zone not null,
                "Category" character varying(128) null,
                "ReceivedAt" timestamp with time zone not null
            );

            create index if not exists "IX_notification_captures_CaseId_CaptureTimestamp"
                on argus.notification_captures ("CaseId", "CaptureTimestamp");
            """,
            cancellationToken);
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
            settings.EvidenceContainerName,
            settings.ExportsContainerName
        ],
        StringComparer.OrdinalIgnoreCase);
    }
}
