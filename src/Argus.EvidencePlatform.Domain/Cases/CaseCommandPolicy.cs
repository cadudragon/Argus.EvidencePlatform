namespace Argus.EvidencePlatform.Domain.Cases;

public sealed class CaseCommandPolicy
{
    public Guid Id { get; private set; }
    public Guid CaseId { get; private set; }
    public int StreamStartFps { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private CaseCommandPolicy()
    {
    }

    public static CaseCommandPolicy CreateDefault(Guid id, Guid caseId, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(id));
        }

        if (caseId == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(caseId));
        }

        if (createdAt == default)
        {
            throw new ArgumentException("Value cannot be default.", nameof(createdAt));
        }

        return new CaseCommandPolicy
        {
            Id = id,
            CaseId = caseId,
            StreamStartFps = CaseCommandPolicyDefaults.StreamStartFps,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }
}
