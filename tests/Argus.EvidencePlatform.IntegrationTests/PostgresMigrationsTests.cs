using System.Data.Common;
using Argus.EvidencePlatform.Infrastructure.Bootstrap;
using Argus.EvidencePlatform.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Argus.EvidencePlatform.IntegrationTests;

public sealed class PostgresMigrationsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16")
        .WithDatabase("argus_bb073_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task Clean_database_should_apply_all_migrations()
    {
        await using var dbContext = CreateDbContext();
        var migrator = CreateMigrator();

        await migrator.EnsureMigratedAsync(dbContext, CancellationToken.None);

        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();

        var migrations = await QueryStringListAsync(
            connection,
            """select "MigrationId" from public."__EFMigrationsHistory" order by "MigrationId";""");
        migrations.Should().ContainInOrder(
            "20260406212005_InitialBaseline",
            "20260406212021_ReconcileLegacySchema");

        var isFirebaseAppIdRequired = await QueryScalarAsync<string>(
            connection,
            """
            select is_nullable
            from information_schema.columns
            where table_schema = 'argus'
              and table_name = 'cases'
              and column_name = 'FirebaseAppId';
            """);
        isFirebaseAppIdRequired.Should().Be("NO");
    }

    [Fact]
    public async Task Legacy_database_should_be_adopted_reconciled_and_preserve_data()
    {
        var firebaseAppId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var caseId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var evidenceId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var blobId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var exportJobId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var bindingId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        await using (var setupDbContext = CreateDbContext())
        {
            var migrator = CreateMigrator();
            await migrator.EnsureMigratedAsync(setupDbContext, CancellationToken.None);
        }

        await using (var connection = new NpgsqlConnection(_postgres.GetConnectionString()))
        {
            await connection.OpenAsync();
            await ExecuteNonQueryAsync(
                connection,
                """
                drop table if exists public."__EFMigrationsHistory";

                alter table argus.cases
                    alter column "FirebaseAppId" drop not null;

                alter table argus.fcm_token_bindings
                    alter column "FirebaseAppId" drop not null;

                alter table argus.evidence_blobs
                    add column if not exists "ImmutabilityState" character varying(64) not null default 'pending',
                    add column if not exists "LegalHoldState" character varying(64) not null default 'none';

                alter table argus.export_jobs
                    add column if not exists "ManifestBlobName" character varying(1024),
                    add column if not exists "PackageBlobName" character varying(1024);

                delete from argus.evidence_blobs;
                delete from argus.evidence_items;
                delete from argus.export_jobs;
                delete from argus.fcm_token_bindings;
                delete from argus.cases;
                delete from argus.firebase_app_registrations;

                insert into argus.firebase_app_registrations
                    ("Id", "Key", "DisplayName", "ProjectId", "ServiceAccountPath", "IsActiveForNewCases", "CreatedAt", "UpdatedAt")
                values
                    ('11111111-1111-1111-1111-111111111111', 'fb-primary', 'Primary', 'argus-primary', 'C:\secrets\fb.json', true, now(), now());

                insert into argus.cases
                    ("Id", "FirebaseAppId", "ExternalCaseId", "Title", "Description", "Status", "CreatedAt", "ClosedAt")
                values
                    ('22222222-2222-2222-2222-222222222222', null, 'CASE-LEGACY-001', 'Legacy Case', null, 'Active', now(), null);

                insert into argus.fcm_token_bindings
                    ("Id", "FirebaseAppId", "DeviceId", "FcmToken", "BoundAt", "UpdatedAt")
                values
                    ('66666666-6666-6666-6666-666666666666', null, 'android-legacy-01', 'legacy-token', now(), now());

                insert into argus.evidence_items
                    ("Id", "CaseId", "SourceId", "EvidenceType", "CaptureTimestamp", "ReceivedAt", "Status", "Classification")
                values
                    ('33333333-3333-3333-3333-333333333333', '22222222-2222-2222-2222-222222222222', 'android-legacy-01', 'Image', now(), now(), 'Preserved', 'screenshot');

                insert into argus.evidence_blobs
                    ("Id", "EvidenceItemId", "ContainerName", "BlobName", "BlobVersionId", "ContentType", "SizeBytes", "Sha256", "ImmutabilityState", "LegalHoldState", "StoredAt")
                values
                    ('44444444-4444-4444-4444-444444444444', '33333333-3333-3333-3333-333333333333', 'staging', 'legacy/capture.jpg', null, 'image/jpeg', 123, repeat('a', 64), 'pending', 'none', now());

                insert into argus.export_jobs
                    ("Id", "CaseId", "Status", "RequestedBy", "RequestedAt", "CompletedAt", "ManifestBlobName", "PackageBlobName")
                values
                    ('55555555-5555-5555-5555-555555555555', '22222222-2222-2222-2222-222222222222', 'Queued', 'Local Operator', now(), null, 'manifest.json', 'package.zip');
                """);
        }

        await using (var legacyDbContext = CreateDbContext())
        {
            var migrator = CreateMigrator();
            await migrator.EnsureMigratedAsync(legacyDbContext, CancellationToken.None);
        }

        await using var assertConnection = new NpgsqlConnection(_postgres.GetConnectionString());
        await assertConnection.OpenAsync();

        var migrations = await QueryStringListAsync(
            assertConnection,
            """select "MigrationId" from public."__EFMigrationsHistory" order by "MigrationId";""");
        migrations.Should().ContainInOrder(
            "20260406212005_InitialBaseline",
            "20260406212021_ReconcileLegacySchema");

        var caseFirebaseAppId = await QueryScalarAsync<Guid>(
            assertConnection,
            """select "FirebaseAppId" from argus.cases where "Id" = @id;""",
            new NpgsqlParameter("@id", caseId));
        caseFirebaseAppId.Should().Be(firebaseAppId);

        var bindingFirebaseAppId = await QueryScalarAsync<Guid>(
            assertConnection,
            """select "FirebaseAppId" from argus.fcm_token_bindings where "Id" = @id;""",
            new NpgsqlParameter("@id", bindingId));
        bindingFirebaseAppId.Should().Be(firebaseAppId);

        var blobCount = await QueryScalarAsync<long>(
            assertConnection,
            """select count(*) from argus.evidence_blobs where "Id" = @id;""",
            new NpgsqlParameter("@id", blobId));
        blobCount.Should().Be(1);

        var exportJobCount = await QueryScalarAsync<long>(
            assertConnection,
            """select count(*) from argus.export_jobs where "Id" = @id;""",
            new NpgsqlParameter("@id", exportJobId));
        exportJobCount.Should().Be(1);

        var deadColumns = await QueryStringListAsync(
            assertConnection,
            """
            select column_name
            from information_schema.columns
            where table_schema = 'argus'
              and (
                (table_name = 'evidence_blobs' and column_name in ('ImmutabilityState', 'LegalHoldState'))
                or
                (table_name = 'export_jobs' and column_name in ('ManifestBlobName', 'PackageBlobName'))
              )
            order by table_name, column_name;
            """);
        deadColumns.Should().BeEmpty();
    }

    private ArgusDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ArgusDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        return new ArgusDbContext(options);
    }

    private static RelationalDatabaseMigrator CreateMigrator()
    {
        return new RelationalDatabaseMigrator(NullLogger<RelationalDatabaseMigrator>.Instance);
    }

    private static async Task ExecuteNonQueryAsync(DbConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<List<string>> QueryStringListAsync(DbConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string>();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }

    private static async Task<T> QueryScalarAsync<T>(DbConnection connection, string sql, params NpgsqlParameter[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        var value = await command.ExecuteScalarAsync();
        return (T)value!;
    }
}
