using System.Text.Json;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Cases;
using Argus.EvidencePlatform.Domain.Audit;
using Argus.EvidencePlatform.Domain.Cases;
using Wolverine.Attributes;

namespace Argus.EvidencePlatform.Application.Cases.CreateCase;

public sealed class CreateCaseHandler(
    ICaseRepository caseRepository,
    IAuditRepository auditRepository,
    IClock clock,
    IUnitOfWork unitOfWork)
{
    [Transactional]
    public async Task<CreateCaseResult> Handle(CreateCaseCommand command, CancellationToken cancellationToken)
    {
        var externalCaseId = command.ExternalCaseId ?? string.Empty;
        var normalizedExternalCaseId = externalCaseId.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedExternalCaseId)
            && await caseRepository.ExistsByExternalCaseIdAsync(normalizedExternalCaseId, cancellationToken))
        {
            return CreateCaseResult.Conflict();
        }

        var now = clock.UtcNow;
        var entity = Case.Create(
            Guid.NewGuid(),
            externalCaseId,
            command.Title,
            command.Description,
            now);

        await caseRepository.AddAsync(entity, cancellationToken);
        await auditRepository.AddAsync(
            AuditEntry.Create(
                Guid.NewGuid(),
                entity.Id,
                AuditActorType.System,
                "system",
                "CaseCreated",
                nameof(Case),
                entity.Id,
                now,
                Guid.NewGuid().ToString("N"),
                JsonSerializer.Serialize(new { entity.ExternalCaseId, entity.Title })),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateCaseResult.Created(new CaseResponse(
            entity.Id,
            entity.ExternalCaseId,
            entity.Title,
            entity.Description,
            entity.Status.ToString(),
            entity.CreatedAt,
            entity.ClosedAt));
    }
}
