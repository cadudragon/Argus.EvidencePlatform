namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public interface IFirebaseAppRoutingResolver
{
    Task<FirebaseAppRoutingContext?> ResolveForCaseAsync(Guid caseId, CancellationToken cancellationToken);
}
