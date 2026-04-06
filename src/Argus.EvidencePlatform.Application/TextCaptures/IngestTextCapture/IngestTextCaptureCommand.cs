namespace Argus.EvidencePlatform.Application.TextCaptures.IngestTextCapture;

public sealed record IngestTextCaptureCommand(
    string DeviceId,
    string CaseId,
    string Sha256,
    DateTimeOffset CaptureTimestamp,
    IReadOnlyList<TextCapturePayload> Captures);
