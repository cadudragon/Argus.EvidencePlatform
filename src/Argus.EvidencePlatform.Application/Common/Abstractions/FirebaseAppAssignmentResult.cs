namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public sealed record FirebaseAppAssignmentResult(
    FirebaseAppAssignmentOutcome Outcome,
    Guid? FirebaseAppId)
{
    public static FirebaseAppAssignmentResult Assigned(Guid firebaseAppId) =>
        new(FirebaseAppAssignmentOutcome.Assigned, firebaseAppId);

    public static FirebaseAppAssignmentResult NoneEligible() =>
        new(FirebaseAppAssignmentOutcome.NoneEligible, null);

    public static FirebaseAppAssignmentResult Ambiguous() =>
        new(FirebaseAppAssignmentOutcome.Ambiguous, null);
}
