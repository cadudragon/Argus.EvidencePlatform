namespace Argus.EvidencePlatform.Contracts.Device;

public sealed record UpdateFcmTokenRequest(
    string DeviceId,
    string FcmToken);
