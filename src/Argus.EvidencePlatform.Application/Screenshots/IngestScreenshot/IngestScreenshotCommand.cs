using Argus.EvidencePlatform.Application.Common.Abstractions;

namespace Argus.EvidencePlatform.Application.Screenshots.IngestScreenshot;

public sealed record IngestScreenshotCommand(
    string CaseId,
    string DeviceId,
    DateTimeOffset CaptureTimestamp,
    StagedBlobDescriptor StagedBlob);
