namespace Argus.EvidencePlatform.Infrastructure.Firebase;

public sealed class FcmCommandEncryptionOptions
{
    public const string SectionName = "Firebase:CommandEncryption";

    public bool Enabled { get; set; } = true;
    public string BackendKeyId { get; set; } = string.Empty;
    public string BackendPrivateKey { get; set; } = string.Empty;
    public bool AllowPlaintextDebugFallback { get; set; }
}
