using Argus.EvidencePlatform.Domain.Devices;

namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public interface IDeviceSourceRepository
{
    Task AddAsync(DeviceSource entity, CancellationToken cancellationToken);
    Task<DeviceSource?> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken);
}
