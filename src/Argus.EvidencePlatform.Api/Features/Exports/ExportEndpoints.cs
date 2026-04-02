using Argus.EvidencePlatform.Api.Validation;
using Argus.EvidencePlatform.Application.Exports.CreateCaseExport;
using Argus.EvidencePlatform.Application.Exports.GetExportJob;
using Argus.EvidencePlatform.Contracts.Exports;
using Wolverine;

namespace Argus.EvidencePlatform.Api.Features.Exports;

public static class ExportEndpoints
{
    public static IEndpointRouteBuilder MapExportEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/exports")
            .WithTags("Exports")
            .RequireAuthorization();

        group.MapPost("/", CreateExportAsync)
            .AddEndpointFilter<ValidationEndpointFilter<CreateCaseExportRequest>>();

        group.MapGet("/{id:guid}", GetExportAsync);

        return builder;
    }

    private static async Task<IResult> CreateExportAsync(
        CreateCaseExportRequest request,
        HttpContext httpContext,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var requestedBy = httpContext.User.Identity?.Name ?? "system";
        var response = await bus.InvokeAsync<ExportJobResponse?>(
            new CreateCaseExportCommand(request.CaseId, requestedBy, request.Format, request.Reason),
            cancellationToken);

        return response is null
            ? Results.NotFound()
            : Results.Accepted($"/api/exports/{response.Id}", response);
    }

    private static async Task<IResult> GetExportAsync(
        Guid id,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var response = await bus.InvokeAsync<ExportJobResponse?>(
            new GetExportJobQuery(id),
            cancellationToken);

        return response is null ? Results.NotFound() : Results.Ok(response);
    }
}
