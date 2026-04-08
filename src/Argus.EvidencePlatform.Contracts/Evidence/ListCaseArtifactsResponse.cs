namespace Argus.EvidencePlatform.Contracts.Evidence;

public sealed record ListCaseArtifactsResponse(
    IReadOnlyList<ArtifactListItemResponse> Items,
    string? NextCursor);
