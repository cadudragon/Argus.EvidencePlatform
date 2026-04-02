using Argus.EvidencePlatform.Contracts.Enrollment;

namespace Argus.EvidencePlatform.Application.Enrollment.ActivateDevice;

public sealed record ActivateDeviceResult(
    ActivateDeviceOutcome Outcome,
    ActivationSuccessResponse? Response)
{
    public static ActivateDeviceResult Success(ActivationSuccessResponse response) =>
        new(ActivateDeviceOutcome.Success, response);

    public static ActivateDeviceResult NotFound() => new(ActivateDeviceOutcome.NotFound, null);

    public static ActivateDeviceResult Gone() => new(ActivateDeviceOutcome.Gone, null);
}
