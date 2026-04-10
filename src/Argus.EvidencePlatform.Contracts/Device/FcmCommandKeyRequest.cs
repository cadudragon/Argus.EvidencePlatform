namespace Argus.EvidencePlatform.Contracts.Device;

public sealed record FcmCommandKeyRequest(
    string Alg,
    string Kid,
    string PublicKey);
