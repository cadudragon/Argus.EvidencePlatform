using Argus.EvidencePlatform.Application.Common.Abstractions;
using Wolverine.Attributes;

namespace Argus.EvidencePlatform.Application.Device.RecordPong;

public sealed class RecordPongHandler(
    IDeviceSourceRepository deviceSourceRepository,
    IClock clock,
    IUnitOfWork unitOfWork)
{
    [Transactional]
    public async Task<RecordPongOutcome> Handle(
        RecordPongCommand command,
        CancellationToken cancellationToken)
    {
        var normalizedDeviceId = NormalizeRequired(command.DeviceId, nameof(command.DeviceId));
        var deviceSource = await deviceSourceRepository.GetByDeviceIdAsync(normalizedDeviceId, cancellationToken);
        var now = clock.UtcNow;
        if (deviceSource is null || !deviceSource.IsActive(now))
        {
            return RecordPongOutcome.Gone;
        }

        deviceSource.RecordPong(now);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return RecordPongOutcome.Success;
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
