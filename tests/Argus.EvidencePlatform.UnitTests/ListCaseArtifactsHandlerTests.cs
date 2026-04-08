using System.Text;
using System.Text.Json;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Application.Evidence.ListArtifacts;
using Argus.EvidencePlatform.Contracts.Evidence;
using Argus.EvidencePlatform.Domain.Evidence;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class ListCaseArtifactsHandlerTests
{
    [Fact]
    public async Task Handle_should_return_items_and_next_cursor()
    {
        var caseId = Guid.NewGuid();
        var nextCursor = new ArtifactListCursor(
            new DateTimeOffset(2026, 4, 8, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 8, 10, 1, 0, TimeSpan.Zero),
            Guid.NewGuid());
        var repository = new FakeEvidenceRepository
        {
            Response = new ArtifactListPage(
            [
                new EvidenceArtifactListItem(
                    Guid.NewGuid(),
                    caseId,
                    "device-01",
                    nameof(EvidenceType.Image),
                    new DateTimeOffset(2026, 4, 8, 10, 0, 0, TimeSpan.Zero),
                    new DateTimeOffset(2026, 4, 8, 10, 1, 0, TimeSpan.Zero),
                    nameof(EvidenceStatus.Preserved),
                    "screenshot",
                    "image/jpeg",
                    128,
                    new string('a', 64),
                    true)
            ],
            nextCursor)
        };
        var handler = new ListCaseArtifactsHandler(repository);

        var result = await handler.Handle(new ListCaseArtifactsQuery(caseId, null, 25), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].DownloadUrl.Should().Be($"/api/evidence/artifacts/{result.Items[0].Id:D}/content");
        result.NextCursor.Should().NotBeNullOrWhiteSpace();
        repository.LastPageSize.Should().Be(25);
        repository.LastCaseId.Should().Be(caseId);
    }

    [Fact]
    public async Task Handle_should_decode_cursor_before_querying_repository()
    {
        var caseId = Guid.NewGuid();
        var expectedCursor = new ArtifactListCursor(
            new DateTimeOffset(2026, 4, 8, 10, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 8, 10, 1, 0, TimeSpan.Zero),
            Guid.NewGuid());
        var encodedCursor = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(expectedCursor)));
        var repository = new FakeEvidenceRepository
        {
            Response = new ArtifactListPage([], null)
        };
        var handler = new ListCaseArtifactsHandler(repository);

        await handler.Handle(new ListCaseArtifactsQuery(caseId, encodedCursor, null), CancellationToken.None);

        repository.LastCursor.Should().Be(expectedCursor);
        repository.LastPageSize.Should().Be(ListCaseArtifactsHandler.DefaultPageSize);
    }

    [Fact]
    public async Task Handle_should_reject_invalid_page_size()
    {
        var handler = new ListCaseArtifactsHandler(new FakeEvidenceRepository());

        var act = async () => await handler.Handle(new ListCaseArtifactsQuery(Guid.NewGuid(), null, 0), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    private sealed class FakeEvidenceRepository : IEvidenceRepository
    {
        public Guid? LastCaseId { get; private set; }

        public ArtifactListCursor? LastCursor { get; private set; }

        public int? LastPageSize { get; private set; }

        public ArtifactListPage Response { get; set; } = new([], null);

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
            LastCaseId = caseId;
            LastCursor = cursor;
            LastPageSize = pageSize;
            return Task.FromResult(Response);
        }

        public Task<EvidenceArtifactDescriptor?> GetArtifactDescriptorAsync(Guid artifactId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
