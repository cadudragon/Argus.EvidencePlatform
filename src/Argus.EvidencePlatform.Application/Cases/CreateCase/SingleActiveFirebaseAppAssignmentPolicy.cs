using Argus.EvidencePlatform.Application.Common.Abstractions;

namespace Argus.EvidencePlatform.Application.Cases.CreateCase;

public sealed class SingleActiveFirebaseAppAssignmentPolicy(
    IFirebaseAppRepository firebaseAppRepository) : IFirebaseAppAssignmentPolicy
{
    public async Task<FirebaseAppAssignmentResult> AssignForNewCaseAsync(CancellationToken cancellationToken)
    {
        var activeApps = await firebaseAppRepository.ListActiveForNewCasesAsync(cancellationToken);
        return activeApps.Count switch
        {
            1 => FirebaseAppAssignmentResult.Assigned(activeApps[0].Id),
            0 => FirebaseAppAssignmentResult.NoneEligible(),
            _ => FirebaseAppAssignmentResult.Ambiguous()
        };
    }
}
