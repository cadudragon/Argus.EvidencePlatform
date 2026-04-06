using System.Data;
using Argus.EvidencePlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;

namespace Argus.EvidencePlatform.Infrastructure.Bootstrap;

public sealed class RelationalDatabaseMigrator(ILogger<RelationalDatabaseMigrator> logger)
{
    private static readonly string[] RequiredLegacyTables =
    [
        "activation_tokens",
        "audit_entries",
        "cases",
        "device_sources",
        "evidence_blobs",
        "evidence_items",
        "export_jobs",
        "fcm_token_bindings",
        "firebase_app_registrations",
        "notification_captures",
        "text_capture_batches"
    ];

    public async Task EnsureMigratedAsync(ArgusDbContext dbContext, CancellationToken cancellationToken)
    {
        if (dbContext.Database.IsInMemory())
        {
            return;
        }

        await AdoptExistingDatabaseAsync(dbContext, cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    private async Task AdoptExistingDatabaseAsync(ArgusDbContext dbContext, CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            if (await HasMigrationHistoryTableAsync(connection, cancellationToken))
            {
                return;
            }

            var existingTables = await GetExistingArgusTablesAsync(connection, cancellationToken);
            if (existingTables.Count == 0)
            {
                return;
            }

            var missingTables = RequiredLegacyTables
                .Where(tableName => !existingTables.Contains(tableName))
                .ToArray();
            if (missingTables.Length > 0)
            {
                throw new InvalidOperationException(
                    $"Existing argus schema is missing required tables for baseline adoption: {string.Join(", ", missingTables)}.");
            }

            var migrationsAssembly = dbContext.Database.GetService<IMigrationsAssembly>();
            var baselineMigrationId = migrationsAssembly.Migrations.Keys.OrderBy(id => id, StringComparer.Ordinal).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(baselineMigrationId))
            {
                throw new InvalidOperationException("No EF Core migrations were found for baseline adoption.");
            }

            await EnsureMigrationHistoryTableAsync(connection, cancellationToken);
            await InsertBaselineHistoryRowAsync(connection, baselineMigrationId, cancellationToken);

            logger.LogInformation(
                "Adopted existing argus schema into EF Core migration history with baseline {BaselineMigrationId}.",
                baselineMigrationId);
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<bool> HasMigrationHistoryTableAsync(
        IDbConnection connection,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            select 1
            from information_schema.tables
            where table_schema = 'public'
              and table_name = '__EFMigrationsHistory'
            """;

        var result = await ExecuteScalarAsync(command, cancellationToken);
        return result is not null;
    }

    private static async Task<HashSet<string>> GetExistingArgusTablesAsync(
        IDbConnection connection,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            select table_name
            from information_schema.tables
            where table_schema = 'argus'
            """;

        await using var reader = await ExecuteReaderAsync(command, cancellationToken);
        var tables = new HashSet<string>(StringComparer.Ordinal);
        while (await reader.ReadAsync(cancellationToken))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private static async Task EnsureMigrationHistoryTableAsync(
        IDbConnection connection,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            create table if not exists public."__EFMigrationsHistory"
            (
                "MigrationId" character varying(150) not null,
                "ProductVersion" character varying(32) not null,
                constraint "PK___EFMigrationsHistory" primary key ("MigrationId")
            );
            """;

        await ExecuteNonQueryAsync(command, cancellationToken);
    }

    private static async Task InsertBaselineHistoryRowAsync(
        IDbConnection connection,
        string baselineMigrationId,
        CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            insert into public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            values (@migrationId, @productVersion)
            on conflict ("MigrationId") do nothing;
            """;

        AddParameter(command, "@migrationId", baselineMigrationId);
        AddParameter(command, "@productVersion", GetEfProductVersion());
        await ExecuteNonQueryAsync(command, cancellationToken);
    }

    private static string GetEfProductVersion()
    {
        var version = typeof(DbContext).Assembly.GetName().Version
            ?? throw new InvalidOperationException("Unable to resolve EF Core product version.");
        return $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private static void AddParameter(IDbCommand command, string parameterName, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = parameterName;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static async Task<object?> ExecuteScalarAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is System.Data.Common.DbCommand dbCommand)
        {
            return await dbCommand.ExecuteScalarAsync(cancellationToken);
        }

        return command.ExecuteScalar();
    }

    private static async Task<int> ExecuteNonQueryAsync(IDbCommand command, CancellationToken cancellationToken)
    {
        if (command is System.Data.Common.DbCommand dbCommand)
        {
            return await dbCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        return command.ExecuteNonQuery();
    }

    private static async Task<System.Data.Common.DbDataReader> ExecuteReaderAsync(
        IDbCommand command,
        CancellationToken cancellationToken)
    {
        if (command is System.Data.Common.DbCommand dbCommand)
        {
            return await dbCommand.ExecuteReaderAsync(cancellationToken);
        }

        return (System.Data.Common.DbDataReader)command.ExecuteReader();
    }
}
