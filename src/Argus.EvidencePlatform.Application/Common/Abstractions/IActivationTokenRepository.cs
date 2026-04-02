using Argus.EvidencePlatform.Domain.Enrollment;

namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public interface IActivationTokenRepository
{
    Task AddAsync(ActivationToken entity, CancellationToken cancellationToken);
    Task<ActivationToken?> GetByTokenAsync(string token, CancellationToken cancellationToken);
}
