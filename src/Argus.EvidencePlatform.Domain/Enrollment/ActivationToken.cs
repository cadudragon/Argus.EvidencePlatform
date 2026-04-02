namespace Argus.EvidencePlatform.Domain.Enrollment;

public sealed class ActivationToken
{
    public Guid Id { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public Guid CaseId { get; private set; }
    public string CaseExternalId { get; private set; } = string.Empty;
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset ValidUntil { get; private set; }
    public DateTimeOffset? ConsumedAt { get; private set; }
    public string? ConsumedByDeviceId { get; private set; }

    private ActivationToken()
    {
    }

    public bool IsConsumed => ConsumedAt.HasValue;

    public bool IsExpired(DateTimeOffset at) => at >= ValidUntil;

    public static ActivationToken Issue(
        Guid id,
        string token,
        Guid caseId,
        string caseExternalId,
        DateTimeOffset issuedAt,
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

        if (issuedAt == default)
        {
            throw new ArgumentException("Value cannot be default.", nameof(issuedAt));
        }

        if (validUntil == default)
        {
            throw new ArgumentException("Value cannot be default.", nameof(validUntil));
        }

        if (validUntil <= issuedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(validUntil), "Value must be after issuedAt.");
        }

        return new ActivationToken
        {
            Id = id,
            Token = NormalizeToken(token),
            CaseId = caseId,
            CaseExternalId = NormalizeRequired(caseExternalId, nameof(caseExternalId)),
            IssuedAt = issuedAt,
            ValidUntil = validUntil
        };
    }

    public void Consume(string deviceId, DateTimeOffset consumedAt)
    {
        if (IsConsumed)
        {
            throw new InvalidOperationException("Activation token has already been consumed.");
        }

        if (consumedAt == default)
        {
            throw new ArgumentException("Value cannot be default.", nameof(consumedAt));
        }

        if (IsExpired(consumedAt))
        {
            throw new InvalidOperationException("Expired activation token cannot be consumed.");
        }

        ConsumedAt = consumedAt;
        ConsumedByDeviceId = NormalizeRequired(deviceId, nameof(deviceId));
    }

    private static string NormalizeToken(string value)
    {
        var normalized = NormalizeRequired(value, nameof(value));
        if (normalized.Length != 9 || normalized.Any(character => !char.IsDigit(character)))
        {
            throw new ArgumentException("Activation token must have exactly 9 digits.", nameof(value));
        }

        return normalized;
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
