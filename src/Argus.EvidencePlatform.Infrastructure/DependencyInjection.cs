using Argus.EvidencePlatform.Application;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Infrastructure.Bootstrap;
using Argus.EvidencePlatform.Infrastructure.HealthChecks;
using Argus.EvidencePlatform.Infrastructure.Persistence;
using Argus.EvidencePlatform.Infrastructure.Persistence.Repositories;
using Argus.EvidencePlatform.Infrastructure.Firebase;
using Argus.EvidencePlatform.Infrastructure.Storage;
using Argus.EvidencePlatform.Infrastructure.Time;
using Azure.Core;
using Azure.Identity;
using Azure.Storage.Blobs;
using JasperFx.Resources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Persistence;
using Wolverine.Postgresql;

namespace Argus.EvidencePlatform.Infrastructure;

public static class DependencyInjection
{
    private const string DefaultPostgresConnectionString =
        "Host=localhost;Port=5432;Database=argus_evidence_platform;Username=postgres;Password=postgres";

    public static IHostApplicationBuilder AddInfrastructure(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("postgresdb")
            ?? DefaultPostgresConnectionString;
        var useInMemoryPersistence = builder.Environment.IsEnvironment("Testing")
            || builder.Configuration.GetValue("Infrastructure:UseInMemoryPersistence", false);

        builder.Services
            .AddOptions<BlobStorageOptions>()
            .Bind(builder.Configuration.GetSection(BlobStorageOptions.SectionName));
        builder.Services
            .AddOptions<FirebaseOptions>()
            .Bind(builder.Configuration.GetSection(FirebaseOptions.SectionName));
        builder.Services
            .AddOptions<InfrastructureBootstrapOptions>()
            .Bind(builder.Configuration.GetSection(InfrastructureBootstrapOptions.SectionName));

        builder.Services.AddSingleton<TokenCredential, DefaultAzureCredential>();
        builder.Services.AddSingleton(sp => CreateBlobServiceClient(sp, builder.Configuration));
        builder.Services.AddSingleton<FirebaseAppRegistry>();
        builder.Services.AddSingleton<RelationalDatabaseMigrator>();
        builder.Services.AddHostedService<FirebaseBootstrapService>();
        builder.Services.AddSingleton<IDeviceCommandDispatcher, FirebaseDeviceCommandDispatcher>();
        builder.Services.AddSingleton<IClock, SystemClock>();
        builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        builder.Services.AddScoped<IBlobStagingService, AzureBlobStagingService>();
        builder.Services.AddScoped<ICaseRepository, CaseRepository>();
        builder.Services.AddScoped<IActivationTokenRepository, ActivationTokenRepository>();
        builder.Services.AddScoped<IDeviceSourceRepository, DeviceSourceRepository>();
        builder.Services.AddScoped<IFcmTokenBindingRepository, FcmTokenBindingRepository>();
        builder.Services.AddScoped<IFirebaseAppRepository, FirebaseAppRepository>();
        builder.Services.AddScoped<IFirebaseAppRoutingResolver, FirebaseAppRoutingResolver>();
        builder.Services.AddScoped<INotificationCaptureRepository, NotificationCaptureRepository>();
        builder.Services.AddScoped<ITextCaptureBatchRepository, TextCaptureBatchRepository>();
        builder.Services.AddScoped<IEvidenceRepository, EvidenceRepository>();
        builder.Services.AddScoped<IExportJobRepository, ExportJobRepository>();
        builder.Services.AddScoped<IAuditRepository, AuditRepository>();

        builder.Services.AddHealthChecks()
            .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"])
            .AddCheck<BlobStorageHealthCheck>("blob-storage", tags: ["ready"]);

        if (!useInMemoryPersistence
            && builder.Configuration.GetValue("Infrastructure:BootstrapOnStartup", false))
        {
            builder.Services.AddHostedService<InfrastructureBootstrapService>();
        }

        if (!useInMemoryPersistence)
        {
            builder.Services.AddHostedService<FirebaseConfigurationBootstrapService>();
        }

        if (!useInMemoryPersistence && builder.Configuration.GetValue("Wolverine:AutoProvision", false))
        {
            builder.Services.AddResourceSetupOnStartup(StartupAction.SetupOnly);
        }

        if (useInMemoryPersistence)
        {
            var inMemoryDatabaseName = builder.Configuration.GetValue("Infrastructure:InMemoryDatabaseName", "argus-evidence-platform-tests");
            builder.Services.AddDbContext<ArgusDbContext>(
                db => db.UseInMemoryDatabase(inMemoryDatabaseName));
        }

        builder.UseWolverine(options =>
        {
            options.Durability.Mode = DurabilityMode.Solo;
            options.Discovery.IncludeAssembly(typeof(Argus.EvidencePlatform.Application.AssemblyMarker).Assembly);
            options.Discovery.IncludeAssembly(typeof(AssemblyMarker).Assembly);
            options.Policies.AutoApplyTransactions();

            if (useInMemoryPersistence)
            {
                return;
            }

            options.PersistMessagesWithPostgresql(connectionString, "wolverine");
            options.UseEntityFrameworkCoreTransactions(TransactionMiddlewareMode.Lightweight);
            options.Policies.UseDurableLocalQueues();
            options.Services.AddDbContextWithWolverineIntegration<ArgusDbContext>(
                db => db.UseNpgsql(connectionString),
                "argus");
        });

        return builder;
    }

    private static BlobServiceClient CreateBlobServiceClient(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        var options = serviceProvider.GetRequiredService<
            Microsoft.Extensions.Options.IOptions<BlobStorageOptions>>().Value;

        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return new BlobServiceClient(options.ConnectionString);
        }

        var namedConnectionString = configuration.GetConnectionString(options.ConnectionName);
        if (!string.IsNullOrWhiteSpace(namedConnectionString))
        {
            return new BlobServiceClient(namedConnectionString);
        }

        if (!string.IsNullOrWhiteSpace(options.ServiceUri))
        {
            return new BlobServiceClient(
                new Uri(options.ServiceUri),
                serviceProvider.GetRequiredService<TokenCredential>());
        }

        return new BlobServiceClient("UseDevelopmentStorage=true");
    }
}
