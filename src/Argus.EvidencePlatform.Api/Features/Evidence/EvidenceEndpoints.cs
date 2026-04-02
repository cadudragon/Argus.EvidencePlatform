using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Application.Evidence.GetTimeline;
using Argus.EvidencePlatform.Application.Evidence.IngestArtifact;
using Argus.EvidencePlatform.Api.Validation;
using Argus.EvidencePlatform.Contracts.Evidence;
using Argus.EvidencePlatform.Domain.Evidence;
using Microsoft.AspNetCore.Mvc;
using Wolverine;

namespace Argus.EvidencePlatform.Api.Features.Evidence;

public static class EvidenceEndpoints
{
    public static IEndpointRouteBuilder MapEvidenceEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/evidence")
            .WithTags("Evidence")
            .RequireAuthorization();

        group.MapPost("/artifacts", IngestArtifactAsync)
            .AddEndpointFilter<ValidationEndpointFilter<IngestArtifactForm>>()
            .DisableAntiforgery();

        group.MapGet("/cases/{caseId:guid}/timeline", GetTimelineAsync);

        return builder;
    }

    private static async Task<IResult> IngestArtifactAsync(
        [AsParameters] IngestArtifactForm form,
        IBlobStagingService blobStagingService,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<EvidenceType>(form.EvidenceType, ignoreCase: true, out var evidenceType))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["evidenceType"] = ["Evidence type must be one of: Image, Document, Text, Binary."]
            });
        }

        var file = form.File!;
        await using var stream = file.OpenReadStream();
        var stagedBlob = await blobStagingService.StageAsync(
            stream,
            file.FileName,
            file.ContentType,
            cancellationToken);

        var response = await bus.InvokeAsync<IngestArtifactResponse?>(
            new FinalizeEvidenceIntakeCommand(
                form.CaseId,
                form.SourceId,
                evidenceType,
                form.CaptureTimestamp ?? DateTimeOffset.UtcNow,
                form.Classification,
                stagedBlob),
            cancellationToken);

        return response is null
            ? Results.NotFound()
            : Results.Accepted($"/api/evidence/{response.EvidenceId}", response);
    }

    private static async Task<IResult> GetTimelineAsync(
        Guid caseId,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var response = await bus.InvokeAsync<IReadOnlyList<EvidenceTimelineItemResponse>>(
            new GetEvidenceTimelineQuery(caseId),
            cancellationToken);

        return Results.Ok(response);
    }
}
