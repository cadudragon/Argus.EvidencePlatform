using System.Security.Cryptography;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;

namespace Argus.EvidencePlatform.Infrastructure.Storage;

public sealed class AzureBlobStagingService(
    BlobServiceClient blobServiceClient,
    IOptions<BlobStorageOptions> options) : IBlobStagingService
{
    public async Task<StagedBlobDescriptor> StageAsync(
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var tempDirectory = Path.Combine(Path.GetTempPath(), "argus-evidence-platform");
        Directory.CreateDirectory(tempDirectory);

        var extension = Path.GetExtension(originalFileName);
        var tempFile = Path.Combine(tempDirectory, $"{Guid.NewGuid():N}{extension}");

        string sha256;
        long sizeBytes;

        await using (var tempStream = File.Create(tempFile))
        using (var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
        {
            var buffer = new byte[81920];
            int bytesRead;

            while ((bytesRead = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                hasher.AppendData(buffer, 0, bytesRead);
                await tempStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
            }

            sha256 = Convert.ToHexString(hasher.GetHashAndReset()).ToLowerInvariant();
            sizeBytes = tempStream.Length;
        }

        try
        {
            var container = blobServiceClient.GetBlobContainerClient(settings.StagingContainerName);
            await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            var blobName = $"{DateTimeOffset.UtcNow:yyyy/MM/dd}/{Guid.NewGuid():N}{extension}";
            var blobClient = container.GetBlobClient(blobName);

            await using var uploadStream = File.OpenRead(tempFile);
            var response = await blobClient.UploadAsync(
                uploadStream,
                new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = string.IsNullOrWhiteSpace(contentType)
                            ? "application/octet-stream"
                            : contentType
                    },
                    Metadata = new Dictionary<string, string>
                    {
                        ["sha256"] = sha256,
                        ["originalFileName"] = originalFileName
                    }
                },
                cancellationToken);

            return new StagedBlobDescriptor(
                settings.StagingContainerName,
                blobName,
                string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
                sizeBytes,
                sha256,
                response.Value.VersionId);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
