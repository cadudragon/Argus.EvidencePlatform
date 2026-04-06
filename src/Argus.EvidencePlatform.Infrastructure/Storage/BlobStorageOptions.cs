namespace Argus.EvidencePlatform.Infrastructure.Storage;

public sealed class BlobStorageOptions
{
    public const string SectionName = "Storage";

    public string ConnectionName { get; set; } = "blobs";
    public string? ConnectionString { get; set; }
    public string? ServiceUri { get; set; }
    public string StagingContainerName { get; set; } = "staging";
    public string ExportsContainerName { get; set; } = "exports";
}
