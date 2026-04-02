using Argus.EvidencePlatform.Api.Validation;
using Argus.EvidencePlatform.Application.Device.BindFcmToken;
using Argus.EvidencePlatform.Application.Device.RecordPong;
using Argus.EvidencePlatform.Contracts.Device;
using Wolverine;

namespace Argus.EvidencePlatform.Api.Features.Device;

public static class DeviceEndpoints
{
    public static IEndpointRouteBuilder MapDeviceEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api")
            .WithTags("Device")
            .RequireAuthorization();

        group.MapPost("/pong", PongAsync)
            .AddEndpointFilter<ValidationEndpointFilter<PongRequest>>();

        group.MapPut("/fcm-token", UpdateFcmTokenAsync)
            .AddEndpointFilter<ValidationEndpointFilter<UpdateFcmTokenRequest>>();

        return builder;
    }

    private static async Task<IResult> PongAsync(
        PongRequest request,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var result = await bus.InvokeAsync<RecordPongOutcome>(
            new RecordPongCommand(request.DeviceId),
            cancellationToken);

        return result == RecordPongOutcome.Success
            ? Results.Ok()
            : Results.StatusCode(StatusCodes.Status410Gone);
    }

    private static async Task<IResult> UpdateFcmTokenAsync(
        UpdateFcmTokenRequest request,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var result = await bus.InvokeAsync<BindFcmTokenOutcome>(
            new BindFcmTokenCommand(request.DeviceId, request.FcmToken),
            cancellationToken);

        return result == BindFcmTokenOutcome.Success
            ? Results.Ok()
            : Results.StatusCode(StatusCodes.Status410Gone);
    }
}
