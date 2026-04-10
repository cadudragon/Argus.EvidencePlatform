namespace Argus.EvidencePlatform.Application.Device.BindFcmToken;

public sealed record BindFcmTokenCommand(
    string DeviceId,
    string FcmToken,
    FcmCommandKeyInput FcmCommandKey);
