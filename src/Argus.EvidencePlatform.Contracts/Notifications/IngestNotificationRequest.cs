namespace Argus.EvidencePlatform.Contracts.Notifications;

public sealed record IngestNotificationRequest(
    string DeviceId,
    string CaseId,
    string Sha256,
    long CaptureTimestamp,
    string PackageName,
    string? Title,
    string? Text,
    string? BigText,
    long Timestamp,
    string? Category);
