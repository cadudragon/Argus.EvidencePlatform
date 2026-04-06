namespace Argus.EvidencePlatform.Infrastructure.Firebase;

public sealed class FirebaseOptions
{
    public const string SectionName = "Firebase";

    public bool Enabled { get; set; }
    public List<FirebaseAppOptions> Apps { get; set; } = [];
}

public sealed class FirebaseAppOptions
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ProjectId { get; set; } = string.Empty;
    public string ServiceAccountPath { get; set; } = string.Empty;
    public bool IsActiveForNewCases { get; set; }
}

