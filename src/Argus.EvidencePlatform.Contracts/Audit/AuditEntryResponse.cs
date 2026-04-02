namespace Argus.EvidencePlatform.Contracts.Audit;

public sealed record AuditEntryResponse(
    Guid Id,
    Guid CaseId,
    string ActorType,
    string ActorId,
    string Action,
    string EntityType,
    Guid EntityId,
    DateTimeOffset OccurredAt,
    string CorrelationId,
    string? PayloadJson);
