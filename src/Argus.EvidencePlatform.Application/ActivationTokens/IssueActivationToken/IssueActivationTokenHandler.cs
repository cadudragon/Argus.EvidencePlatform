using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Domain.Enrollment;
using Wolverine.Attributes;

namespace Argus.EvidencePlatform.Application.ActivationTokens.IssueActivationToken;

public sealed class IssueActivationTokenHandler(
    ICaseRepository caseRepository,
    IActivationTokenRepository activationTokenRepository,
    IClock clock,
    IUnitOfWork unitOfWork)
{
    [Transactional]
    public async Task<IssueActivationTokenResult?> Handle(
        IssueActivationTokenCommand command,
        CancellationToken cancellationToken)
    {
        var normalizedCaseId = NormalizeRequired(command.CaseId, nameof(command.CaseId));
        var caseId = await caseRepository.GetIdByExternalCaseIdAsync(normalizedCaseId, cancellationToken);
        if (caseId is null)
        {
            return null;
        }

        var token = ActivationToken.Issue(
            Guid.NewGuid(),
            command.Token,
            caseId.Value,
            normalizedCaseId,
            clock.UtcNow,
            command.ValidUntil);

        await activationTokenRepository.AddAsync(token, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new IssueActivationTokenResult(
            token.Id,
            token.CaseExternalId,
            token.Token,
            token.ValidUntil);
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
