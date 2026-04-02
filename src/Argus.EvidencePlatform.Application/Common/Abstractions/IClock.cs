namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
