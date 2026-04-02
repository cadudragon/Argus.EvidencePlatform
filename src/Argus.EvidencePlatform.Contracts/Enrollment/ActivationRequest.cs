namespace Argus.EvidencePlatform.Contracts.Enrollment;

public sealed record ActivationRequest(
    string Token,
    string DeviceId);
