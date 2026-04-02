namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public sealed record DeviceCommandDispatchResult(
    DeviceCommandDispatchStatus Status,
    string? MessageId,
    string? FailureReason = null);

public enum DeviceCommandDispatchStatus
{
    Success = 0,
    TokenInvalid = 1,
    Failed = 2
}
