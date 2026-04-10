namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public sealed record DeviceCommandKey(
    string Alg,
    string Kid,
    string PublicKey);
