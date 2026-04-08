using System.Text;
using System.Text.Json;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Evidence;

namespace Argus.EvidencePlatform.Application.Evidence.ListArtifacts;

public sealed class ListCaseArtifactsHandler(IEvidenceRepository evidenceRepository)
{
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 100;

    public async Task<ListCaseArtifactsResponse> Handle(
        ListCaseArtifactsQuery query,
        CancellationToken cancellationToken)
    {
        var pageSize = NormalizePageSize(query.PageSize);
        var cursor = DecodeCursor(query.Cursor);
        var page = await evidenceRepository.GetArtifactsPageAsync(
            query.CaseId,
            cursor,
            pageSize,
            cancellationToken);

        return new ListCaseArtifactsResponse(
            page.Items.Select(item => new ArtifactListItemResponse(
                item.Id,
                item.CaseId,
                item.SourceId,
                item.ArtifactType,
                item.CaptureTimestamp,
                item.ReceivedAt,
                item.Status,
                item.Classification,
                item.ContentType,
                item.SizeBytes,
                item.Sha256,
                item.HasBinary,
                $"/api/evidence/artifacts/{item.Id:D}/content"))
            .ToList(),
            EncodeCursor(page.NextCursor));
    }

    private static int NormalizePageSize(int? pageSize)
    {
        if (pageSize is null)
        {
            return DefaultPageSize;
        }

        if (pageSize <= 0 || pageSize > MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), $"pageSize must be between 1 and {MaxPageSize}.");
        }

        return pageSize.Value;
    }

    private static ArtifactListCursor? DecodeCursor(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            return JsonSerializer.Deserialize<ArtifactListCursor>(json);
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            throw new ArgumentException("cursor is invalid.", nameof(cursor), ex);
        }
    }

    private static string? EncodeCursor(ArtifactListCursor? cursor)
    {
        if (cursor is null)
        {
            return null;
        }

        var json = JsonSerializer.Serialize(cursor);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }
}
