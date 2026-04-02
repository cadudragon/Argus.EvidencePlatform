using Argus.EvidencePlatform.Domain.Devices;

namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public interface IFcmTokenBindingRepository
{
    Task AddAsync(FcmTokenBinding entity, CancellationToken cancellationToken);
    Task<FcmTokenBinding?> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken);
}
