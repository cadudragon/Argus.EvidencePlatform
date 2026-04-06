namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public interface IFirebaseAppAssignmentPolicy
{
    Task<FirebaseAppAssignmentResult> AssignForNewCaseAsync(CancellationToken cancellationToken);
}
