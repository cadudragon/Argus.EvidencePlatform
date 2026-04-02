namespace Argus.EvidencePlatform.Domain.Audit;

public sealed class AuditEntry
{
    public Guid Id { get; private set; }
    public Guid CaseId { get; private set; }
    public AuditActorType ActorType { get; private set; }
    public string ActorId { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public Guid EntityId { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public string? PayloadJson { get; private set; }

    private AuditEntry()
    {
    }

    public static AuditEntry Create(
        Guid id,
        Guid caseId,
        AuditActorType actorType,
        string actorId,
        string action,
        string entityType,
        Guid entityId,
        DateTimeOffset occurredAt,
        string correlationId,
        string? payloadJson)
    {
        return new AuditEntry
        {
            Id = id,
            CaseId = caseId,
            ActorType = actorType,
            ActorId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OccurredAt = occurredAt,
            CorrelationId = correlationId,
            PayloadJson = payloadJson
        };
    }
}
