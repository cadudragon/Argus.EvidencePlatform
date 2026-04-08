namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public sealed record ArtifactListPage(
    IReadOnlyList<EvidenceArtifactListItem> Items,
    ArtifactListCursor? NextCursor);
