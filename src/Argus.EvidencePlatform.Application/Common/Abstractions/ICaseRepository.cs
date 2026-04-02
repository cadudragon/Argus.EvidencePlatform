using Argus.EvidencePlatform.Contracts.Cases;
using Argus.EvidencePlatform.Domain.Cases;

namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public interface ICaseRepository
{
    Task AddAsync(Case entity, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(Guid caseId, CancellationToken cancellationToken);
    Task<bool> ExistsByExternalCaseIdAsync(string externalCaseId, CancellationToken cancellationToken);
    Task<Guid?> GetIdByExternalCaseIdAsync(string externalCaseId, CancellationToken cancellationToken);
    Task<CaseResponse?> GetByIdAsync(Guid caseId, CancellationToken cancellationToken);
}
