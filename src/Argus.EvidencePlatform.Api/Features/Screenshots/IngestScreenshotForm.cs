using Microsoft.AspNetCore.Mvc;

namespace Argus.EvidencePlatform.Api.Features.Screenshots;

public sealed class IngestScreenshotForm
{
    [FromForm(Name = "deviceId")]
    public string DeviceId { get; init; } = string.Empty;

    [FromForm(Name = "sha256")]
    public string Sha256 { get; init; } = string.Empty;

    [FromForm(Name = "caseId")]
    public string CaseId { get; init; } = string.Empty;

    [FromForm(Name = "captureTimestamp")]
    public string CaptureTimestamp { get; init; } = string.Empty;

    [FromForm(Name = "image")]
    public IFormFile? Image { get; init; }
}
