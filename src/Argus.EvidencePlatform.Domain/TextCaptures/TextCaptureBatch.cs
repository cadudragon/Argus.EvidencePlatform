namespace Argus.EvidencePlatform.Domain.TextCaptures;

public sealed class TextCaptureBatch
{
    public Guid Id { get; private set; }
    public Guid CaseId { get; private set; }
    public string CaseExternalId { get; private set; } = string.Empty;
    public string DeviceId { get; private set; } = string.Empty;
    public string Sha256 { get; private set; } = string.Empty;
    public DateTimeOffset CaptureTimestamp { get; private set; }
    public int CaptureCount { get; private set; }
    public string PayloadJson { get; private set; } = string.Empty;
    public string PackageNamesJson { get; private set; } = string.Empty;
    public DateTimeOffset ReceivedAt { get; private set; }

    private TextCaptureBatch()
    {
    }

    public static TextCaptureBatch Capture(
        Guid id,
        Guid caseId,
        string caseExternalId,
        string deviceId,
        string sha256,
        DateTimeOffset captureTimestamp,
        int captureCount,
        string payloadJson,
        string packageNamesJson,
        DateTimeOffset receivedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(id));
        }

        if (caseId == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(caseId));
        }

        if (captureTimestamp == default)
        {
            throw new ArgumentException("Value cannot be default.", nameof(captureTimestamp));
        }

        if (captureCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(captureCount), "Value must be greater than zero.");
        }

        if (receivedAt == default)
        {
            throw new ArgumentException("Value cannot be default.", nameof(receivedAt));
        }

        return new TextCaptureBatch
        {
            Id = id,
            CaseId = caseId,
            CaseExternalId = NormalizeRequired(caseExternalId, nameof(caseExternalId)),
            DeviceId = NormalizeRequired(deviceId, nameof(deviceId)),
            Sha256 = NormalizeRequired(sha256, nameof(sha256)),
            CaptureTimestamp = captureTimestamp,
            CaptureCount = captureCount,
            PayloadJson = NormalizeRequired(payloadJson, nameof(payloadJson)),
            PackageNamesJson = NormalizeRequired(packageNamesJson, nameof(packageNamesJson)),
            ReceivedAt = receivedAt
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
}
