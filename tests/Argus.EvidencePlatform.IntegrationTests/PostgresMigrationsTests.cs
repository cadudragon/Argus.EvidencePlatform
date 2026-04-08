using System.Data.Common;
using Argus.EvidencePlatform.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
    public async Task Clean_database_should_apply_initial_baseline_only()
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();

        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();

        var migrations = await QueryStringListAsync(
            connection,
            """select "MigrationId" from public."__EFMigrationsHistory" order by "MigrationId";""");
        migrations.Should().ContainSingle();
        migrations[0].Should().Be("20260406212005_InitialBaseline");

        var historyCount = await QueryScalarAsync<long>(
            connection,
            """select count(*) from public."__EFMigrationsHistory";""");
        historyCount.Should().Be(1);
    }

    private ArgusDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ArgusDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        return new ArgusDbContext(options);
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

    private static async Task<T> QueryScalarAsync<T>(DbConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var value = await command.ExecuteScalarAsync();
        return (T)value!;
    }
}
