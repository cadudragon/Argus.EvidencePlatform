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

    public async Task<ArtifactListPage> GetArtifactsPageAsync(
        Guid caseId,
        ArtifactListCursor? cursor,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.EvidenceItems
            .AsNoTracking()
            .Include(x => x.Blob)
            .Where(x => x.CaseId == caseId);

        if (cursor is not null)
        {
            query = query.Where(x =>
                x.CaptureTimestamp < cursor.CaptureTimestamp
                || (x.CaptureTimestamp == cursor.CaptureTimestamp && x.ReceivedAt < cursor.ReceivedAt)
                || (x.CaptureTimestamp == cursor.CaptureTimestamp && x.ReceivedAt == cursor.ReceivedAt && x.Id.CompareTo(cursor.Id) < 0));
        }

        var items = await query
            .OrderByDescending(x => x.CaptureTimestamp)
            .ThenByDescending(x => x.ReceivedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => new EvidenceArtifactListItem(
                x.Id,
                x.CaseId,
                x.SourceId,
                x.EvidenceType.ToString(),
                x.CaptureTimestamp,
                x.ReceivedAt,
                x.Status.ToString(),
                x.Classification,
                x.Blob.ContentType,
                x.Blob.SizeBytes,
                x.Blob.Sha256,
                true))
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        ArtifactListCursor? nextCursor = null;
        if (items.Count > pageSize)
        {
            var lastIncluded = items[pageSize - 1];
            nextCursor = new ArtifactListCursor(lastIncluded.CaptureTimestamp, lastIncluded.ReceivedAt, lastIncluded.Id);
            items.RemoveAt(items.Count - 1);
        }

        return new ArtifactListPage(items, nextCursor);
    }

    public async Task<EvidenceArtifactDescriptor?> GetArtifactDescriptorAsync(Guid artifactId, CancellationToken cancellationToken)
    {
        return await dbContext.EvidenceItems
            .AsNoTracking()
            .Include(x => x.Blob)
            .Where(x => x.Id == artifactId)
            .Select(x => new EvidenceArtifactDescriptor(
                x.Id,
                x.CaseId,
                x.SourceId,
                x.EvidenceType.ToString(),
                x.CaptureTimestamp,
                x.ReceivedAt,
                x.Status.ToString(),
                x.Classification,
                x.Blob.ContainerName,
                x.Blob.BlobName,
                x.Blob.BlobVersionId,
                x.Blob.ContentType,
                x.Blob.SizeBytes,
                x.Blob.Sha256))
            .SingleOrDefaultAsync(cancellationToken);
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
