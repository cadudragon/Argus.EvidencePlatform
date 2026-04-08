using Argus.EvidencePlatform.Application.Common.Abstractions;

namespace Argus.EvidencePlatform.Application.Evidence.GetArtifactContent;

public sealed class GetArtifactContentHandler(
    IEvidenceRepository evidenceRepository,
    IEvidenceBlobReader evidenceBlobReader)
{
    public async Task<EvidenceContentResult> Handle(
        GetArtifactContentQuery query,
        CancellationToken cancellationToken)
    {
        var descriptor = await evidenceRepository.GetArtifactDescriptorAsync(query.ArtifactId, cancellationToken);
        if (descriptor is null)
        {
            return EvidenceContentResult.NotFound();
        }

        var content = await evidenceBlobReader.OpenReadAsync(
            descriptor.ContainerName,
            descriptor.BlobName,
            descriptor.BlobVersionId,
            cancellationToken);

        return content is null
            ? EvidenceContentResult.Conflict()
            : EvidenceContentResult.Success(content);
    }
}
