using System.Text.Json;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Screenshots;
using Argus.EvidencePlatform.Domain.Audit;
using Argus.EvidencePlatform.Domain.Evidence;
using Wolverine.Attributes;

namespace Argus.EvidencePlatform.Application.Screenshots.IngestScreenshot;

public sealed class IngestScreenshotHandler(
    ICaseRepository caseRepository,
    IDeviceSourceRepository deviceSourceRepository,
    IEvidenceRepository evidenceRepository,
    IAuditRepository auditRepository,
    IClock clock,
    IUnitOfWork unitOfWork)
{
    [Transactional]
    public async Task<IngestScreenshotResult> Handle(
        IngestScreenshotCommand command,
        CancellationToken cancellationToken)
    {
        var normalizedCaseId = NormalizeRequired(command.CaseId, nameof(command.CaseId));
        var normalizedDeviceId = NormalizeRequired(command.DeviceId, nameof(command.DeviceId));

        var caseId = await caseRepository.GetIdByExternalCaseIdAsync(normalizedCaseId, cancellationToken);
        if (caseId is null)
        {
            return IngestScreenshotResult.NotFound();
        }

        var deviceSource = await deviceSourceRepository.GetByDeviceIdAsync(normalizedDeviceId, cancellationToken);
        if (deviceSource is null)
        {
            return IngestScreenshotResult.NotFound();
        }

        var now = clock.UtcNow;
        if (!deviceSource.IsActive(now))
        {
            return IngestScreenshotResult.Gone();
        }

        if (deviceSource.CaseId != caseId.Value || !string.Equals(deviceSource.CaseExternalId, normalizedCaseId, StringComparison.Ordinal))
        {
            return IngestScreenshotResult.Conflict();
        }

        var evidenceId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var receivedAt = now;
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
            caseId.Value,
            normalizedDeviceId,
            EvidenceType.Image,
            command.CaptureTimestamp,
            receivedAt,
            "screenshot",
            blob);

        await evidenceRepository.AddAsync(evidence, cancellationToken);
        await auditRepository.AddAsync(
            AuditEntry.Create(
                Guid.NewGuid(),
                caseId.Value,
                AuditActorType.Device,
                normalizedDeviceId,
                "ScreenshotPreserved",
                nameof(EvidenceItem),
                evidenceId,
                receivedAt,
                receiptId.ToString("N"),
                JsonSerializer.Serialize(new
                {
                    CaseId = normalizedCaseId,
                    DeviceId = normalizedDeviceId,
                    BlobName = blob.BlobName,
                    blob.Sha256
                })),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return IngestScreenshotResult.Success(new IngestScreenshotResponse(
            receiptId,
            evidenceId,
            normalizedCaseId,
            normalizedDeviceId,
            blob.Sha256,
            blob.SizeBytes,
            receivedAt,
            evidence.Status.ToString()));
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", paramName);
        }

        return value.Trim();
    }
}
