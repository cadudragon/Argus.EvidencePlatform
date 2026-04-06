namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public interface IDeviceCommandDispatcher
{
    Task<DeviceCommandDispatchResult> RequestScreenshotAsync(
        Guid firebaseAppId,
        string deviceId,
        string fcmToken,
        CancellationToken cancellationToken);
}
