namespace Argus.EvidencePlatform.Infrastructure.Firebase;

public static class FcmCommandEnvelopeContract
{
    public const string Enc = "1";
    public const string Alg = "ECDH-P256-HKDF-SHA256+A256GCM";
    public const string DeviceKeyAlg = "ECDH-P256";
    public const int IvSizeBytes = 12;
    public const int TagSizeBytes = 16;
    public const int KeySizeBytes = 32;
    public const int MaxTtlMinutes = 10;
    public const int MaxIssuedAtFutureSkewSeconds = 60;
}
