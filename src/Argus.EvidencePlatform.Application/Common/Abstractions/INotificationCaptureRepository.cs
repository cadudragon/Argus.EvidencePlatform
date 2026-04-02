using Argus.EvidencePlatform.Domain.Notifications;

namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public interface INotificationCaptureRepository
{
    Task AddAsync(NotificationCapture entity, CancellationToken cancellationToken);
}
