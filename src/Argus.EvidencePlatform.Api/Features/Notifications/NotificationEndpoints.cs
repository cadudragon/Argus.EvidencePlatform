using Argus.EvidencePlatform.Api.Validation;
using Argus.EvidencePlatform.Application.Notifications.IngestNotification;
using Argus.EvidencePlatform.Contracts.Notifications;
using Wolverine;

namespace Argus.EvidencePlatform.Api.Features.Notifications;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapPost("/notifications", IngestNotificationAsync)
            .AddEndpointFilter<ValidationEndpointFilter<IngestNotificationRequest>>();

        return builder;
    }

    private static async Task<IResult> IngestNotificationAsync(
        IngestNotificationRequest request,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var result = await bus.InvokeAsync<IngestNotificationOutcome>(
            new IngestNotificationCommand(
                request.DeviceId,
                request.CaseId,
                request.Sha256,
                DateTimeOffset.FromUnixTimeMilliseconds(request.CaptureTimestamp),
                request.PackageName,
                request.Title,
                request.Text,
                request.BigText,
                DateTimeOffset.FromUnixTimeMilliseconds(request.Timestamp),
                request.Category),
            cancellationToken);

        return result switch
        {
            IngestNotificationOutcome.Success => Results.Ok(),
            IngestNotificationOutcome.NotFound => Results.NotFound(),
            IngestNotificationOutcome.Gone => Results.StatusCode(StatusCodes.Status410Gone),
            IngestNotificationOutcome.Conflict => Results.Conflict(),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
