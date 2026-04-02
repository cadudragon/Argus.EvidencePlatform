using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Domain.Notifications;

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Repositories;

public sealed class NotificationCaptureRepository(ArgusDbContext dbContext) : INotificationCaptureRepository
{
    public Task AddAsync(NotificationCapture entity, CancellationToken cancellationToken)
    {
        return dbContext.NotificationCaptures.AddAsync(entity, cancellationToken).AsTask();
    }
}
