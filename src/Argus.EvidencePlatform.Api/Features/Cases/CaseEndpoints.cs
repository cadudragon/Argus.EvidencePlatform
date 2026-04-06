using Argus.EvidencePlatform.Api.Validation;
using Argus.EvidencePlatform.Application.Cases.CreateCase;
using Argus.EvidencePlatform.Application.Cases.GetCase;
using Argus.EvidencePlatform.Contracts.Cases;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace Argus.EvidencePlatform.Api.Features.Cases;

public static class CaseEndpoints
{
    public static IEndpointRouteBuilder MapCaseEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/cases")
            .WithTags("Cases")
            .RequireAuthorization();

        group.MapPost("/", CreateCaseAsync)
            .AddEndpointFilter<ValidationEndpointFilter<CreateCaseRequest>>();

        group.MapGet("/{id:guid}", GetCaseAsync);

        return builder;
    }

    private static async Task<IResult> CreateCaseAsync(
        CreateCaseRequest request,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var result = await bus.InvokeAsync<CreateCaseResult>(
            new CreateCaseCommand(request.ExternalCaseId, request.Title, request.Description),
            cancellationToken);

        if (result.AlreadyExists)
        {
            return Results.Conflict(new ProblemDetails
            {
                Title = "Case already exists.",
                Detail = $"A case with externalCaseId '{request.ExternalCaseId.Trim()}' already exists.",
                Status = StatusCodes.Status409Conflict
            });
        }

        if (result.Outcome == CreateCaseOutcome.FirebaseUnavailable)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "No eligible Firebase app is available for new cases.",
                detail: "The backend configuration does not currently allow assigning a Firebase app to a new case.");
        }

        return Results.Created($"/api/cases/{result.Case!.Id}", result.Case);
    }

    private static async Task<IResult> GetCaseAsync(
        Guid id,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var response = await bus.InvokeAsync<CaseResponse?>(
            new GetCaseByIdQuery(id),
            cancellationToken);

        return response is null ? Results.NotFound() : Results.Ok(response);
    }
}
