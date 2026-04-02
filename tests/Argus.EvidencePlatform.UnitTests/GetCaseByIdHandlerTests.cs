using Argus.EvidencePlatform.Application.Cases.GetCase;
using Argus.EvidencePlatform.Application.Common.Abstractions;
using Argus.EvidencePlatform.Contracts.Cases;
using Argus.EvidencePlatform.Domain.Cases;
using FluentAssertions;

namespace Argus.EvidencePlatform.UnitTests;

public sealed class GetCaseByIdHandlerTests
{
    [Fact]
    public async Task Handle_should_return_case_when_repository_finds_it()
    {
        var caseId = Guid.NewGuid();
        var expected = new CaseResponse(
            caseId,
            "CASE-2026-001",
            "Investigation",
            "High priority",
            nameof(CaseStatus.Active),
            new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero),
            null);
        var repository = new FakeCaseRepository
        {
            Response = expected
        };
        var handler = new GetCaseByIdHandler(repository);
        var query = new GetCaseByIdQuery(caseId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
        repository.LastRequestedCaseId.Should().Be(caseId);
    }

    [Fact]
    public async Task Handle_should_return_null_when_repository_does_not_find_case()
    {
        var caseId = Guid.NewGuid();
        var repository = new FakeCaseRepository();
        var handler = new GetCaseByIdHandler(repository);
        var query = new GetCaseByIdQuery(caseId);

        var result = await handler.Handle(query, CancellationToken.None);

        result.Should().BeNull();
        repository.LastRequestedCaseId.Should().Be(caseId);
    }

    private sealed class FakeCaseRepository : ICaseRepository
    {
        public CaseResponse? Response { get; set; }

        public Guid? LastRequestedCaseId { get; private set; }

        public Task AddAsync(Case entity, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<bool> ExistsAsync(Guid caseId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<bool> ExistsByExternalCaseIdAsync(string externalCaseId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Guid?> GetIdByExternalCaseIdAsync(string externalCaseId, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<CaseResponse?> GetByIdAsync(Guid caseId, CancellationToken cancellationToken)
        {
            LastRequestedCaseId = caseId;
            return Task.FromResult(Response);
        }
    }
}
