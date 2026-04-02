using Azure.Storage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Argus.EvidencePlatform.Infrastructure.HealthChecks;

public sealed class BlobStorageHealthCheck(BlobServiceClient blobServiceClient) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await blobServiceClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Blob Storage connectivity failed.", ex);
        }
    }
}
