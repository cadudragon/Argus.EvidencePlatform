namespace Argus.EvidencePlatform.Application.Common.Abstractions;

public interface IBlobStagingService
{
    Task<StagedBlobDescriptor> StageAsync(
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken);
}
