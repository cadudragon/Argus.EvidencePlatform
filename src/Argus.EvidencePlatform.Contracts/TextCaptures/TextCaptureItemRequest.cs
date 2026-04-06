namespace Argus.EvidencePlatform.Contracts.TextCaptures;

public sealed record TextCaptureItemRequest(
    string PackageName,
    string ClassName,
    string? Text,
    string? ContentDescription);
