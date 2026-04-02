using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Domain.Devices;
using Microsoft.EntityFrameworkCore;

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Repositories;

public sealed class DeviceSourceRepository(ArgusDbContext dbContext) : IDeviceSourceRepository
{
    public Task AddAsync(DeviceSource entity, CancellationToken cancellationToken)
    {
        return dbContext.DeviceSources.AddAsync(entity, cancellationToken).AsTask();
    }

    public Task<DeviceSource?> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken)
    {
        return dbContext.DeviceSources
            .SingleOrDefaultAsync(x => x.DeviceId == deviceId, cancellationToken);
    }
}
