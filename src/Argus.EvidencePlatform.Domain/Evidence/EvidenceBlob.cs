namespace Argus.EvidencePlatform.Domain.Evidence;

public sealed class EvidenceBlob
{
    public Guid Id { get; private set; }
    public Guid EvidenceItemId { get; private set; }
    public string ContainerName { get; private set; } = string.Empty;
    public string BlobName { get; private set; } = string.Empty;
    public string? BlobVersionId { get; private set; }
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string Sha256 { get; private set; } = string.Empty;
    public string ImmutabilityState { get; private set; } = "pending";
    public string LegalHoldState { get; private set; } = "none";
    public DateTimeOffset StoredAt { get; private set; }

    private EvidenceBlob()
    {
    }

    public static EvidenceBlob Create(
        Guid id,
        Guid evidenceItemId,
        string containerName,
        string blobName,
        string? blobVersionId,
        string contentType,
        long sizeBytes,
        string sha256,
        DateTimeOffset storedAt)
    {
        if (evidenceItemId == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(evidenceItemId));
        }

        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Value must be greater than zero.");
        }

        var normalizedContainerName = NormalizeRequired(containerName, nameof(containerName));
        var normalizedBlobName = NormalizeRequired(blobName, nameof(blobName));
        var normalizedContentType = NormalizeRequired(contentType, nameof(contentType));
        var normalizedSha256 = NormalizeSha256(sha256, nameof(sha256));

        return new EvidenceBlob
        {
            Id = id,
            EvidenceItemId = evidenceItemId,
            ContainerName = normalizedContainerName,
            BlobName = normalizedBlobName,
            BlobVersionId = blobVersionId,
            ContentType = normalizedContentType,
            SizeBytes = sizeBytes,
            Sha256 = normalizedSha256,
            StoredAt = storedAt
        };
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", paramName);
        }

        return value.Trim();
    }

    private static string NormalizeSha256(string value, string paramName)
    {
        var normalized = NormalizeRequired(value, paramName).ToLowerInvariant();
        if (normalized.Length != 64 || !normalized.All(Uri.IsHexDigit))
        {
            throw new ArgumentException("Value must be a valid SHA-256 hex string.", paramName);
        }

        return normalized;
    }
}
