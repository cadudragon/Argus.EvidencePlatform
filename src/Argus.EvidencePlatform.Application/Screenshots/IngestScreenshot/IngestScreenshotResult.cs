using Argus.EvidencePlatform.Contracts.Screenshots;

namespace Argus.EvidencePlatform.Application.Screenshots.IngestScreenshot;

public sealed record IngestScreenshotResult(
    IngestScreenshotOutcome Outcome,
    IngestScreenshotResponse? Response)
{
    public static IngestScreenshotResult Success(IngestScreenshotResponse response) => new(IngestScreenshotOutcome.Success, response);
    public static IngestScreenshotResult NotFound() => new(IngestScreenshotOutcome.NotFound, null);
    public static IngestScreenshotResult Gone() => new(IngestScreenshotOutcome.Gone, null);
    public static IngestScreenshotResult Conflict() => new(IngestScreenshotOutcome.Conflict, null);
}
