namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public interface IDeviceCommandDispatcher
{
    Task<DeviceCommandDispatchResult> RequestScreenshotAsync(
        string deviceId,
        string fcmToken,
        CancellationToken cancellationToken);
}
