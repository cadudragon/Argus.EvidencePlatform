using Argus.EvidencePlatform.Application.Common.Abstractions;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Argus.EvidencePlatform.Infrastructure.Storage;

public sealed class AzureEvidenceBlobReader(BlobServiceClient blobServiceClient) : IEvidenceBlobReader
{
    public async Task<EvidenceContentStream?> OpenReadAsync(
        string containerName,
        string blobName,
        string? blobVersionId,
        CancellationToken cancellationToken)
    {
        var blobClient = blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);

        if (!string.IsNullOrWhiteSpace(blobVersionId))
        {
            blobClient = blobClient.WithVersion(blobVersionId);
        }

        try
        {
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            var stream = await blobClient.OpenReadAsync(cancellationToken: cancellationToken);

            return new EvidenceContentStream(
                stream,
                properties.Value.ContentType ?? "application/octet-stream",
                properties.Value.ContentLength,
                properties.Value.LastModified,
                SupportsRangeProcessing: true,
                Path.GetFileName(blobName));
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }
}
