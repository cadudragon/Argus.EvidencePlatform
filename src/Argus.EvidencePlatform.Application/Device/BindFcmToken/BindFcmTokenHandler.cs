using System.Text.Json;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Domain.Audit;
using Argus.EvidencePlatform.Domain.Devices;
using Wolverine.Attributes;

namespace Argus.EvidencePlatform.Application.Device.BindFcmToken;

public sealed class BindFcmTokenHandler(
    IDeviceSourceRepository deviceSourceRepository,
    IFirebaseAppRoutingResolver firebaseAppRoutingResolver,
    IFcmTokenBindingRepository fcmTokenBindingRepository,
    IAuditRepository auditRepository,
    IClock clock,
    IUnitOfWork unitOfWork)
{
    [Transactional]
    public async Task<BindFcmTokenOutcome> Handle(
        BindFcmTokenCommand command,
        CancellationToken cancellationToken)
    {
        var normalizedDeviceId = NormalizeRequired(command.DeviceId, nameof(command.DeviceId));
        var normalizedFcmToken = NormalizeRequired(command.FcmToken, nameof(command.FcmToken));
        var deviceSource = await deviceSourceRepository.GetByDeviceIdAsync(normalizedDeviceId, cancellationToken);
        var now = clock.UtcNow;
        if (deviceSource is null || !deviceSource.IsActive(now))
        {
            return BindFcmTokenOutcome.Gone;
        }

        var firebaseApp = await firebaseAppRoutingResolver.ResolveForCaseAsync(deviceSource.CaseId, cancellationToken);
        if (firebaseApp is null)
        {
            return BindFcmTokenOutcome.Gone;
        }

        var binding = await fcmTokenBindingRepository.GetByDeviceIdAsync(normalizedDeviceId, cancellationToken);
        if (binding is null)
        {
            binding = FcmTokenBinding.Bind(Guid.NewGuid(), firebaseApp.FirebaseAppId, normalizedDeviceId, normalizedFcmToken, now);
            await fcmTokenBindingRepository.AddAsync(binding, cancellationToken);
        }
        else
        {
            binding.UpdateToken(firebaseApp.FirebaseAppId, normalizedFcmToken, now);
        }

        await auditRepository.AddAsync(
            AuditEntry.Create(
                Guid.NewGuid(),
                deviceSource.CaseId,
                AuditActorType.Device,
                normalizedDeviceId,
                "FcmTokenBound",
                nameof(FcmTokenBinding),
                binding.Id,
                now,
                Guid.NewGuid().ToString("N"),
                JsonSerializer.Serialize(new
                {
                    deviceSource.CaseExternalId,
                    DeviceId = normalizedDeviceId,
                    firebaseApp.Key,
                    firebaseApp.ProjectId
                })),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return BindFcmTokenOutcome.Success;
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
