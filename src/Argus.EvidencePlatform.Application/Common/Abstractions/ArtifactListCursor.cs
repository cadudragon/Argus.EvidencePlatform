namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public sealed record ArtifactListCursor(
    DateTimeOffset CaptureTimestamp,
    DateTimeOffset ReceivedAt,
    Guid Id);
