using Microsoft.AspNetCore.Mvc;

namespace Argus.EvidencePlatform.Api.Features.Evidence;

public sealed class IngestArtifactForm
{
    [FromForm(Name = "caseId")]
    public Guid CaseId { get; init; }

    [FromForm(Name = "sourceId")]
    public string SourceId { get; init; } = string.Empty;

    [FromForm(Name = "evidenceType")]
    public string EvidenceType { get; init; } = "Binary";

    [FromForm(Name = "captureTimestamp")]
    public DateTimeOffset? CaptureTimestamp { get; init; }

    [FromForm(Name = "classification")]
    public string? Classification { get; init; }

    [FromForm(Name = "file")]
    public IFormFile? File { get; init; }
}
