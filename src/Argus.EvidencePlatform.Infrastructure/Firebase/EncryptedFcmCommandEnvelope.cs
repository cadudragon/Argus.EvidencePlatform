namespace Argus.EvidencePlatform.Infrastructure.Firebase;

public sealed record EncryptedFcmCommandEnvelope(
    string Enc,
    string Alg,
    string Kid,
    string Dkid,
    string Iv,
    string Ct);
