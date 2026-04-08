using Argus.EvidencePlatform.Contracts.Evidence;
using Argus.EvidencePlatform.Domain.Evidence;

namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public interface IEvidenceRepository
{
    Task AddAsync(EvidenceItem entity, CancellationToken cancellationToken);
    Task<IReadOnlyList<EvidenceTimelineItemResponse>> GetTimelineAsync(Guid caseId, CancellationToken cancellationToken);
    Task<ArtifactListPage> GetArtifactsPageAsync(
        Guid caseId,
        ArtifactListCursor? cursor,
        int pageSize,
        CancellationToken cancellationToken);
    Task<EvidenceArtifactDescriptor?> GetArtifactDescriptorAsync(Guid artifactId, CancellationToken cancellationToken);
}
