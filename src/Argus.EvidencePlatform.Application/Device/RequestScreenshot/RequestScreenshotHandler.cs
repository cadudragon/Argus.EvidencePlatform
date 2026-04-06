using System.Text.Json;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Device;
using Argus.EvidencePlatform.Domain.Audit;
using Wolverine.Attributes;

namespace Argus.EvidencePlatform.Application.Device.RequestScreenshot;

public sealed class RequestScreenshotHandler(
    IDeviceSourceRepository deviceSourceRepository,
    IFirebaseAppRoutingResolver firebaseAppRoutingResolver,
    IFcmTokenBindingRepository fcmTokenBindingRepository,
    IDeviceCommandDispatcher deviceCommandDispatcher,
    IAuditRepository auditRepository,
    IClock clock,
    IUnitOfWork unitOfWork)
{
    [Transactional]
    public async Task<RequestScreenshotResult> Handle(
        RequestScreenshotCommand command,
        CancellationToken cancellationToken)
    {
        var normalizedDeviceId = NormalizeRequired(command.DeviceId, nameof(command.DeviceId));
        var now = clock.UtcNow;

        var deviceSource = await deviceSourceRepository.GetByDeviceIdAsync(normalizedDeviceId, cancellationToken);
        if (deviceSource is null)
        {
            return RequestScreenshotResult.NotFound();
        }

        if (!deviceSource.IsActive(now))
        {
            return RequestScreenshotResult.Gone();
        }

        var firebaseApp = await firebaseAppRoutingResolver.ResolveForCaseAsync(deviceSource.CaseId, cancellationToken);
        if (firebaseApp is null)
        {
            return RequestScreenshotResult.Failed();
        }

        var binding = await fcmTokenBindingRepository.GetByDeviceIdAsync(normalizedDeviceId, cancellationToken);
        if (binding is null)
        {
            return RequestScreenshotResult.NotFound();
        }

        var dispatch = await deviceCommandDispatcher.RequestScreenshotAsync(
            firebaseApp.FirebaseAppId,
            normalizedDeviceId,
            binding.FcmToken,
            cancellationToken);

        if (dispatch.Status == DeviceCommandDispatchStatus.TokenInvalid)
        {
            await fcmTokenBindingRepository.RemoveAsync(binding, cancellationToken);
            await auditRepository.AddAsync(
                AuditEntry.Create(
                    Guid.NewGuid(),
                    deviceSource.CaseId,
                    AuditActorType.System,
                    "system",
                    "FcmTokenInvalidated",
                    binding.GetType().Name,
                    binding.Id,
                    now,
                    Guid.NewGuid().ToString("N"),
                    JsonSerializer.Serialize(new
                    {
                        deviceSource.CaseExternalId,
                        DeviceId = normalizedDeviceId
                    })),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return RequestScreenshotResult.Gone();
        }

        if (dispatch.Status != DeviceCommandDispatchStatus.Success || string.IsNullOrWhiteSpace(dispatch.MessageId))
        {
            return RequestScreenshotResult.Failed();
        }

        await auditRepository.AddAsync(
            AuditEntry.Create(
                Guid.NewGuid(),
                deviceSource.CaseId,
                AuditActorType.System,
                "system",
                "ScreenshotCommandRequested",
                nameof(RequestScreenshotCommand),
                deviceSource.Id,
                now,
                Guid.NewGuid().ToString("N"),
                JsonSerializer.Serialize(new
                {
                    deviceSource.CaseExternalId,
                    DeviceId = normalizedDeviceId,
                    firebaseApp.Key,
                    firebaseApp.ProjectId,
                    dispatch.MessageId
                })),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RequestScreenshotResult.Success(new RequestScreenshotCommandResponse(
            normalizedDeviceId,
            deviceSource.CaseExternalId,
            dispatch.MessageId));
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
