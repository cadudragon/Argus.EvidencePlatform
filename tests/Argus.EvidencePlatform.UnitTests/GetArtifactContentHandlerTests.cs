using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Application.Evidence.GetArtifactContent;
using Argus.EvidencePlatform.Contracts.Evidence;
using Argus.EvidencePlatform.Domain.Evidence;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class GetArtifactContentHandlerTests
{
    [Fact]
    public async Task Handle_should_return_not_found_when_descriptor_does_not_exist()
    {
        var repository = new FakeEvidenceRepository();
        var blobReader = new FakeEvidenceBlobReader();
        var handler = new GetArtifactContentHandler(repository, blobReader);

        var result = await handler.Handle(new GetArtifactContentQuery(Guid.NewGuid()), CancellationToken.None);

        result.Outcome.Should().Be(EvidenceContentOutcome.NotFound);
    }

    [Fact]
    public async Task Handle_should_return_conflict_when_blob_is_missing()
    {
        var artifactId = Guid.NewGuid();
        var repository = new FakeEvidenceRepository
        {
            Descriptor = CreateDescriptor(artifactId)
        };
        var blobReader = new FakeEvidenceBlobReader();
        var handler = new GetArtifactContentHandler(repository, blobReader);

        var result = await handler.Handle(new GetArtifactContentQuery(artifactId), CancellationToken.None);

        result.Outcome.Should().Be(EvidenceContentOutcome.Conflict);
    }

    [Fact]
    public async Task Handle_should_return_stream_when_blob_exists()
    {
        var artifactId = Guid.NewGuid();
        var repository = new FakeEvidenceRepository
        {
            Descriptor = CreateDescriptor(artifactId)
        };
        var blobReader = new FakeEvidenceBlobReader
        {
            Response = new EvidenceContentStream(
                new MemoryStream([1, 2, 3]),
                "image/jpeg",
                3,
                new DateTimeOffset(2026, 4, 8, 10, 0, 0, TimeSpan.Zero),
                true,
                "capture.jpg")
        };
        var handler = new GetArtifactContentHandler(repository, blobReader);

        var result = await handler.Handle(new GetArtifactContentQuery(artifactId), CancellationToken.None);

        result.Outcome.Should().Be(EvidenceContentOutcome.Success);
        result.Content.Should().NotBeNull();
        result.Content!.ContentType.Should().Be("image/jpeg");
        result.Content.ContentLength.Should().Be(3);
    }

    private static EvidenceArtifactDescriptor CreateDescriptor(Guid artifactId)
    {
        return new EvidenceArtifactDescriptor(
            artifactId,
            Guid.NewGuid(),
            "device-01",
            nameof(EvidenceType.Image),
            new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 1, 12, 1, 0, TimeSpan.Zero),
            nameof(EvidenceStatus.Preserved),
            "screenshot",
            "staging",
            "2026/04/01/capture.jpg",
            "v1",
            "image/jpeg",
            3,
            new string('a', 64));
    }

    private sealed class FakeEvidenceRepository : IEvidenceRepository
    {
        public EvidenceArtifactDescriptor? Descriptor { get; set; }

        public Task AddAsync(EvidenceItem entity, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<EvidenceTimelineItemResponse>> GetTimelineAsync(Guid caseId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<ArtifactListPage> GetArtifactsPageAsync(
            Guid caseId,
            ArtifactListCursor? cursor,
            int pageSize,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<EvidenceArtifactDescriptor?> GetArtifactDescriptorAsync(Guid artifactId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Descriptor);
        }
    }

    private sealed class FakeEvidenceBlobReader : IEvidenceBlobReader
    {
        public EvidenceContentStream? Response { get; set; }

        public Task<EvidenceContentStream?> OpenReadAsync(
            string containerName,
            string blobName,
            string? blobVersionId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Response);
        }
    }
}
