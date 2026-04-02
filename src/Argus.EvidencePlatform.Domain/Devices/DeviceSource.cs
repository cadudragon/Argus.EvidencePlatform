namespace Argus.EvidencePlatform.Domain.Devices;

public sealed class DeviceSource
{
    public Guid Id { get; private set; }
    public string DeviceId { get; private set; } = string.Empty;
    public Guid CaseId { get; private set; }
    public string CaseExternalId { get; private set; } = string.Empty;
    public DateTimeOffset EnrolledAt { get; private set; }
    public DateTimeOffset ValidUntil { get; private set; }
    public DateTimeOffset? LastSeenAt { get; private set; }

    private DeviceSource()
    {
    }

    public bool IsActive(DateTimeOffset at) => at < ValidUntil;

    public static DeviceSource Register(
        Guid id,
        string deviceId,
        Guid caseId,
        string caseExternalId,
        DateTimeOffset enrolledAt,
        DateTimeOffset validUntil)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(id));
        }

        if (caseId == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(caseId));
        }

        if (enrolledAt == default)
        {
            throw new ArgumentException("Value cannot be default.", nameof(enrolledAt));
        }

        if (validUntil == default)
        {
            throw new ArgumentException("Value cannot be default.", nameof(validUntil));
        }

        if (validUntil <= enrolledAt)
        {
            throw new ArgumentOutOfRangeException(nameof(validUntil), "Value must be after enrolledAt.");
        }

        return new DeviceSource
        {
            Id = id,
            DeviceId = NormalizeRequired(deviceId, nameof(deviceId)),
            CaseId = caseId,
            CaseExternalId = NormalizeRequired(caseExternalId, nameof(caseExternalId)),
            EnrolledAt = enrolledAt,
            ValidUntil = validUntil
        };
    }

    public void RenewEnrollment(
        Guid caseId,
        string caseExternalId,
        DateTimeOffset enrolledAt,
        DateTimeOffset validUntil)
    {
        if (caseId == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(caseId));
        }

        if (enrolledAt == default)
        {
            throw new ArgumentException("Value cannot be default.", nameof(enrolledAt));
        }

        if (validUntil == default)
        {
            throw new ArgumentException("Value cannot be default.", nameof(validUntil));
        }

        if (validUntil <= enrolledAt)
        {
            throw new ArgumentOutOfRangeException(nameof(validUntil), "Value must be after enrolledAt.");
        }

        CaseId = caseId;
        CaseExternalId = NormalizeRequired(caseExternalId, nameof(caseExternalId));
        EnrolledAt = enrolledAt;
        ValidUntil = validUntil;
    }

    public void RecordPong(DateTimeOffset seenAt)
    {
        if (seenAt == default)
        {
            throw new ArgumentException("Value cannot be default.", nameof(seenAt));
        }

        LastSeenAt = seenAt;
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
