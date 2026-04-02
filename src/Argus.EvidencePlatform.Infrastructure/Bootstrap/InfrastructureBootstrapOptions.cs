namespace Argus.EvidencePlatform.Infrastructure.Bootstrap;

public sealed class InfrastructureBootstrapOptions
{
    public const string SectionName = "Infrastructure";

    public bool BootstrapOnStartup { get; set; }

    public int RetryCount { get; set; } = 15;

    public int RetryDelaySeconds { get; set; } = 2;
}
