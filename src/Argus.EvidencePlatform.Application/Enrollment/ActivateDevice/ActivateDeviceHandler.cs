using System.Text.Json;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Enrollment;
using Argus.EvidencePlatform.Domain.Audit;
using Argus.EvidencePlatform.Domain.Devices;
using Wolverine.Attributes;

namespace Argus.EvidencePlatform.Application.Enrollment.ActivateDevice;

public sealed class ActivateDeviceHandler(
    IActivationTokenRepository activationTokenRepository,
    IDeviceSourceRepository deviceSourceRepository,
    IAuditRepository auditRepository,
    IClock clock,
    IUnitOfWork unitOfWork)
{
    private static readonly string[] DefaultScope = ["screenshot", "notification", "text"];

    [Transactional]
    public async Task<ActivateDeviceResult> Handle(
        ActivateDeviceCommand command,
        CancellationToken cancellationToken)
    {
        var normalizedToken = NormalizeRequired(command.Token, nameof(command.Token));
        var normalizedDeviceId = NormalizeRequired(command.DeviceId, nameof(command.DeviceId));
        var activationToken = await activationTokenRepository.GetByTokenAsync(normalizedToken, cancellationToken);
        if (activationToken is null)
        {
            return ActivateDeviceResult.NotFound();
        }

        var now = clock.UtcNow;
        if (activationToken.IsConsumed || activationToken.IsExpired(now))
        {
            return ActivateDeviceResult.Gone();
        }

        var deviceSource = await deviceSourceRepository.GetByDeviceIdAsync(normalizedDeviceId, cancellationToken);
        if (deviceSource is null)
        {
            deviceSource = DeviceSource.Register(
                Guid.NewGuid(),
                normalizedDeviceId,
                activationToken.CaseId,
                activationToken.CaseExternalId,
                now,
                activationToken.ValidUntil);
            await deviceSourceRepository.AddAsync(deviceSource, cancellationToken);
        }
        else
        {
            deviceSource.RenewEnrollment(
                activationToken.CaseId,
                activationToken.CaseExternalId,
                now,
                activationToken.ValidUntil);
        }

        activationToken.Consume(normalizedDeviceId, now);

        await auditRepository.AddAsync(
            AuditEntry.Create(
                Guid.NewGuid(),
                activationToken.CaseId,
                AuditActorType.Device,
                normalizedDeviceId,
                "DeviceActivated",
                nameof(DeviceSource),
                deviceSource.Id,
                now,
                Guid.NewGuid().ToString("N"),
                JsonSerializer.Serialize(new
                {
                    activationToken.CaseExternalId,
                    activationToken.ValidUntil,
                    DeviceId = normalizedDeviceId
                })),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ActivateDeviceResult.Success(new ActivationSuccessResponse(
            activationToken.CaseExternalId,
            activationToken.ValidUntil.ToUnixTimeMilliseconds(),
            DefaultScope));
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
