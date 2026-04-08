using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Application.Evidence.GetArtifactContent;
using Argus.EvidencePlatform.Application.Evidence.GetTimeline;
using Argus.EvidencePlatform.Application.Evidence.IngestArtifact;
using Argus.EvidencePlatform.Application.Evidence.ListArtifacts;
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

        group.MapGet("/cases/{caseId:guid}/artifacts", ListArtifactsAsync);
        group.MapGet("/cases/{caseId:guid}/timeline", GetTimelineAsync);
        group.MapGet("/artifacts/{artifactId:guid}/content", GetArtifactContentAsync);

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

    private static async Task<IResult> ListArtifactsAsync(
        Guid caseId,
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await bus.InvokeAsync<ListCaseArtifactsResponse>(
                new ListCaseArtifactsQuery(caseId, cursor, pageSize),
                cancellationToken);

            return Results.Ok(response);
        }
        catch (ArgumentOutOfRangeException ex) when (string.Equals(ex.ParamName, "pageSize", StringComparison.Ordinal))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["pageSize"] = [ex.Message]
            });
        }
        catch (ArgumentException ex) when (string.Equals(ex.ParamName, "cursor", StringComparison.Ordinal))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["cursor"] = ["cursor is invalid."]
            });
        }
    }

    private static async Task<IResult> GetArtifactContentAsync(
        Guid artifactId,
        IMessageBus bus,
        CancellationToken cancellationToken)
    {
        var result = await bus.InvokeAsync<EvidenceContentResult>(
            new GetArtifactContentQuery(artifactId),
            cancellationToken);

        return result.Outcome switch
        {
            EvidenceContentOutcome.Success => Results.File(
                result.Content!.Content,
                result.Content.ContentType,
                fileDownloadName: result.Content.FileName,
                lastModified: result.Content.LastModified,
                entityTag: null,
                enableRangeProcessing: result.Content.SupportsRangeProcessing),
            EvidenceContentOutcome.NotFound => Results.NotFound(),
            EvidenceContentOutcome.Conflict => Results.Conflict(),
            _ => Results.StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
