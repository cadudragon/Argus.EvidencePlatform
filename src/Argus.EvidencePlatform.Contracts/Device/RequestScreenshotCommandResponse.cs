namespace Argus.EvidencePlatform.Contracts.Device;

public sealed record RequestScreenshotCommandResponse(
    string DeviceId,
    string CaseId,
    string MessageId);
