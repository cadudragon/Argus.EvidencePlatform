using Argus.EvidencePlatform.Api.Validation;
using Argus.EvidencePlatform.Application.Enrollment.ActivateDevice;
using Argus.EvidencePlatform.Contracts.Enrollment;
using Wolverine;

namespace Argus.EvidencePlatform.Api.Features.Enrollment;

public static class EnrollmentEndpoints
{
    public static IEndpointRouteBuilder MapEnrollmentEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/api")
            .WithTags("Enrollment")
            .RequireAuthorization()
            .MapPost("/activate", ActivateAsync)
            .AddEndpointFilter<ValidationEndpointFilter<ActivationRequest>>();

        return builder;
    }

    private static async Task<IResult> ActivateAsync(
        ActivationRequest request,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var result = await bus.InvokeAsync<ActivateDeviceResult>(
            new ActivateDeviceCommand(request.Token, request.DeviceId),
            cancellationToken);

        return result.Outcome switch
        {
            ActivateDeviceOutcome.Success => Results.Ok(result.Response),
            ActivateDeviceOutcome.NotFound => Results.NotFound(),
            ActivateDeviceOutcome.Gone => Results.StatusCode(StatusCodes.Status410Gone),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
