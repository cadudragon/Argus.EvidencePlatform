namespace Argus.EvidencePlatform.Application.Enrollment.ActivateDevice;

public sealed record ActivateDeviceCommand(
    string Token,
    string DeviceId);
