namespace Argus.EvidencePlatform.Infrastructure.Firebase;

public sealed class FirebaseOptions
{
    public const string SectionName = "Firebase";

    public bool Enabled { get; set; }

    public string? ProjectId { get; set; }

    public string? ServiceAccountPath { get; set; }
}

