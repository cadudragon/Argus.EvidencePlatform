using Argus.EvidencePlatform.Application.Audit.GetCaseAuditTrail;
using Argus.EvidencePlatform.Contracts.Audit;
using Wolverine;

namespace Argus.EvidencePlatform.Api.Features.Audit;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGroup("/api/audit")
            .WithTags("Audit")
            .RequireAuthorization()
            .MapGet("/cases/{caseId:guid}", GetCaseAuditTrailAsync);

        return builder;
    }

    private static async Task<IResult> GetCaseAuditTrailAsync(
        Guid caseId,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var response = await bus.InvokeAsync<IReadOnlyList<AuditEntryResponse>>(
            new GetCaseAuditTrailQuery(caseId),
            cancellationToken);

        return Results.Ok(response);
    }
}
