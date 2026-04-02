using System.Text.Json;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Evidence;
using Argus.EvidencePlatform.Domain.Audit;
using Argus.EvidencePlatform.Domain.Evidence;
using Wolverine.Attributes;

namespace Argus.EvidencePlatform.Application.Evidence.IngestArtifact;

public sealed class FinalizeEvidenceIntakeHandler(
    ICaseRepository caseRepository,
    IEvidenceRepository evidenceRepository,
    IAuditRepository auditRepository,
    IClock clock,
    IUnitOfWork unitOfWork)
{
    [Transactional]
    public async Task<IngestArtifactResponse?> Handle(
        FinalizeEvidenceIntakeCommand command,
        CancellationToken cancellationToken)
    {
        if (!await caseRepository.ExistsAsync(command.CaseId, cancellationToken))
        {
            return null;
        }

        var evidenceId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var receivedAt = clock.UtcNow;

        var blob = EvidenceBlob.Create(
            Guid.NewGuid(),
            evidenceId,
            command.StagedBlob.ContainerName,
            command.StagedBlob.BlobName,
            command.StagedBlob.BlobVersionId,
            command.StagedBlob.ContentType,
            command.StagedBlob.SizeBytes,
            command.StagedBlob.Sha256,
            receivedAt);

        var evidence = EvidenceItem.Preserve(
            evidenceId,
            command.CaseId,
            command.SourceId,
            command.EvidenceType,
            command.CaptureTimestamp,
            receivedAt,
            command.Classification,
            blob);

        await evidenceRepository.AddAsync(evidence, cancellationToken);
        await auditRepository.AddAsync(
            AuditEntry.Create(
                Guid.NewGuid(),
                command.CaseId,
                AuditActorType.System,
                "system",
                "EvidencePreserved",
                nameof(EvidenceItem),
                evidenceId,
                receivedAt,
                receiptId.ToString("N"),
                JsonSerializer.Serialize(new
                {
                    evidence.SourceId,
                    EvidenceType = command.EvidenceType.ToString(),
                    BlobName = blob.BlobName,
                    Sha256 = blob.Sha256
                })),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new IngestArtifactResponse(
            receiptId,
            evidenceId,
            command.CaseId,
            evidence.SourceId,
            command.EvidenceType.ToString(),
            blob.BlobName,
            blob.Sha256,
            blob.SizeBytes,
            receivedAt,
            evidence.Status.ToString());
    }
}
