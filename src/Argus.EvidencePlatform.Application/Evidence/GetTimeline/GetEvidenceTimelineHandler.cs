using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Evidence;

namespace Argus.EvidencePlatform.Application.Evidence.GetTimeline;

public sealed class GetEvidenceTimelineHandler(IEvidenceRepository evidenceRepository)
{
    public Task<IReadOnlyList<EvidenceTimelineItemResponse>> Handle(
        GetEvidenceTimelineQuery query,
        CancellationToken cancellationToken)
    {
        return evidenceRepository.GetTimelineAsync(query.CaseId, cancellationToken);
    }
}
