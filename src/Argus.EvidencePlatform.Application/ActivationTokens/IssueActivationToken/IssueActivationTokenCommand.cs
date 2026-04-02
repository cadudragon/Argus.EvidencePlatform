namespace Argus.EvidencePlatform.Application.ActivationTokens.IssueActivationToken;

public sealed record IssueActivationTokenCommand(
    string CaseId,
    string Token,
    DateTimeOffset ValidUntil);
