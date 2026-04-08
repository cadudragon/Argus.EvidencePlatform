namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public interface IEvidenceBlobReader
{
    Task<EvidenceContentStream?> OpenReadAsync(
        string containerName,
        string blobName,
        string? blobVersionId,
        CancellationToken cancellationToken);
}
