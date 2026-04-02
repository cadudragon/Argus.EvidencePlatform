using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Application.Evidence.GetTimeline;
using Argus.EvidencePlatform.Contracts.Evidence;
using Argus.EvidencePlatform.Domain.Evidence;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class GetEvidenceTimelineHandlerTests
{
    [Fact]
    public async Task Handle_should_return_timeline_from_repository()
    {
        var caseId = Guid.NewGuid();
        var expected = new List<EvidenceTimelineItemResponse>
        {
            new(
                Guid.NewGuid(),
                caseId,
                "device-01",
                nameof(EvidenceType.Image),
                new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 4, 1, 12, 1, 0, TimeSpan.Zero),
                nameof(EvidenceStatus.Preserved),
                "secret",
                "artifact-1.jpg",
                new string('a', 64),
                128,
                "image/jpeg")
        };
        var repository = new FakeEvidenceRepository(expected);
        var handler = new GetEvidenceTimelineHandler(repository);
        var query = new GetEvidenceTimelineQuery(caseId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeSameAs(expected);
        repository.LastRequestedCaseId.Should().Be(caseId);
    }

    [Fact]
    public async Task Handle_should_return_empty_timeline_when_repository_has_no_entries()
    {
        var caseId = Guid.NewGuid();
        var repository = new FakeEvidenceRepository([]);
        var handler = new GetEvidenceTimelineHandler(repository);
        var query = new GetEvidenceTimelineQuery(caseId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeEmpty();
        repository.LastRequestedCaseId.Should().Be(caseId);
    }

    private sealed class FakeEvidenceRepository(IReadOnlyList<EvidenceTimelineItemResponse> response) : IEvidenceRepository
    {
        public Guid? LastRequestedCaseId { get; private set; }

        public Task AddAsync(EvidenceItem entity, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<EvidenceTimelineItemResponse>> GetTimelineAsync(Guid caseId, CancellationToken cancellationToken)
        {
            LastRequestedCaseId = caseId;
            return Task.FromResult(response);
        }
    }
}
