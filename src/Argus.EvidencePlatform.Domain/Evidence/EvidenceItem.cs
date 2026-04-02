namespace Argus.EvidencePlatform.Domain.Evidence;

public sealed class EvidenceItem
{
    public Guid Id { get; private set; }
    public Guid CaseId { get; private set; }
    public string SourceId { get; private set; } = string.Empty;
    public EvidenceType EvidenceType { get; private set; }
    public DateTimeOffset CaptureTimestamp { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public EvidenceStatus Status { get; private set; }
    public string? Classification { get; private set; }
    public EvidenceBlob Blob { get; private set; } = null!;

    private EvidenceItem()
    {
    }

    public static EvidenceItem Preserve(
        Guid id,
        Guid caseId,
        string sourceId,
        EvidenceType evidenceType,
        DateTimeOffset captureTimestamp,
        DateTimeOffset receivedAt,
        string? classification,
        EvidenceBlob blob)
    {
        if (caseId == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(caseId));
        }

        if (captureTimestamp == default)
        {
            throw new ArgumentException("Value cannot be default.", nameof(captureTimestamp));
        }

        if (receivedAt == default)
        {
            throw new ArgumentException("Value cannot be default.", nameof(receivedAt));
        }

        if (!Enum.IsDefined(evidenceType))
        {
            throw new ArgumentOutOfRangeException(nameof(evidenceType));
        }

        ArgumentNullException.ThrowIfNull(blob);

        if (blob.EvidenceItemId != id)
        {
            throw new ArgumentException("Blob must belong to the evidence item being preserved.", nameof(blob));
        }

        return new EvidenceItem
        {
            Id = id,
            CaseId = caseId,
            SourceId = NormalizeRequired(sourceId, nameof(sourceId)),
            EvidenceType = evidenceType,
            CaptureTimestamp = captureTimestamp,
            ReceivedAt = receivedAt,
            Status = EvidenceStatus.Preserved,
            Classification = NormalizeOptional(classification),
            Blob = blob
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

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
