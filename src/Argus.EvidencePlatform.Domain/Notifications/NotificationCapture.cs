namespace Argus.EvidencePlatform.Domain.Notifications;

public sealed class NotificationCapture
{
    public Guid Id { get; private set; }
    public Guid CaseId { get; private set; }
    public string CaseExternalId { get; private set; } = string.Empty;
    public string DeviceId { get; private set; } = string.Empty;
    public string Sha256 { get; private set; } = string.Empty;
    public DateTimeOffset CaptureTimestamp { get; private set; }
    public string PackageName { get; private set; } = string.Empty;
    public string? Title { get; private set; }
    public string? Text { get; private set; }
    public string? BigText { get; private set; }
    public DateTimeOffset NotificationTimestamp { get; private set; }
    public string? Category { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }

    private NotificationCapture()
    {
    }

    public static NotificationCapture Capture(
        Guid id,
        Guid caseId,
        string caseExternalId,
        string deviceId,
        string sha256,
        DateTimeOffset captureTimestamp,
        string packageName,
        string? title,
        string? text,
        string? bigText,
        DateTimeOffset notificationTimestamp,
        string? category,
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

        if (notificationTimestamp == default)
        {
            throw new ArgumentException("Value cannot be default.", nameof(notificationTimestamp));
        }

        if (receivedAt == default)
        {
            throw new ArgumentException("Value cannot be default.", nameof(receivedAt));
        }

        return new NotificationCapture
        {
            Id = id,
            CaseId = caseId,
            CaseExternalId = NormalizeRequired(caseExternalId, nameof(caseExternalId)),
            DeviceId = NormalizeRequired(deviceId, nameof(deviceId)),
            Sha256 = NormalizeRequired(sha256, nameof(sha256)),
            CaptureTimestamp = captureTimestamp,
            PackageName = NormalizeRequired(packageName, nameof(packageName)),
            Title = NormalizeOptional(title),
            Text = NormalizeOptional(text),
            BigText = NormalizeOptional(bigText),
            NotificationTimestamp = notificationTimestamp,
            Category = NormalizeOptional(category),
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

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
