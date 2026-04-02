using Argus.EvidencePlatform.Api.Validation;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Application.Screenshots.IngestScreenshot;
using Wolverine;

namespace Argus.EvidencePlatform.Api.Features.Screenshots;

public static class ScreenshotEndpoints
{
    public static IEndpointRouteBuilder MapScreenshotEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api")
            .WithTags("Screenshots")
            .RequireAuthorization();

        group.MapPost("/screenshots", IngestScreenshotAsync)
            .AddEndpointFilter<ValidationEndpointFilter<IngestScreenshotForm>>()
            .DisableAntiforgery();

        return builder;
    }

    private static async Task<IResult> IngestScreenshotAsync(
        [AsParameters] IngestScreenshotForm form,
        IBlobStagingService blobStagingService,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var file = form.Image!;
        await using var stream = file.OpenReadStream();
        var stagedBlob = await blobStagingService.StageAsync(
            stream,
            file.FileName,
            file.ContentType ?? "application/octet-stream",
            cancellationToken);

        if (!string.Equals(stagedBlob.Sha256, form.Sha256.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["sha256"] = ["sha256 does not match the uploaded screenshot."]
            });
        }

        var result = await bus.InvokeAsync<IngestScreenshotResult>(
            new IngestScreenshotCommand(
                form.CaseId,
                form.DeviceId,
                ParseCaptureTimestamp(form.CaptureTimestamp),
                stagedBlob),
            cancellationToken);

        return result.Outcome switch
        {
            IngestScreenshotOutcome.Success => Results.Accepted($"/api/screenshots/{result.Response!.EvidenceId}", result.Response),
            IngestScreenshotOutcome.NotFound => Results.NotFound(),
            IngestScreenshotOutcome.Gone => Results.StatusCode(StatusCodes.Status410Gone),
            IngestScreenshotOutcome.Conflict => Results.Conflict(),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private static DateTimeOffset ParseCaptureTimestamp(string value)
    {
        if (long.TryParse(value, out var unixMilliseconds))
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds);
        }

        return DateTimeOffset.Parse(value);
    }
}
