namespace Argus.EvidencePlatform.Contracts.TextCaptures;

public sealed record IngestTextCaptureRequest(
    string DeviceId,
    string CaseId,
    string Sha256,
    long CaptureTimestamp,
    IReadOnlyList<TextCaptureItemRequest> Captures);
