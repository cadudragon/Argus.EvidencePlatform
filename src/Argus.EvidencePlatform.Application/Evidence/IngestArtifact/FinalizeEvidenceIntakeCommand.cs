using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Evidence;
using Argus.EvidencePlatform.Domain.Evidence;

namespace Argus.EvidencePlatform.Application.Evidence.IngestArtifact;

public sealed record FinalizeEvidenceIntakeCommand(
    Guid CaseId,
    string SourceId,
    EvidenceType EvidenceType,
    DateTimeOffset CaptureTimestamp,
    string? Classification,
    StagedBlobDescriptor StagedBlob);
