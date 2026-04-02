namespace Argus.EvidencePlatform.Contracts.Screenshots;

public sealed record IngestScreenshotResponse(
    Guid ReceiptId,
    Guid EvidenceId,
    string CaseId,
    string DeviceId,
    string Sha256,
    long SizeBytes,
    DateTimeOffset ReceivedAt,
    string Status);
