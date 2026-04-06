using Argus.EvidencePlatform.Api.Validation;
using Argus.EvidencePlatform.Application.TextCaptures.IngestTextCapture;
using Argus.EvidencePlatform.Contracts.TextCaptures;
using Wolverine;

namespace Argus.EvidencePlatform.Api.Features.TextCaptures;

public static class TextCaptureEndpoints
{
    public static IEndpointRouteBuilder MapTextCaptureEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api")
            .WithTags("TextCaptures")
            .RequireAuthorization();

        group.MapPost("/text-captures", IngestTextCaptureAsync)
            .AddEndpointFilter<ValidationEndpointFilter<IngestTextCaptureRequest>>();

        return builder;
    }

    private static async Task<IResult> IngestTextCaptureAsync(
        IngestTextCaptureRequest request,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var result = await bus.InvokeAsync<IngestTextCaptureOutcome>(
            new IngestTextCaptureCommand(
                request.DeviceId,
                request.CaseId,
                request.Sha256,
                DateTimeOffset.FromUnixTimeMilliseconds(request.CaptureTimestamp),
                request.Captures
                    .Select(capture => new TextCapturePayload(
                        capture.PackageName,
                        capture.ClassName,
                        capture.Text,
                        capture.ContentDescription))
                    .ToArray()),
            cancellationToken);

        return result switch
        {
            IngestTextCaptureOutcome.Success => Results.Ok(),
            IngestTextCaptureOutcome.NotFound => Results.NotFound(),
            IngestTextCaptureOutcome.Gone => Results.StatusCode(StatusCodes.Status410Gone),
            IngestTextCaptureOutcome.Conflict => Results.Conflict(),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
