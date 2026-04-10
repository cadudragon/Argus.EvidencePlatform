namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public sealed record DeviceCommandDispatchRequest(
    Guid FirebaseAppId,
    Guid CaseId,
    string DeviceId,
    string FcmToken,
    string Command,
    IReadOnlyDictionary<string, string> Parameters,
    DeviceCommandKey DeviceCommandKey,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    string Nonce);
