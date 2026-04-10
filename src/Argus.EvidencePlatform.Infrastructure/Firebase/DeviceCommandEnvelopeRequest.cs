using Argus.EvidencePlatform.Application.Common.Abstractions;

namespace Argus.EvidencePlatform.Infrastructure.Firebase;

public sealed record DeviceCommandEnvelopeRequest(
    string Command,
    IReadOnlyDictionary<string, string> Parameters,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    string Nonce,
    DeviceCommandKey DeviceKey);
