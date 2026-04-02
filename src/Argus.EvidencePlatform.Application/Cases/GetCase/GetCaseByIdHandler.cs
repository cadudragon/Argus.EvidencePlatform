using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Cases;

namespace Argus.EvidencePlatform.Application.Cases.GetCase;

public sealed class GetCaseByIdHandler(ICaseRepository caseRepository)
{
    public Task<CaseResponse?> Handle(GetCaseByIdQuery query, CancellationToken cancellationToken)
    {
        return caseRepository.GetByIdAsync(query.CaseId, cancellationToken);
    }
}
