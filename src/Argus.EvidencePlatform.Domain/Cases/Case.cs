namespace Argus.EvidencePlatform.Domain.Cases;

public sealed class Case
{
    public Guid Id { get; private set; }
    public Guid FirebaseAppId { get; private set; }
    public string ExternalCaseId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public CaseStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    private Case()
    {
    }

    public static Case Create(
        Guid id,
        Guid firebaseAppId,
        string externalCaseId,
        string title,
        string? description,
        DateTimeOffset createdAt)
    {
        if (firebaseAppId == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(firebaseAppId));
        }

        var normalizedExternalCaseId = NormalizeRequired(externalCaseId, nameof(externalCaseId));
        var normalizedTitle = NormalizeRequired(title, nameof(title));
        var normalizedDescription = NormalizeOptional(description);

        return new Case
        {
            Id = id,
            FirebaseAppId = firebaseAppId,
            ExternalCaseId = normalizedExternalCaseId,
            Title = normalizedTitle,
            Description = normalizedDescription,
            Status = CaseStatus.Active,
            CreatedAt = createdAt
        };
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
