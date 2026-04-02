namespace Argus.EvidencePlatform.Application.Notifications.IngestNotification;

public sealed record IngestNotificationCommand(
    string DeviceId,
    string CaseId,
    string Sha256,
    DateTimeOffset CaptureTimestamp,
    string PackageName,
    string? Title,
    string? Text,
    string? BigText,
    DateTimeOffset NotificationTimestamp,
    string? Category);
