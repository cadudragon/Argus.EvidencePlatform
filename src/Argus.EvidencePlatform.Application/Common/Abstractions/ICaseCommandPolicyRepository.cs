using Argus.EvidencePlatform.Domain.Cases;

namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public interface ICaseCommandPolicyRepository
{
    Task<CaseCommandPolicy> GetOrCreateDefaultAsync(
        Guid caseId,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
