using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Argus.EvidencePlatform.Infrastructure.Firebase;

public sealed class FirebaseBootstrapService(
    FirebaseAppAccessor accessor,
    ILogger<FirebaseBootstrapService> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (accessor.IsConfigured)
        {
            logger.LogInformation("Firebase bootstrap completed.");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

