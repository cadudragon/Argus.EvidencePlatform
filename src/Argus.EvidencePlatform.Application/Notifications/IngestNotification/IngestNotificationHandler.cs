using System.Text.Json;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Domain.Audit;
using Argus.EvidencePlatform.Domain.Notifications;
using Wolverine.Attributes;

namespace Argus.EvidencePlatform.Application.Notifications.IngestNotification;

public sealed class IngestNotificationHandler(
    ICaseRepository caseRepository,
    IDeviceSourceRepository deviceSourceRepository,
    INotificationCaptureRepository notificationCaptureRepository,
    IAuditRepository auditRepository,
    IClock clock,
    IUnitOfWork unitOfWork)
{
    [Transactional]
    public async Task<IngestNotificationOutcome> Handle(
        IngestNotificationCommand command,
        CancellationToken cancellationToken)
    {
        var normalizedCaseId = NormalizeRequired(command.CaseId, nameof(command.CaseId));
        var normalizedDeviceId = NormalizeRequired(command.DeviceId, nameof(command.DeviceId));

        var caseId = await caseRepository.GetIdByExternalCaseIdAsync(normalizedCaseId, cancellationToken);
        if (caseId is null)
        {
            return IngestNotificationOutcome.NotFound;
        }

        var deviceSource = await deviceSourceRepository.GetByDeviceIdAsync(normalizedDeviceId, cancellationToken);
        if (deviceSource is null)
        {
            return IngestNotificationOutcome.NotFound;
        }

        var now = clock.UtcNow;
        if (!deviceSource.IsActive(now))
        {
            return IngestNotificationOutcome.Gone;
        }

        if (deviceSource.CaseId != caseId.Value
            || !string.Equals(deviceSource.CaseExternalId, normalizedCaseId, StringComparison.Ordinal))
        {
            return IngestNotificationOutcome.Conflict;
        }

        var notificationCapture = NotificationCapture.Capture(
            Guid.NewGuid(),
            caseId.Value,
            normalizedCaseId,
            normalizedDeviceId,
            command.Sha256,
            command.CaptureTimestamp,
            command.PackageName,
            command.Title,
            command.Text,
            command.BigText,
            command.NotificationTimestamp,
            command.Category,
            now);

        await notificationCaptureRepository.AddAsync(notificationCapture, cancellationToken);
        await auditRepository.AddAsync(
            AuditEntry.Create(
                Guid.NewGuid(),
                caseId.Value,
                AuditActorType.Device,
                normalizedDeviceId,
                "NotificationCaptured",
                nameof(NotificationCapture),
                notificationCapture.Id,
                now,
                Guid.NewGuid().ToString("N"),
                JsonSerializer.Serialize(new
                {
                    CaseId = normalizedCaseId,
                    DeviceId = normalizedDeviceId,
                    notificationCapture.PackageName,
                    notificationCapture.Sha256,
                    NotificationTimestamp = notificationCapture.NotificationTimestamp
                })),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return IngestNotificationOutcome.Success;
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", paramName);
        }

        return value.Trim();
    }
}
