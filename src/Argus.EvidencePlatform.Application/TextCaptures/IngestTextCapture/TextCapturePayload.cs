namespace Argus.EvidencePlatform.Application.TextCaptures.IngestTextCapture;

public sealed record TextCapturePayload(
    string PackageName,
    string ClassName,
    string? Text,
    string? ContentDescription);
