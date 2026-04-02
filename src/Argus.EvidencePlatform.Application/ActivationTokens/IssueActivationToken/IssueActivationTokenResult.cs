namespace Argus.EvidencePlatform.Application.ActivationTokens.IssueActivationToken;

public sealed record IssueActivationTokenResult(
    Guid Id,
    string CaseId,
    string Token,
    DateTimeOffset ValidUntil);
