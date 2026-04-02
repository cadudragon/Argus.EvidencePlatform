using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Evidence;
using Argus.EvidencePlatform.Domain.Evidence;
using Microsoft.EntityFrameworkCore;

namespace Argus.EvidencePlatform.Infrastructure.Persistence.Repositories;

public sealed class EvidenceRepository(ArgusDbContext dbContext) : IEvidenceRepository
{
    public Task AddAsync(EvidenceItem entity, CancellationToken cancellationToken)
    {
        return dbContext.EvidenceItems.AddAsync(entity, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyList<EvidenceTimelineItemResponse>> GetTimelineAsync(
        Guid caseId,
        CancellationToken cancellationToken)
    {
        return await dbContext.EvidenceItems
            .AsNoTracking()
            .Include(x => x.Blob)
            .Where(x => x.CaseId == caseId)
            .OrderByDescending(x => x.CaptureTimestamp)
            .ThenByDescending(x => x.ReceivedAt)
            .Select(x => new EvidenceTimelineItemResponse(
                x.Id,
                x.CaseId,
                x.SourceId,
                x.EvidenceType.ToString(),
                x.CaptureTimestamp,
                x.ReceivedAt,
                x.Status.ToString(),
                x.Classification,
                x.Blob.BlobName,
                x.Blob.Sha256,
                x.Blob.SizeBytes,
                x.Blob.ContentType))
            .ToListAsync(cancellationToken);
    }
}
