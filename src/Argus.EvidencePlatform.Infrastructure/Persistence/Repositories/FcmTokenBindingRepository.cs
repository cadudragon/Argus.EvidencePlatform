using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Domain.Devices;
using Microsoft.EntityFrameworkCore;

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Repositories;

public sealed class FcmTokenBindingRepository(ArgusDbContext dbContext) : IFcmTokenBindingRepository
{
    public Task AddAsync(FcmTokenBinding entity, CancellationToken cancellationToken)
    {
        return dbContext.FcmTokenBindings.AddAsync(entity, cancellationToken).AsTask();
    }

    public Task<FcmTokenBinding?> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken)
    {
        return dbContext.FcmTokenBindings
            .SingleOrDefaultAsync(x => x.DeviceId == deviceId, cancellationToken);
    }
}
