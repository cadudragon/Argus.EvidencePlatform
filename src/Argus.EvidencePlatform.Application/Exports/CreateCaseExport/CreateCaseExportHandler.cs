using System.Text.Json;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Exports;
using Argus.EvidencePlatform.Domain.Audit;
using Argus.EvidencePlatform.Domain.Exports;
using Wolverine.Attributes;

namespace Argus.EvidencePlatform.Application.Exports.CreateCaseExport;

public sealed class CreateCaseExportHandler(
    ICaseRepository caseRepository,
    IExportJobRepository exportJobRepository,
    IAuditRepository auditRepository,
    IClock clock,
    IUnitOfWork unitOfWork)
{
    [Transactional]
    public async Task<ExportJobResponse?> Handle(
        CreateCaseExportCommand command,
        CancellationToken cancellationToken)
    {
        if (!await caseRepository.ExistsAsync(command.CaseId, cancellationToken))
        {
            return null;
        }

        var now = clock.UtcNow;
        var job = ExportJob.Queue(Guid.NewGuid(), command.CaseId, command.RequestedBy, now);
        var normalizedFormat = NormalizeOptional(command.Format)?.ToLowerInvariant();
        var normalizedReason = NormalizeOptional(command.Reason);

        await exportJobRepository.AddAsync(job, cancellationToken);
        await auditRepository.AddAsync(
            AuditEntry.Create(
                Guid.NewGuid(),
                command.CaseId,
                AuditActorType.Operator,
                command.RequestedBy,
                "ExportQueued",
                nameof(ExportJob),
                job.Id,
                now,
                Guid.NewGuid().ToString("N"),
                JsonSerializer.Serialize(new { Format = normalizedFormat, Reason = normalizedReason })),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ExportJobResponse(
            job.Id,
            job.CaseId,
            job.Status.ToString(),
            job.RequestedBy,
            job.RequestedAt,
            job.CompletedAt);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
