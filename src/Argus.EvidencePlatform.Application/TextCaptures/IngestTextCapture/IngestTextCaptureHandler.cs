using System.Text.Json;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Domain.Audit;
using Argus.EvidencePlatform.Domain.TextCaptures;
using Wolverine.Attributes;

namespace Argus.EvidencePlatform.Application.TextCaptures.IngestTextCapture;

public sealed class IngestTextCaptureHandler(
    ICaseRepository caseRepository,
    IDeviceSourceRepository deviceSourceRepository,
    ITextCaptureBatchRepository textCaptureBatchRepository,
    IAuditRepository auditRepository,
    IClock clock,
    IUnitOfWork unitOfWork)
{
    [Transactional]
    public async Task<IngestTextCaptureOutcome> Handle(
        IngestTextCaptureCommand command,
        CancellationToken cancellationToken)
    {
        var normalizedCaseId = NormalizeRequired(command.CaseId, nameof(command.CaseId));
        var normalizedDeviceId = NormalizeRequired(command.DeviceId, nameof(command.DeviceId));

        var caseId = await caseRepository.GetIdByExternalCaseIdAsync(normalizedCaseId, cancellationToken);
        if (caseId is null)
        {
            return IngestTextCaptureOutcome.NotFound;
        }

        var deviceSource = await deviceSourceRepository.GetByDeviceIdAsync(normalizedDeviceId, cancellationToken);
        if (deviceSource is null)
        {
            return IngestTextCaptureOutcome.NotFound;
        }

        var now = clock.UtcNow;
        if (!deviceSource.IsActive(now))
        {
            return IngestTextCaptureOutcome.Gone;
        }

        if (deviceSource.CaseId != caseId.Value
            || !string.Equals(deviceSource.CaseExternalId, normalizedCaseId, StringComparison.Ordinal))
        {
            return IngestTextCaptureOutcome.Conflict;
        }

        var normalizedCaptures = command.Captures
            .Select(capture => new
            {
                PackageName = NormalizeRequired(capture.PackageName, nameof(capture.PackageName)),
                ClassName = NormalizeRequired(capture.ClassName, nameof(capture.ClassName)),
                Text = NormalizeOptional(capture.Text),
                ContentDescription = NormalizeOptional(capture.ContentDescription)
            })
            .ToArray();

        var payloadJson = JsonSerializer.Serialize(normalizedCaptures);
        var packageNamesJson = JsonSerializer.Serialize(
            normalizedCaptures
                .Select(capture => capture.PackageName)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(packageName => packageName, StringComparer.Ordinal)
                .ToArray());

        var textCaptureBatch = TextCaptureBatch.Capture(
            Guid.NewGuid(),
            caseId.Value,
            normalizedCaseId,
            normalizedDeviceId,
            command.Sha256,
            command.CaptureTimestamp,
            normalizedCaptures.Length,
            payloadJson,
            packageNamesJson,
            now);

        await textCaptureBatchRepository.AddAsync(textCaptureBatch, cancellationToken);
        await auditRepository.AddAsync(
            AuditEntry.Create(
                Guid.NewGuid(),
                caseId.Value,
                AuditActorType.Device,
                normalizedDeviceId,
                "TextCaptureBatchCaptured",
                nameof(TextCaptureBatch),
                textCaptureBatch.Id,
                now,
                Guid.NewGuid().ToString("N"),
                JsonSerializer.Serialize(new
                {
                    CaseId = normalizedCaseId,
                    DeviceId = normalizedDeviceId,
                    textCaptureBatch.CaptureCount,
                    PackageNames = normalizedCaptures
                        .Select(capture => capture.PackageName)
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(packageName => packageName, StringComparer.Ordinal)
                        .ToArray()
                })),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return IngestTextCaptureOutcome.Success;
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", paramName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
