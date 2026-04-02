using Argus.EvidencePlatform.Contracts.Device;

namespace Argus.EvidencePlatform.Application.Device.RequestScreenshot;

public sealed record RequestScreenshotResult(
    RequestScreenshotOutcome Outcome,
    RequestScreenshotCommandResponse? Response)
{
    public static RequestScreenshotResult Success(RequestScreenshotCommandResponse response) => new(RequestScreenshotOutcome.Success, response);
    public static RequestScreenshotResult NotFound() => new(RequestScreenshotOutcome.NotFound, null);
    public static RequestScreenshotResult Gone() => new(RequestScreenshotOutcome.Gone, null);
    public static RequestScreenshotResult Failed() => new(RequestScreenshotOutcome.Failed, null);
}
