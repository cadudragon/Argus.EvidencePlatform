namespace Argus.EvidencePlatform.Domain.Exports;

public sealed class ExportJob
{
    public Guid Id { get; private set; }
    public Guid CaseId { get; private set; }
    public ExportJobStatus Status { get; private set; }
    public string RequestedBy { get; private set; } = string.Empty;
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? ManifestBlobName { get; private set; }
    public string? PackageBlobName { get; private set; }

    private ExportJob()
    {
    }

    public static ExportJob Queue(
        Guid id,
        Guid caseId,
        string requestedBy,
        DateTimeOffset requestedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(id));
        }

        if (caseId == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be empty.", nameof(caseId));
        }

        if (requestedAt == default)
        {
            throw new ArgumentException("Value cannot be default.", nameof(requestedAt));
        }

        return new ExportJob
        {
            Id = id,
            CaseId = caseId,
            RequestedBy = NormalizeRequired(requestedBy, nameof(requestedBy)),
            RequestedAt = requestedAt,
            Status = ExportJobStatus.Queued
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
}
