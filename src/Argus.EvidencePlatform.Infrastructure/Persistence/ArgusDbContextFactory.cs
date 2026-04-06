using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Argus.EvidencePlatform.Infrastructure.Persistence;

public sealed class ArgusDbContextFactory : IDesignTimeDbContextFactory<ArgusDbContext>
{
    private const string DefaultPostgresConnectionString =
        "Host=localhost;Port=5432;Database=argus_evidence_platform;Username=postgres;Password=postgres";

    public ArgusDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString();
        var optionsBuilder = new DbContextOptionsBuilder<ArgusDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new ArgusDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString()
    {
        return Environment.GetEnvironmentVariable("ARGUS_EVIDENCE_PLATFORM_POSTGRES")
            ?? DefaultPostgresConnectionString;
    }
}
