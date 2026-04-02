using Argus.EvidencePlatform.Application.Common.Abstractions;

namespace Argus.EvidencePlatform.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
