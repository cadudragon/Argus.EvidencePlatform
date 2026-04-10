namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public interface IDeviceCommandDispatcher
{
    Task<DeviceCommandDispatchResult> DispatchAsync(
        DeviceCommandDispatchRequest request,
        CancellationToken cancellationToken);
}
